#!/usr/bin/env node
// Mock Azure DevOps MCP server for the OpsTrack lab.
// - stdio transport only, no network access
// - seed data: data/workitems.json (never modified)
// - runtime changes: data/workitems.local.json (git-ignored, delete it to reset)
// - test result summaries: data/results/<id>-<timestamp>.md
// Never write to stdout here except through the MCP transport: stdout is the protocol channel.

import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { z } from 'zod';
import { spawn } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(process.env.OPSTRACK_ROOT || path.join(HERE, '..', '..'));
const SEED_FILE = path.join(HERE, 'data', 'workitems.json');
const STATE_FILE = path.join(HERE, 'data', 'workitems.local.json');
const RESULTS_DIR = path.join(HERE, 'data', 'results');
const RUNS_DIR = path.join(HERE, '.runs');
const TEST_TIMEOUT_MS = Number(process.env.ADO_MOCK_TEST_TIMEOUT_MS || 300_000);

const TEST_PROJECTS = {
  unit: 'tests/OpsTrack.Business.Tests/OpsTrack.Business.Tests.csproj',
  api: 'tests/OpsTrack.Api.Tests/OpsTrack.Api.Tests.csproj',
};

// Characters allowed in a test filter. Arguments are passed without a shell, this is a second fence.
const SAFE_FILTER = /^[A-Za-z0-9_.=!~&|()\s,:\-฀-๿]*$/;

function log(...args) {
  process.stderr.write('[ado-mock] ' + args.join(' ') + '\n');
}

function loadState() {
  const file = fs.existsSync(STATE_FILE) ? STATE_FILE : SEED_FILE;
  return JSON.parse(fs.readFileSync(file, 'utf8'));
}

function saveState(state) {
  fs.writeFileSync(STATE_FILE, JSON.stringify(state, null, 2) + '\n', 'utf8');
}

function findItem(state, id) {
  const item = state.workItems.find((w) => w.id === id);
  if (!item) {
    throw new Error(`Work item ${id} not found`);
  }
  item.testCases ??= [];
  item.history ??= [];
  return item;
}

function asText(value) {
  return { content: [{ type: 'text', text: typeof value === 'string' ? value : JSON.stringify(value, null, 2) }] };
}

function asError(message) {
  return { isError: true, content: [{ type: 'text', text: message }] };
}

function timestamp() {
  return new Date().toISOString();
}

function fileStamp() {
  return new Date().toISOString().replace(/[-:]/g, '').replace(/\.\d+Z$/, 'Z');
}

function killTree(child) {
  if (!child.pid) return;
  if (process.platform === 'win32') {
    spawn('taskkill', ['/pid', String(child.pid), '/T', '/F'], { stdio: 'ignore' });
  } else {
    child.kill('SIGKILL');
  }
}

function run(command, args, cwd) {
  return new Promise((resolve) => {
    const started = Date.now();
    let output = '';
    let timedOut = false;
    let child;
    try {
      child = spawn(command, args, {
        cwd,
        env: { ...process.env, DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_NOLOGO: '1', CI: '1' },
        stdio: ['ignore', 'pipe', 'pipe'],
        windowsHide: true,
      });
    } catch (err) {
      resolve({ exitCode: -1, output: String(err), timedOut: false, durationMs: 0 });
      return;
    }
    const timer = setTimeout(() => {
      timedOut = true;
      killTree(child);
    }, TEST_TIMEOUT_MS);
    child.stdout.on('data', (d) => { output += d.toString(); });
    child.stderr.on('data', (d) => { output += d.toString(); });
    child.on('error', (err) => { output += '\n' + String(err); });
    child.on('close', (code) => {
      clearTimeout(timer);
      resolve({ exitCode: code, output, timedOut, durationMs: Date.now() - started });
    });
  });
}

function tail(text, lines = 40) {
  return text.split(/\r?\n/).slice(-lines).join('\n');
}

function decodeXml(s) {
  return s
    .replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&quot;/g, '"')
    .replace(/&apos;/g, "'").replace(/&#xD;/g, '').replace(/&#xA;/g, '\n').replace(/&amp;/g, '&');
}

function attr(tag, name) {
  const m = tag.match(new RegExp(`\\b${name}="([^"]*)"`));
  return m ? decodeXml(m[1]) : null;
}

function parseTrx(xml) {
  const counters = xml.match(/<Counters\b[^>]*\/>/);
  const num = (n) => (counters ? Number(attr(counters[0], n) || 0) : 0);
  const failedTests = [];
  const re = /<UnitTestResult\b([^>]*)>([\s\S]*?)<\/UnitTestResult>|<UnitTestResult\b([^>]*)\/>/g;
  let m;
  while ((m = re.exec(xml)) !== null) {
    const tag = m[1] ?? m[3];
    if (attr(tag, 'outcome') === 'Failed') {
      const msg = (m[2] || '').match(/<Message>([\s\S]*?)<\/Message>/);
      failedTests.push({
        name: attr(tag, 'testName'),
        message: msg ? decodeXml(msg[1]).trim().slice(0, 1500) : null,
      });
    }
  }
  return {
    total: num('total'),
    passed: num('passed'),
    failed: num('failed'),
    skipped: num('notExecuted'),
    failedTests,
  };
}

async function runDotnetTests(kind, filter) {
  fs.mkdirSync(RUNS_DIR, { recursive: true });
  const trxName = `${kind}-${fileStamp()}.trx`;
  const args = [
    'test', TEST_PROJECTS[kind], '--nologo',
    '--logger', `trx;LogFileName=${trxName}`,
    '--logger', 'console;verbosity=minimal',
    '--results-directory', RUNS_DIR,
  ];
  if (filter) args.push('--filter', filter);

  const result = await run('dotnet', args, REPO_ROOT);
  const trxPath = path.join(RUNS_DIR, trxName);
  const summary = {
    kind,
    filter: filter || null,
    command: 'dotnet ' + args.map((a) => (/\s/.test(a) ? `"${a}"` : a)).join(' '),
    exitCode: result.exitCode,
    timedOut: result.timedOut,
    durationMs: result.durationMs,
  };
  if (fs.existsSync(trxPath)) {
    Object.assign(summary, parseTrx(fs.readFileSync(trxPath, 'utf8')), { trx: path.relative(REPO_ROOT, trxPath).split(path.sep).join('/') });
  } else {
    Object.assign(summary, { total: 0, passed: 0, failed: 0, skipped: 0, failedTests: [], outputTail: tail(result.output) });
  }
  return summary;
}

async function runPlaywright(filter) {
  const e2eDir = path.join(REPO_ROOT, 'e2e');
  const cli = path.join(e2eDir, 'node_modules', '@playwright', 'test', 'cli.js');
  if (!fs.existsSync(cli)) {
    return { kind: 'e2e', error: 'Playwright is not installed. Run "npm install" in the e2e folder first.' };
  }
  const args = [cli, 'test', '--reporter=json'];
  if (filter) args.push('--grep', filter);
  const result = await run(process.execPath, args, e2eDir);
  const summary = {
    kind: 'e2e',
    filter: filter || null,
    command: 'npx playwright test --reporter=json' + (filter ? ` --grep "${filter}"` : ''),
    exitCode: result.exitCode,
    timedOut: result.timedOut,
    durationMs: result.durationMs,
  };
  const start = result.output.indexOf('{');
  const end = result.output.lastIndexOf('}');
  try {
    const report = JSON.parse(result.output.slice(start, end + 1));
    const failedTests = [];
    const walk = (suite, prefix) => {
      for (const spec of suite.specs || []) {
        const bad = (spec.tests || []).some((t) => t.status === 'unexpected' || t.status === 'flaky');
        if (bad) {
          const firstError = spec.tests.flatMap((t) => t.results || []).flatMap((r) => r.errors || [])[0];
          failedTests.push({ name: [...prefix, spec.title].join(' > '), message: firstError ? String(firstError.message).slice(0, 1500) : null });
        }
      }
      for (const child of suite.suites || []) walk(child, [...prefix, child.title]);
    };
    for (const suite of report.suites || []) walk(suite, [suite.title]);
    const s = report.stats || {};
    Object.assign(summary, {
      total: (s.expected || 0) + (s.unexpected || 0) + (s.flaky || 0) + (s.skipped || 0),
      passed: s.expected || 0,
      failed: (s.unexpected || 0) + (s.flaky || 0),
      skipped: s.skipped || 0,
      failedTests,
    });
  } catch {
    Object.assign(summary, { total: 0, passed: 0, failed: 0, skipped: 0, failedTests: [], outputTail: tail(result.output) });
  }
  return summary;
}

const server = new McpServer({ name: 'ado-mock', version: '1.0.0' });

server.registerTool(
  'list_work_items',
  {
    title: 'List work items',
    description: 'List work items in the OpsTrack project (id, type, title, state, tags). Optional filter by state, e.g. "New", "Active", "To Do".',
    inputSchema: { state: z.string().max(40).optional().describe('Only items in this state') },
    annotations: { readOnlyHint: true, openWorldHint: false },
  },
  async ({ state }) => {
    const items = loadState().workItems
      .filter((w) => !state || w.state.toLowerCase() === state.toLowerCase())
      .map(({ id, type, title, state: s, tags }) => ({ id, type, title, state: s, tags }));
    return asText(items);
  },
);

server.registerTool(
  'get_work_item',
  {
    title: 'Get work item',
    description: 'Full details of one work item: description, acceptance criteria, repro steps, links, test cases and history.',
    inputSchema: { id: z.number().int().positive().describe('Work item id, e.g. 101') },
    annotations: { readOnlyHint: true, openWorldHint: false },
  },
  async ({ id }) => {
    try {
      return asText(findItem(loadState(), id));
    } catch (err) {
      return asError(err.message);
    }
  },
);

server.registerTool(
  'add_test_cases',
  {
    title: 'Add test cases (write)',
    description: 'WRITE: attach new test cases to a work item. Each case has a title, ordered steps and the expected result.',
    inputSchema: {
      workItemId: z.number().int().positive(),
      cases: z.array(z.object({
        title: z.string().min(1).max(200),
        steps: z.union([z.string().min(1), z.array(z.string().min(1)).min(1)]).describe('Ordered steps, as a list or one text block'),
        expected: z.string().min(1).max(2000),
      })).min(1).max(50),
    },
    annotations: { readOnlyHint: false, destructiveHint: false, idempotentHint: false, openWorldHint: false },
  },
  async ({ workItemId, cases }) => {
    try {
      const state = loadState();
      const item = findItem(state, workItemId);
      const added = cases.map((c, i) => ({
        id: `TC-${workItemId}-${item.testCases.length + i + 1}`,
        title: c.title,
        steps: Array.isArray(c.steps) ? c.steps : c.steps.split(/\r?\n/).filter((s) => s.trim()),
        expected: c.expected,
        createdAt: timestamp(),
      }));
      item.testCases.push(...added);
      item.history.push({ at: timestamp(), by: 'ado-mock-mcp', action: 'Test cases added', detail: added.map((c) => c.id).join(', ') });
      saveState(state);
      log(`added ${added.length} test cases to ${workItemId}`);
      return asText({ workItemId, added });
    } catch (err) {
      return asError(err.message);
    }
  },
);

server.registerTool(
  'run_tests',
  {
    title: 'Run tests',
    description: 'Run OpsTrack tests from the repo root and return pass/fail counts and failed test names. ' +
      'kind "unit" = tests/OpsTrack.Business.Tests, "api" = tests/OpsTrack.Api.Tests (both via dotnet test, filter is a dotnet --filter expression such as "Area=Sla"), ' +
      '"e2e" = Playwright in e2e/ (filter is a --grep pattern). Time-limited.',
    inputSchema: {
      kind: z.enum(['api', 'unit', 'e2e']),
      filter: z.string().max(200).regex(SAFE_FILTER, 'filter contains characters that are not allowed').optional(),
    },
    annotations: { readOnlyHint: false, destructiveHint: false, openWorldHint: false },
  },
  async ({ kind, filter }) => {
    log(`run_tests kind=${kind} filter=${filter ?? ''}`);
    const summary = kind === 'e2e' ? await runPlaywright(filter) : await runDotnetTests(kind, filter);
    if (summary.error) return asError(summary.error);
    if (summary.timedOut) return { isError: true, content: [{ type: 'text', text: JSON.stringify(summary, null, 2) }] };
    return asText(summary);
  },
);

server.registerTool(
  'update_test_result',
  {
    title: 'Update test result (write)',
    description: 'WRITE: record a test run outcome on a work item. Saves the Markdown summary to data/results and appends an entry to the work item history.',
    inputSchema: {
      workItemId: z.number().int().positive(),
      outcome: z.enum(['Passed', 'Failed', 'Blocked', 'NotExecuted']),
      summaryMarkdown: z.string().min(1).max(20000),
    },
    annotations: { readOnlyHint: false, destructiveHint: false, idempotentHint: false, openWorldHint: false },
  },
  async ({ workItemId, outcome, summaryMarkdown }) => {
    try {
      const state = loadState();
      const item = findItem(state, workItemId);
      fs.mkdirSync(RESULTS_DIR, { recursive: true });
      const file = path.join(RESULTS_DIR, `${workItemId}-${fileStamp()}.md`);
      const header = `# ผลการทดสอบ work item ${workItemId}: ${item.title}\n\n- ผล: ${outcome}\n- บันทึกเมื่อ: ${timestamp()}\n\n`;
      fs.writeFileSync(file, header + summaryMarkdown.trim() + '\n', 'utf8');
      const relative = path.relative(REPO_ROOT, file).split(path.sep).join('/');
      item.lastTestOutcome = outcome;
      item.history.push({ at: timestamp(), by: 'ado-mock-mcp', action: 'Test result', outcome, detail: relative });
      saveState(state);
      log(`test result ${outcome} saved for ${workItemId}`);
      return asText({ workItemId, outcome, file: relative });
    } catch (err) {
      return asError(err.message);
    }
  },
);

const transport = new StdioServerTransport();
await server.connect(transport);
log(`ready, repo root ${REPO_ROOT}`);
