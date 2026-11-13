#!/usr/bin/env node
// Smoke test for the mock MCP server: starts server.mjs over stdio with the official SDK client,
// lists the tools, reads work item 101 and runs the unit tests. Exit code 0 = everything worked.

import { Client } from '@modelcontextprotocol/sdk/client/index.js';
import { StdioClientTransport } from '@modelcontextprotocol/sdk/client/stdio.js';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const EXPECTED_TOOLS = ['list_work_items', 'get_work_item', 'add_test_cases', 'run_tests', 'update_test_result'];

function check(condition, message) {
  if (!condition) {
    console.error('FAIL: ' + message);
    process.exitCode = 1;
    throw new Error(message);
  }
  console.log('ok   ' + message);
}

function parse(result) {
  const text = result.content?.find((c) => c.type === 'text')?.text ?? '';
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

const transport = new StdioClientTransport({
  command: process.execPath,
  args: [path.join(HERE, 'server.mjs')],
  env: { ...process.env },
  stderr: 'inherit',
});
const client = new Client({ name: 'ado-mock-selftest', version: '1.0.0' });

try {
  await client.connect(transport);

  const { tools } = await client.listTools();
  console.log('tools/list -> ' + tools.map((t) => t.name).join(', '));
  check(EXPECTED_TOOLS.every((n) => tools.some((t) => t.name === n)), 'all 5 tools are listed');

  const item = parse(await client.callTool({ name: 'get_work_item', arguments: { id: 101 } }));
  console.log(`get_work_item(101) -> ${item.type} "${item.title}" state=${item.state} acceptanceCriteria="${item.acceptanceCriteria}"`);
  check(item.id === 101, 'work item 101 is returned');

  console.log('run_tests(kind "unit") -> running dotnet test, this can take a minute ...');
  const run = await client.callTool(
    { name: 'run_tests', arguments: { kind: 'unit' } },
    undefined,
    { timeout: 600_000 },
  );
  const summary = parse(run);
  console.log(JSON.stringify(summary, null, 2));
  check(!run.isError, 'run_tests did not report a tool error');
  check(summary.total > 0, `unit tests were discovered (total=${summary.total})`);
  check(summary.failed === 0, `no unit test failed (passed=${summary.passed}, failed=${summary.failed})`);

  console.log('\nSELFTEST PASSED');
} catch (err) {
  if (!process.exitCode) process.exitCode = 1;
  console.error('SELFTEST FAILED: ' + err.message);
} finally {
  await client.close();
}
