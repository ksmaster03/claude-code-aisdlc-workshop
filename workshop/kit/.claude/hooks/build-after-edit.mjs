// PostToolUse hook: after Claude edits a .cs file, build the solution.
// If the build fails, exit 2 so the compiler errors are fed back to Claude
// (the edit already happened; exit 2 here only reports, it cannot undo).
import { readFileSync } from "node:fs";
import { spawnSync } from "node:child_process";

const input = JSON.parse(readFileSync(0, "utf8").replace(/^\uFEFF/, "") || "{}");
const file = input?.tool_input?.file_path || "";
if (!file.endsWith(".cs") && !file.endsWith(".csproj")) process.exit(0);

const root = process.env.CLAUDE_PROJECT_DIR || process.cwd();
const r = spawnSync("dotnet", ["build", "OpsTrack.sln", "-nologo", "-v", "q", "-clp:ErrorsOnly"], {
  cwd: root,
  encoding: "utf8",
  timeout: 180000,
  shell: process.platform === "win32",
});

if (r.status !== 0) {
  const out = `${r.stdout || ""}${r.stderr || ""}`.split(/\r?\n/).filter((l) => /error/i.test(l)).slice(0, 20).join("\n");
  console.error(`Build failed after editing ${file}:\n${out || "(no error lines captured)"}`);
  process.exit(2);
}
process.exit(0);
