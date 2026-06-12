#!/usr/bin/env node
// Stop: before the agent finishes its turn, scan Assets/ and Packages/ for .meta files
// whose paired asset is missing (e.g. a script deleted without its .meta). Exit 2 blocks
// the stop and reports the orphans so the agent cleans them up before declaring done.
const fs = require('fs');
const path = require('path');
let data = {};
try { data = JSON.parse(fs.readFileSync(0, 'utf8')); } catch { /* tolerate empty input */ }
if (data && data.stop_hook_active) process.exit(0); // already re-prompted once; avoid loops

const roots = ['Assets', 'Packages'].filter(d => fs.existsSync(d));
const orphans = [];
function walk(dir) {
  let entries;
  try { entries = fs.readdirSync(dir, { withFileTypes: true }); } catch { return; }
  for (const e of entries) {
    const p = path.join(dir, e.name);
    if (e.isDirectory()) walk(p);
    else if (e.name.endsWith('.meta') && !fs.existsSync(p.slice(0, -'.meta'.length))) orphans.push(p);
  }
}
roots.forEach(walk);
if (orphans.length) {
  process.stderr.write(
    'Orphaned .meta files (paired asset missing):\n' + orphans.join('\n') +
    '\nDelete them or restore the assets before finishing.\n');
  process.exit(2);
}
process.exit(0);
