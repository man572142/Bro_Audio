#!/usr/bin/env node
// PostToolUse(Edit|Write|MultiEdit): report UnityEditor usage outside #if UNITY_EDITOR
// guards in runtime assemblies. Heuristic, not a compiler:
// - An assembly is Editor-only iff its .asmdef lists "Editor" in includePlatforms.
//   An absent or empty includePlatforms means "all platforms" => runtime assembly.
// - Any #if/#elif whose condition mentions a non-negated UNITY_EDITOR counts as a guard
//   (compound conditions like `UNITY_EDITOR || DEBUG` are accepted; rare false negatives).
// PostToolUse runs after the edit, so exit 2 feeds the report back to Claude for a fix;
// it does not revert the file.
const fs = require('fs');
const path = require('path');
let data;
try { data = JSON.parse(fs.readFileSync(0, 'utf8')); } catch { process.exit(0); }
const fp = data && data.tool_input && data.tool_input.file_path;
if (!fp || !/\.cs$/i.test(fp) || !fs.existsSync(fp)) process.exit(0);

// Walk up to the governing .asmdef (nearest ancestor wins, mirroring Unity).
let dir = path.dirname(path.resolve(fp));
let asmdef = null;
while (true) {
  let entries = [];
  try { entries = fs.readdirSync(dir); } catch { break; }
  const found = entries.find(f => f.toLowerCase().endsWith('.asmdef'));
  if (found) { asmdef = path.join(dir, found); break; }
  const parent = path.dirname(dir);
  if (parent === dir) break;
  dir = parent;
}
if (!asmdef) process.exit(0); // predefined assemblies (Assembly-CSharp*) — nothing to check against

let def;
try { def = JSON.parse(fs.readFileSync(asmdef, 'utf8')); } catch { process.exit(0); }
const include = Array.isArray(def.includePlatforms) ? def.includePlatforms : [];
if (include.includes('Editor')) process.exit(0); // Editor assembly — UnityEditor is fine

const isEditorGuard = cond => /(^|[^!\w])UNITY_EDITOR\b/.test(cond || '');
const lines = fs.readFileSync(fp, 'utf8').split(/\r?\n/);
const stack = []; // one bool per open #if: true = editor guard
const violations = [];
for (let i = 0; i < lines.length; i++) {
  const t = lines[i].trim();
  let m;
  if ((m = /^#if\s+(.*)/.exec(t))) { stack.push(isEditorGuard(m[1])); continue; }
  if (/^#endif\b/.test(t)) { stack.pop(); continue; }
  if ((m = /^#(?:elif|else)\b\s*(.*)/.exec(t))) {
    if (stack.length) stack[stack.length - 1] = isEditorGuard(m[1]);
    continue;
  }
  if (stack.includes(true)) continue;
  if (/^\s*using\s+(static\s+)?UnityEditor\b/.test(lines[i]) || /\bUnityEditor\s*\./.test(lines[i])) {
    violations.push(i + 1);
  }
}
if (violations.length) {
  process.stderr.write(
    `${path.basename(fp)} belongs to runtime assembly '${def.name || path.basename(asmdef)}' ` +
    `(${path.relative(process.cwd(), asmdef)}) but uses UnityEditor outside #if UNITY_EDITOR ` +
    `on line(s) ${violations.join(', ')}. Guard the code or move it to an Editor assembly.\n`);
  process.exit(2);
}
process.exit(0);
