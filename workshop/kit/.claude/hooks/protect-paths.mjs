// PreToolUse hook: block edits to protected paths.
// Claude Code sends the tool call as JSON on stdin. Exit code 2 blocks the call
// and stderr is shown to Claude as the reason.
// Protected paths are listed in .claude/protected-paths.txt (one prefix per line, # = comment).
//
// This is a guardrail against mistakes, used together with a deny rule in settings.json.
// It compares REAL paths, so other spellings of the same file cannot slip past it:
// letter case, 8.3 short names (DB~1), junctions and symlinks, trailing dots or spaces,
// and ":stream" suffixes. When a path cannot be resolved, it blocks (fails closed).
import { readFileSync, existsSync, lstatSync, realpathSync } from "node:fs";
import path from "node:path";

const isWin = process.platform === "win32";
const block = (msg) => { console.error(msg); process.exit(2); };

let input;
try {
  input = JSON.parse(readFileSync(0, "utf8").replace(/^\uFEFF/, "") || "{}");
} catch {
  block("Blocked: protect-paths hook could not read the tool call.");
}
const file = input?.tool_input?.file_path;
if (!file) process.exit(0);

// Resolve the nearest part of the path that exists to its real path (expands short names
// and links, fixes letter case), then add back the parts that do not exist yet (a new file).
// lstat does not follow links, so a link whose target is missing still counts as existing;
// realpath then throws for it and the caller blocks. Parts added back from `rest` do not
// exist at all, so none of them can be a link.
function realPath(p) {
  let cur = path.resolve(p);
  const rest = [];
  for (;;) {
    let exists = true;
    try { lstatSync(cur); } catch { exists = false; }
    if (exists) return path.join(realpathSync.native(cur), ...rest);
    const parent = path.dirname(cur);
    if (parent === cur) throw new Error("no existing parent folder");
    rest.unshift(path.basename(cur));
    cur = parent;
  }
}

// Lower-case on every platform: Windows and macOS file systems ignore case by default.
// On a case-sensitive Linux disk this can block a little more than needed, which is the safe side.
const norm = (p) => p.toLowerCase();

const root = process.env.CLAUDE_PROJECT_DIR || process.cwd();
const abs = path.resolve(root, file);

if (isWin) {
  // Windows silently drops trailing dots and spaces, and "name:stream" writes to the same file.
  const segments = abs.slice(path.parse(abs).root.length).split(/[\\/]/);
  if (segments.some((s) => /[. ]$/.test(s)) || segments.some((s) => s.includes(":"))) {
    block(`Blocked: ${file} uses a Windows path form (trailing dot or space, or ":stream") that can point at another file.`);
  }
}

let realRoot, realFile;
try {
  realRoot = realPath(root);
  realFile = realPath(abs);
} catch {
  block(`Blocked: could not resolve ${file} to a real path.`);
}

const rel = norm(path.relative(realRoot, realFile).split(path.sep).join("/"));

const listFile = path.join(root, ".claude", "protected-paths.txt");
const prefixes = existsSync(listFile)
  ? readFileSync(listFile, "utf8").split(/\r?\n/).map((s) => s.trim()).filter((s) => s && !s.startsWith("#"))
  : ["db/scripts/released/"];

// match whole folder or file names only: "released/" must not match "released-old/"
const hit = prefixes.find((p) => (rel + "/").startsWith(norm(p).replace(/\/?$/, "/")));
if (hit) {
  block(
    `Blocked: ${rel} is protected (${hit}). ` +
    `Released SQL scripts and locked tests must not change. ` +
    `Add a new script under db/scripts/pending/ or explain why the test is wrong instead.`
  );
}
process.exit(0);
