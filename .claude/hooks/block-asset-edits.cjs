#!/usr/bin/env node
// PreToolUse(Edit|Write|MultiEdit): block hand-edits to Unity YAML assets and .meta files.
// These are YAML containing GUIDs/fileIDs that break silently when edited outside the Editor.
// Cross-platform (Windows + Claude Code web) via Node.
const fs = require('fs');
let data;
try { data = JSON.parse(fs.readFileSync(0, 'utf8')); } catch { process.exit(0); }
const fp = data && data.tool_input && data.tool_input.file_path;
if (!fp) process.exit(0);
const norm = fp.replace(/\\/g, '/');

if (/\.(prefab|unity|asset|mat|controller|anim|playable|mixer|overrideController)$/i.test(norm) || /\.meta$/i.test(norm)) {
  process.stderr.write(`Blocked: '${fp}' is a Unity-serialized asset or .meta file. Change it through the Unity Editor or an Editor script - hand-editing corrupts GUIDs/fileIDs.\n`);
  process.exit(2);
}
process.exit(0);
