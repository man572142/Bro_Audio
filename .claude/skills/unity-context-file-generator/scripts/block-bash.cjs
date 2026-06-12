#!/usr/bin/env node
// PreToolUse(Bash): block destructive git/file operations.
// Adapt the patterns to the team's workflow (e.g. some teams allow --force-with-lease).
const fs = require('fs');
let data;
try { data = JSON.parse(fs.readFileSync(0, 'utf8')); } catch { process.exit(0); }
const cmd = data && data.tool_input && data.tool_input.command;
if (!cmd) process.exit(0);

const rules = [
  { re: /\bgit\s+push\b[^|;&]*\s(--force\b|-f\b|--force-with-lease\b)/, msg: 'force-push is not allowed' },
  { re: /\bgit\s+push\b[^|;&]*\s(origin|upstream)\s+(main|master)\b/, msg: 'pushing directly to main/master is not allowed' },
  { re: /\brm\s+[^|;&]*-(?:[a-z]*r[a-z]*f|[a-z]*f[a-z]*r)[a-z]*\b/i, msg: 'recursive force-delete is not allowed; delete specific files instead' },
];
for (const rule of rules) {
  if (rule.re.test(cmd)) {
    process.stderr.write(`Blocked: ${rule.msg}.\nCommand: ${cmd}\n`);
    process.exit(2);
  }
}
process.exit(0);
