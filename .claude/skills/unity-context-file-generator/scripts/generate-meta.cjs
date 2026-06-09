#!/usr/bin/env node
// PostToolUse(Write): generate a .meta with a fresh GUID for new files created outside
// Unity, so the asset and its .meta are committed together (avoids GUID churn for
// teammates pulling the change). Generation-only: never touches an existing .meta.
// Writes LF, UTF-8, no BOM. Unity adopts the GUID and upgrades importer fields on refresh.
// Extend the template map below for other importer types the project uses.
const fs = require('fs');
const crypto = require('crypto');
let data;
try { data = JSON.parse(fs.readFileSync(0, 'utf8')); } catch { process.exit(0); }
const fp = data && data.tool_input && data.tool_input.file_path;
if (!fp) process.exit(0);
if (!fs.existsSync(fp)) process.exit(0);

const meta = fp + '.meta';
if (fs.existsSync(meta)) process.exit(0);

const guid = crypto.randomUUID().replace(/-/g, '');
const tail = `  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
`;
let content;
if (/\.cs$/i.test(fp)) {
  content = `fileFormatVersion: 2
guid: ${guid}
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData:
  assetBundleName:
  assetBundleVariant:
`;
} else if (/\.asmdef$/i.test(fp)) {
  content = `fileFormatVersion: 2
guid: ${guid}
AssemblyDefinitionImporter:
${tail}`;
} else if (/\.asmref$/i.test(fp)) {
  content = `fileFormatVersion: 2
guid: ${guid}
AssemblyDefinitionReferenceImporter:
${tail}`;
} else if (/\.shader$/i.test(fp)) {
  content = `fileFormatVersion: 2
guid: ${guid}
ShaderImporter:
  externalObjects: {}
  defaultTextures: []
  nonModifiableTextures: []
  userData:
  assetBundleName:
  assetBundleVariant:
`;
} else if (/\.(hlsl|cginc)$/i.test(fp)) {
  content = `fileFormatVersion: 2
guid: ${guid}
DefaultImporter:
${tail}`;
} else {
  process.exit(0);
}

fs.writeFileSync(meta, content, { encoding: 'utf8' });
process.stderr.write(`Generated ${meta} (guid ${guid}). Verify in Unity on next refresh.\n`);
process.exit(0);
