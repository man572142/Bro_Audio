---
name: unity-context-file-generator
description: Generate, audit, or improve a Unity project's CLAUDE.md for Claude Code. Use this skill whenever the user wants to create, audit, refactor, or debug a CLAUDE.md file for a Unity (C#/.NET) project — including when they say "Claude keeps ignoring X" in a Unity repo, mention .asmdef structure, or want to set up Claude Code for their Unity workflow. Also triggers for requests about Unity project context files, Unity-specific agent rules, or structuring CLAUDE.md around Unity conventions (assembly definitions, serialization, Editor vs Runtime, ScriptableObjects, Addressables, etc.). Do NOT use for non-Unity .NET or general C# projects — use the general context-file-generator skill instead.
---

# Unity CLAUDE.md Generator & Auditor

Generate, audit, or refactor a Unity project's CLAUDE.md so Claude Code works effectively in the repo. Unity-specific edition — covers assembly definitions, serialization, Editor/Runtime boundaries, and the unique constraints of Unity's compilation pipeline.

## Why this matters

Auto-generated context files (e.g. `/init` output left untouched) measurably hurt agent performance — lower solve rates, higher inference cost. Unity projects compound this: the agent may guess wrong about assembly boundaries, forget to create `.meta` files when working outside Unity, reference Editor-only APIs from Runtime code, or break serialization — failures that often surface only in the Unity Editor, not at the CLI.

The general rule still applies, with extra weight for Unity: **CLAUDE.md sets expectations (~70% compliance); hooks enforce them deterministically.** Unity has many deterministic failure modes that hooks catch better than prose ever will. Bias toward hooks for anything that would break the build or corrupt the asset database, and reserve CLAUDE.md for the higher-level conventions the agent needs in order to compose code correctly in the first place.

---

## Core principles (internalize before writing a single line)

1. **Less is more.** Target **under 200 lines** for the root file. Well-performing production CLAUDE.md files are often under 60 lines. If you're over 200, split into `.claude/rules/*.md` with path-scoped frontmatter — never pad the root.

2. **Prefer the non-inferable.** For every candidate line, apply the litmus test: *"Does this cost fewer tokens to declare than for the agent to discover?"* A one-line Unity version declaration is cheaper than parsing `ProjectSettings/ProjectVersion.txt`; a directory tree dump is not. What the agent definitely cannot infer on its own: assembly definition boundaries, custom Editor tooling locations, team serialization conventions, build pipeline quirks, and lessons from past agent failures — always high-value.

3. **Hooks over prose for deterministic rules.** CLAUDE.md is advisory — Claude Code wraps it in a `<system_reminder>` noting the content *may or may not be relevant*. Anything that must happen 100% of the time belongs in `.claude/settings.json` hooks. For Unity, this especially means: assembly boundary violations, missing `.meta` files for new scripts, edits to binary or YAML assets, and writes to `ProjectSettings/`. Don't repeat in prose what a hook already enforces — state the rule once and let the hook do the rest.

4. **Commands first, prose last.** Agents reference build/test commands repeatedly. Put them at the top. Fenced `bash` blocks, not English descriptions of what to type.

5. **Reserve emphasis markers.** `IMPORTANT:` and `YOU MUST` boost adherence, but only when rare. Max 2 per file; more dilutes the signal.

---

## Workflow

### Mode detection

Determine which mode the user needs:

| User signal | Mode |
|---|---|
| No existing CLAUDE.md | **Generate** — full creation flow |
| Existing file present | **Audit** — read it, score it, propose targeted fixes |
| Existing file > 250 lines | **Refactor** — split into root + `.claude/rules/*.md` |
| User says "Claude keeps ignoring X" | **Debug** — check if rule belongs in hooks, not prose |

### Stage 1 — Inventory the project (read-only)

Read these files (skip missing ones silently):

```
ProjectSettings/ProjectVersion.txt
README.md
Packages/manifest.json
Assets/**/*.asmdef                 (list paths + assembly names only, don't read full contents of every one)
Existing CLAUDE.md / CLAUDE.local.md
.claude/settings.json
.editorconfig
CI config (.github/workflows/, .gitlab-ci.yml, Jenkinsfile)
```

Run `ls` (or `view`) on the top level of `Assets/`, `Packages/`, and `ProjectSettings/` to understand the project shape. Detect:

- **Render pipeline**: URP / HDRP / Built-in (check `Packages/manifest.json` for `com.unity.render-pipelines.*`)
- **Assembly definitions**: count and layout of `.asmdef` / `.asmref` — defines compilation boundaries the agent must respect when composing code
- **Testing**: presence of `Tests/` folders, test `.asmdef` referencing `UnityEngine.TestRunner` / `UnityEditor.TestRunner`
- **Package manager**: local packages in `Packages/`, embedded vs registry
- **Addressables / Asset Bundles**: presence of `AddressableAssetsData/`, `StreamingAssets/`
- **Source control**: `.gitignore` / `.p4ignore`, whether `.meta` files are properly tracked, precompiled DLLs under `Plugins/`
- **CI/CD**: build scripts under `Assets/Editor/Build/`, GameCI workflows, etc.

**Do NOT read every source file.** You are gathering metadata, not doing a code review.

### Stage 2 — Filter: what goes in vs. what stays out

| Drop (agent can infer, or a hook enforces) | Keep (agent cannot infer) |
|---|---|
| Verbose Unity feature descriptions | Concise tech stack: "Unity 2022.3 LTS, URP, C# (.NET Standard 2.1)" |
| Full directory tree / `tree` output | Key directories with *purpose* annotations (1 line each) |
| Standard Unity API usage | Internal wrappers: "use `AudioManager.Play()`, never raw `AudioSource`" |
| C# style rules a linter / `.editorconfig` handles | Non-obvious conventions: "all ScriptableObjects live under `Assets/Data/`" |
| README intro / project description | Assembly definition map (helps agent compose correct `using` directives) |
| API docs (link instead) | Editor tooling locations: "custom inspectors in `Assets/Editor/Inspectors/`" |
| Rules already enforced by hooks | Hard-won lessons from past agent failures |
| "Write clean code" / empty platitudes | Why a key architectural decision was made (if non-obvious) |

### Stage 3 — Compose in priority order

Build sections in this order. Stop adding when you hit ~150 lines; anything remaining goes into `.claude/rules/*.md` with path-scoped frontmatter.

**Priority 1 — Commands (most valuable signal)**

Unity projects typically lack a single CLI build command. Adapt to what the project actually uses:

```markdown
## Commands
\```bash
# Tests (Edit Mode)
unity-editor -runTests -testPlatform EditMode -projectPath . -testResults results.xml

# Tests (Play Mode)
unity-editor -runTests -testPlatform PlayMode -projectPath . -testResults results.xml

# Build (if a CLI build script exists)
unity-editor -batchmode -nographics -executeMethod BuildScript.PerformBuild -quit

# Format check
dotnet format <solution>.sln --verify-no-changes
\```
```

If the project has no CLI test or build commands, say so explicitly — this stops the agent from guessing at nonexistent commands:

```markdown
## Commands
No CLI build pipeline. Builds run from Unity Editor.
Tests: open Test Runner (Window > General > Test Runner).
```

**Priority 2 — Definition of Done**

```markdown
## Definition of Done
A task is complete when ALL of these pass:
1. `dotnet format --verify-no-changes` exits 0 (if applicable)
2. No compiler errors visible in the changed files (full verification requires the Unity Editor)
3. All `.claude/settings.json` hooks pass (assembly boundaries, .meta generation, asset protection)
4. Changed files staged with commit message format: `type(scope): description`
```

Hooks handle the Unity-specific safety checks; the agent doesn't need a separate prose checklist for them.

**Priority 3 — Boundaries (three-tier)**

```markdown
## Boundaries
- ✅ Always: read the relevant `.asmdef` before editing files in that assembly so `using` directives match
- ⚠️ Ask first: adding new package dependencies, creating new `.asmdef` files, modifying `ProjectSettings/`, shader changes, schema changes to ScriptableObjects that have existing instances
- 🚫 Never: force-push, push to `main`, commit `Library/` or `Temp/`, hand-edit YAML asset files (the binary-asset hook will block this — but state it for human readers too)
```

**Priority 4 — Tech stack**

Two to four lines. Call out anything non-default or that the agent would guess wrong.

```markdown
## Tech Stack
Unity 2022.3 LTS, URP, C# (.NET Standard 2.1)
Input System: New Input System (com.unity.inputsystem) — NOT legacy Input Manager
Testing: Unity Test Framework (Edit Mode + Play Mode)
CI: GitHub Actions with GameCI
```

**Priority 5 — Project structure (semantic value only)**

One line per key directory describing its *purpose* — what `ls` alone cannot tell the agent. Skip if the layout is self-evident.

```markdown
## Project Structure
- `Assets/Scripts/Runtime/` – gameplay code, split by .asmdef per system
- `Assets/Scripts/Editor/` – custom Editor tools, inspectors, build scripts
- `Assets/Data/` – ScriptableObject assets (game config, item databases)
- `Assets/Prefabs/` – prefab hierarchy mirrors scene structure
- `Packages/com.team.core/` – local package: shared utilities
- `Tests/` – EditMode and PlayMode test assemblies
```

**Priority 6 — Assembly Map (Unity-specific, high value)**

For projects with 3+ `.asmdef` files, a brief dependency map helps the agent compose correct `using` directives and pick the right namespace. Even with the assembly-boundary hook as a safety net, this saves an entire failed-edit-and-retry cycle.

```markdown
## Assembly Map
- `Game.Runtime` (Assets/Scripts/Runtime/) → Unity.Mathematics, Game.Core
- `Game.Core` (Packages/com.team.core/Runtime/) → leaf assembly, no references
- `Game.Editor` (Assets/Scripts/Editor/) → Game.Runtime, Game.Core (Editor-only)
- `Game.Tests` (Tests/) → Game.Runtime, UnityEngine.TestRunner, UnityEditor.TestRunner
```

Skip this section entirely for single-assembly projects.

**Priority 7 — Code style (only if not linter-enforceable)**

Prefer a single real code example (✅ good / ❌ bad) over prose:

```markdown
## Style
✅
\`\`\`csharp
[SerializeField] private float moveSpeed = 5f;

private void Awake()
{
    _rigidbody = GetComponent<Rigidbody>();  // cache once
}
\`\`\`

❌
\`\`\`csharp
public float MoveSpeed = 5f;  // expose via [SerializeField] instead

void Awake()
{
    _rigidbody = FindObjectOfType<Rigidbody>();  // scene-wide search, expensive
}
\`\`\`
```

**Priority 8 — Common pitfalls / Lessons learned**

Append-only section. Apply the **two-strikes rule**: only promote a lesson to a permanent rule on its *second* occurrence. First occurrence might be noise. Pre-seed with a few universally applicable Unity pitfalls if the project has no history yet:

```markdown
## Pitfalls
- Coroutines silently stop when the GameObject is disabled or destroyed. Prefer async/await with cancellation tokens for long-running operations.
- `GetComponent<T>()` in `Update()` allocates. Cache in `Awake()`.
- ScriptableObject field changes in Play Mode persist to disk — never rely on runtime SO state being transient.
```

**Priority 9 — Pointers to extended docs**

```markdown
## References
@docs/architecture.md
@docs/networking.md
```

### Stage 4 — Format for maximum compliance

- **Imperative mood**: "Cache GetComponent results in Awake()" not "please consider caching."
- **Bullet lists** over paragraphs.
- **Fenced `bash` blocks** for all commands.
- **Code examples** (✅ / ❌ pairs) for style rules.
- **Three-tier emoji boundaries** (✅ ⚠️ 🚫) for scannability.
- **One `IMPORTANT:` max** — reserve for the single most critical rule.
- **Conditional blocks** for scenario-specific instructions:
  ```xml
  <important if="you are writing or modifying tests">
  - Place tests under `Tests/` with the matching .asmdef
  - Test classes mirror the source namespace
  </important>
  ```

### Stage 5 — Emit hook recommendations (do this thoroughly)

For Unity projects, hooks do most of the safety work. Output a recommended `.claude/settings.json` together with the actual hook scripts, and frame them to the user as *the primary enforcement mechanism* — CLAUDE.md is just the human-readable conventions on top.

Generate the following hooks by default, adapting paths to the project's actual layout. Examples below use bash; for Windows-only teams, adapt to PowerShell or a small .NET Native AOT executable — the logic is identical.

#### Hook 1 — Block dangerous bash operations (PreToolUse on Bash)

Standard set: block `rm -rf`, `git push --force`, pushes to `main`/`master`, writes that target `Library/`, `Temp/`, or `.git/`.

#### Hook 2 — Block hand-edits to binary / YAML assets (PreToolUse on Edit|Write)

Unity stores prefabs, scenes, ScriptableObjects, materials, and animator controllers as YAML containing GUIDs and file IDs. They look text-editable but break silently when hand-edited. Block edits to: `.prefab`, `.unity`, `.asset`, `.mat`, `.controller`, `.playable`, `.anim`, `.fbx`, image files, audio files.

```bash
#!/usr/bin/env bash
# .claude/hooks/block-binary-asset-edits.sh
file_path="$1"
case "$file_path" in
  *.prefab|*.unity|*.asset|*.mat|*.controller|*.playable|*.anim|*.fbx|*.png|*.jpg|*.wav|*.ogg)
    echo "Refusing to edit Unity asset file directly: $file_path" >&2
    echo "Use the Unity Editor or an Editor script instead." >&2
    exit 2
    ;;
esac
exit 0
```

#### Hook 3 — Assembly boundary check (PostToolUse on Edit|Write)

For any `.cs` file modified, walk up the directory tree to find the governing `.asmdef`, then check whether `UnityEditor` is referenced outside `#if UNITY_EDITOR` guards in an assembly that doesn't include the Editor platform.

```bash
#!/usr/bin/env bash
# .claude/hooks/check-assembly-boundaries.sh
file_path="$1"
[[ "$file_path" != *.cs ]] && exit 0

# Find governing asmdef
dir=$(dirname "$file_path")
while [[ "$dir" != "/" && -z "$(find "$dir" -maxdepth 1 -name '*.asmdef' 2>/dev/null)" ]]; do
  dir=$(dirname "$dir")
done
asmdef=$(find "$dir" -maxdepth 1 -name '*.asmdef' 2>/dev/null | head -n1)
[[ -z "$asmdef" ]] && exit 0

# Runtime assembly if includePlatforms is empty or doesn't contain "Editor"
if grep -q '"includePlatforms": \[\]' "$asmdef" || ! grep -q '"Editor"' "$asmdef"; then
  # Strip lines inside #if UNITY_EDITOR blocks, then check for raw using UnityEditor
  if awk '
      /^[[:space:]]*#if UNITY_EDITOR/ {skip=1; next}
      /^[[:space:]]*#endif/          {skip=0; next}
      !skip
    ' "$file_path" | grep -nE '^using UnityEditor'; then
    echo "Runtime assembly $asmdef contains unguarded UnityEditor usage in $file_path" >&2
    exit 2
  fi
fi
exit 0
```

#### Hook 4 — Auto-generate `.meta` files for new scripts (PostToolUse on Write)

This is the **highest-impact Unity hook**. When the agent creates a new `.cs` file outside Unity (the common case — Claude Code runs in a terminal, not the Editor), Unity won't generate the `.meta` file until the Editor next refreshes. If the agent commits the `.cs` without its `.meta`, anyone pulling the change hits GUID conflicts or has to open Unity and regenerate manually — slow and error-prone.

The hook generates a `.meta` file with a freshly minted GUID from a template per file type. Unity recognizes the existing `.meta` on next refresh and uses it as-is.

```bash
#!/usr/bin/env bash
# .claude/hooks/generate-meta.sh
# Generates .meta files for new .cs/.asmdef/.shader files created outside Unity.
file_path="$1"
[[ ! -f "$file_path" ]] && exit 0

meta_path="${file_path}.meta"
[[ -f "$meta_path" ]] && exit 0   # already exists, leave alone

template_dir="$(dirname "$0")/meta-templates"

case "$file_path" in
  *.cs)      template="$template_dir/cs.meta.tmpl" ;;
  *.asmdef)  template="$template_dir/asmdef.meta.tmpl" ;;
  *.shader)  template="$template_dir/shader.meta.tmpl" ;;
  *.hlsl)    template="$template_dir/hlsl.meta.tmpl" ;;
  *)         exit 0 ;;
esac

[[ ! -f "$template" ]] && { echo "No meta template for $file_path" >&2; exit 0; }

# Generate Unity-format GUID (32 lowercase hex chars, no dashes)
if command -v uuidgen >/dev/null; then
  guid=$(uuidgen | tr -d '-' | tr '[:upper:]' '[:lower:]')
else
  guid=$(python3 -c "import uuid; print(uuid.uuid4().hex)")
fi

sed "s/__GUID__/$guid/" "$template" > "$meta_path"
echo "Generated $meta_path" >&2
exit 0
```

Templates live in `.claude/hooks/meta-templates/`. Example for `cs.meta.tmpl`:

```yaml
fileFormatVersion: 2
guid: __GUID__
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
```

Add similar templates for the importer types the project uses (`AssemblyDefinitionImporter`, `ShaderImporter`, `ComputeShaderImporter`, `NativePluginImporter`, etc.). The hook is generation-only — it never touches existing `.meta` files. Pair it with a PreToolUse rule that blocks `Edit` on `*.meta` so the agent can't accidentally modify GUIDs that other assets reference.

#### Hook 5 — Detect orphaned `.meta` files (Stop)

Before declaring done, scan for `.meta` files whose paired asset is missing.

```bash
#!/usr/bin/env bash
# .claude/hooks/check-orphan-meta.sh
orphans=$(find Assets Packages -name '*.meta' 2>/dev/null | while read meta; do
  paired="${meta%.meta}"
  [[ ! -e "$paired" ]] && echo "$meta"
done)
if [[ -n "$orphans" ]]; then
  echo "Orphaned .meta files detected (asset missing):" >&2
  echo "$orphans" >&2
  exit 2
fi
exit 0
```

#### Hook 6 — Protect shared project config (PreToolUse on Edit|Write)

```bash
#!/usr/bin/env bash
# .claude/hooks/protect-project-config.sh
file_path="$1"
case "$file_path" in
  ProjectSettings/*|Packages/manifest.json)
    echo "Blocked: $file_path is shared project config. Ask the user before modifying." >&2
    exit 2
    ;;
esac
exit 0
```

#### Wiring it up

```json
{
  "hooks": {
    "PreToolUse": [
      { "matcher": "Bash", "hooks": [{ "type": "command", "command": "bash .claude/hooks/block-bash.sh" }] },
      { "matcher": "Edit|Write", "hooks": [
        { "type": "command", "command": "bash .claude/hooks/block-binary-asset-edits.sh \"$CLAUDE_TOOL_INPUT_file_path\"" },
        { "type": "command", "command": "bash .claude/hooks/protect-project-config.sh \"$CLAUDE_TOOL_INPUT_file_path\"" }
      ]},
      { "matcher": "Edit", "hooks": [{ "type": "command", "command": "bash .claude/hooks/block-meta-edits.sh \"$CLAUDE_TOOL_INPUT_file_path\"" }] }
    ],
    "PostToolUse": [
      { "matcher": "Edit|Write", "hooks": [
        { "type": "command", "command": "bash .claude/hooks/check-assembly-boundaries.sh \"$CLAUDE_TOOL_INPUT_file_path\"" },
        { "type": "command", "command": "bash .claude/hooks/generate-meta.sh \"$CLAUDE_TOOL_INPUT_file_path\"" }
      ]}
    ],
    "Stop": [
      { "hooks": [{ "type": "command", "command": "bash .claude/hooks/check-orphan-meta.sh" }] }
    ]
  }
}
```

Adjust the input-variable name if the user's Claude Code version uses a different convention; the principle is the same.

### Stage 6 — Self-check before delivery

Before presenting the output, verify:

1. **Line count**: root file ≤ 200 lines? If not, extract to `.claude/rules/*.md`.
2. **Litmus test**: for each line, can the agent infer this from the repo, or does a hook already enforce it? Delete anything that fails.
3. **Command test**: mentally invoke "recite the build/test commands." If the file doesn't make them obvious in the first 20 lines, restructure.
4. **No hook duplication**: rules enforced by `.claude/settings.json` hooks should not also appear as prose rules in CLAUDE.md. At most one passing mention (e.g. in Boundaries) is fine.
5. **No secrets**: no API keys, internal URLs, customer data, or vulnerability details. CLAUDE.md is committed to git.
6. **No `/init` smell**: no directory tree dumps, no restated README, no Unity feature explanations the agent already knows.
7. **Emphasis budget**: at most 2 uses of IMPORTANT / YOU MUST / NEVER across the whole file.

---

## Audit mode (existing file)

When the user has an existing CLAUDE.md, score it against these criteria:

| Check | Pass condition |
|---|---|
| Length | ≤ 200 lines in root file |
| Commands present | Build/test commands in fenced bash blocks within first 30 lines |
| No README duplication | No project description paragraphs restating the README |
| No inferable content | No `tree` dumps, no verbose Unity feature descriptions |
| No lint-able rules | No style rules `.editorconfig` already enforces |
| No hook duplication | Assembly boundaries, .meta handling, binary asset protection live in hooks rather than prose |
| Boundaries defined | ✅/⚠️/🚫 tiers present |
| Definition of Done | Exit-code-verifiable checklist present |
| Assembly map | Present if project has 3+ `.asmdef` files |
| No platitudes | No "write clean code", "follow SOLID", "be careful" |
| Emphasis budget | ≤ 2 uses of IMPORTANT / MUST / NEVER |
| No secrets | No API keys, tokens, internal URLs |
| Hooks present | `.claude/settings.json` enforces the deterministic Unity rules |

If hooks are missing, propose adding them as part of the audit response. Many existing CLAUDE.md files compensate for the lack of hooks with verbose prose rules; replacing those with hooks shrinks the file *and* improves reliability — a double win.

Output a summary table with ✅ / ❌ per check, then propose specific edits. Prefer surgical fixes over full rewrites — respect the user's existing structure and intent.

---

## Refactor mode (file > 250 lines)

1. Identify topic clusters. Common Unity clusters:
   - **Runtime conventions** → `.claude/rules/runtime.md` (path: `Assets/Scripts/Runtime/**`)
   - **Editor tooling** → `.claude/rules/editor.md` (path: `Assets/Scripts/Editor/**`, `Assets/Editor/**`)
   - **Testing** → `.claude/rules/testing.md` (path: `Tests/**`, `**/Tests/**`)
   - **Networking / Multiplayer** → `.claude/rules/networking.md` (path: `**/Networking/**`, `**/Netcode/**`)
   - **Shader / VFX** → `.claude/rules/shaders.md` (path: `**/*.shader`, `**/*.hlsl`, `Assets/Shaders/**`)
   - **Build / CI** → `.claude/rules/build.md` (path: `.github/**`, `Assets/Editor/Build/**`)

2. Extract each cluster with path-scoped frontmatter:
   ```markdown
   ---
   paths:
     - "Assets/Scripts/Runtime/**/*.cs"
   ---
   # Runtime Code Conventions
   ...
   ```

3. Replace extracted content in root with `@.claude/rules/<topic>.md` imports.
4. Verify root is now ≤ 150 lines (leave room for growth).
5. Remind user: path-scoped rules have **zero token cost** when the agent isn't touching matching files — this is how to scale past 200 lines without context pollution.

---

## Reference: Anthropic's memory hierarchy (load order, last wins)

1. **Managed policy** — system-wide, set by IT, users cannot override
2. **User** — `~/.claude/CLAUDE.md`, personal prefs across all projects
3. **Project** — `./CLAUDE.md` or `./.claude/CLAUDE.md`, team-shared via git
4. **Local** — `./CLAUDE.local.md`, personal project notes, gitignored
5. **Subdirectory** — loaded on-demand when agent reads files in that subtree
6. **Path-scoped rules** — `.claude/rules/*.md` with `paths:` frontmatter, loaded only when matching files are touched

`@import` syntax: relative paths resolve against the containing file, max 5 hops of recursion. HTML comments (`<!-- -->`) outside code blocks are stripped before injection (zero token cost, safe for maintainer notes).

After `/compact`, root CLAUDE.md is re-read from disk. Subdirectory files reload when the agent next touches files in that subtree. Conversation-only instructions are lost.

---

## What NOT to generate

Never include any of the following in the output CLAUDE.md:

- Verbatim `tree` output or full directory listings (a curated list of key directories with *purpose annotations* is fine and encouraged)
- Restated README content
- Unity API documentation or feature explanations (the agent knows Unity)
- Lint-enforceable C# style rules (defer to `.editorconfig` / `dotnet format`)
- Vague directives: "be careful with serialization", "handle errors gracefully"
- Rules already enforced by hooks (assembly boundaries, `.meta` handling, binary asset protection, `ProjectSettings/` writes) — state them once in the hook section, not as repeated prose
- Instructions to hand-edit YAML-serialized Unity files (the binary-asset hook blocks these anyway)
- Task-specific instructions in the root (use path-scoped rules or conditional blocks)
- Contradictory priorities without explicit ordering
- More than one `IMPORTANT:` marker
- Full `Packages/manifest.json` contents (agent can read it)
- Any secrets, tokens, or sensitive URLs
