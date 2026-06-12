# Unity hooks for Claude Code — protocol, installation, and per-hook docs

Hooks are the deterministic safety layer for Unity projects. CLAUDE.md sets expectations;
hooks enforce them on every tool call. The scripts in `../scripts/` are working,
cross-platform implementations — copy and adapt them rather than writing hooks from prose.

## Hook protocol (how Claude Code actually invokes hooks)

- The hook command is executed with a **JSON payload on stdin** — NOT as CLI arguments and
  NOT as per-field environment variables. Parse stdin and read the fields you need:
  - `tool_name` — e.g. `Edit`, `Write`, `Bash`
  - `tool_input.file_path` — for Edit/Write/MultiEdit
  - `tool_input.command` — for Bash
  - `hook_event_name`, `session_id`, `cwd`, and (Stop only) `stop_hook_active`
- **Exit codes**: `0` = allow/pass. `2` = block: stderr is fed back to Claude.
  - `PreToolUse` + exit 2: the tool call is blocked before it runs.
  - `PostToolUse` + exit 2: the edit has already happened; the hook *reports* the problem
    so Claude fixes it — it does not revert the file.
  - `Stop` + exit 2: Claude is prevented from finishing and must address the report.
    Stop hooks receive `stop_hook_active: true` when the stop was already blocked once in
    this cycle — exit 0 in that case to avoid infinite loops (see `check-orphan-meta.cjs`).
- Hook commands run from the project root with the project's `.claude/settings.json` config.
- The bundled scripts are Node (`.cjs`) instead of bash so they behave identically on
  Windows, macOS, Linux, and Claude Code on the web. Node is always available where
  Claude Code runs. Every script fails open (`exit 0`) on unparseable input so a harness
  change degrades to "no enforcement", never to "everything blocked".

## Installation

1. Copy the needed scripts from this skill's `scripts/` directory into the target
   project's `.claude/hooks/`.
2. Merge `scripts/settings.example.json` into the project's `.claude/settings.json`
   (create it if absent). Don't clobber existing hooks — append to the matching arrays.
3. Adapt per-project details: blocked-extension lists, protected paths, git branch names,
   and meta templates (see per-hook notes below).
4. Sanity-check one hook end-to-end, e.g.:
   `echo '{"tool_input":{"file_path":"Assets/Foo.prefab"}}' | node .claude/hooks/block-asset-edits.cjs`
   — expect a "Blocked" message and exit code 2.

## The hooks

| Script | Event | What it does |
|---|---|---|
| `block-bash.cjs` | PreToolUse (Bash) | Blocks force-push, push to main/master, `rm -rf` |
| `block-asset-edits.cjs` | PreToolUse (Edit\|Write\|MultiEdit) | Blocks hand-edits to Unity YAML assets and `.meta` files |
| `protect-project-config.cjs` | PreToolUse (Edit\|Write\|MultiEdit) | Blocks writes to `ProjectSettings/` and `Packages/manifest.json` |
| `check-assembly-boundaries.cjs` | PostToolUse (Edit\|Write\|MultiEdit) | Reports unguarded `UnityEditor` usage in runtime assemblies |
| `generate-meta.cjs` | PostToolUse (Write) | Mints a fresh-GUID `.meta` for new scripts/asmdefs/shaders |
| `check-orphan-meta.cjs` | Stop | Scans for `.meta` files whose paired asset is missing |

### generate-meta.cjs — highest-impact Unity hook

Claude Code runs outside the Unity Editor, so Unity won't create `.meta` files for new
scripts until the next Editor refresh. A `.cs` committed without its `.meta` causes GUID
churn for everyone who pulls. This hook writes a `.meta` with a freshly minted GUID
(32 lowercase hex chars) the moment a new file is created; Unity adopts the GUID as-is on
refresh and upgrades any importer fields it wants to.

- Generation-only: it never touches an existing `.meta`. Pair it with
  `block-asset-edits.cjs`, which blocks Edit/Write on `*.meta`, so the agent can't
  modify GUIDs that other assets reference.
- Covers `.cs`, `.asmdef`, `.asmref`, `.shader`, `.hlsl`/`.cginc`. Extend the template
  map in the script for other importer types the project creates from the CLI
  (`ComputeShaderImporter`, `TextScriptImporter`, etc.) — copy a freshly generated
  `.meta` from the project as the template and substitute the GUID.

### check-assembly-boundaries.cjs — heuristic, know its limits

Walks up from the edited `.cs` to the nearest `.asmdef` (mirroring Unity's nearest-wins
rule), then flags `using UnityEditor` / `UnityEditor.` outside `#if UNITY_EDITOR` guards
in runtime assemblies.

- Editor-only is defined as `includePlatforms` containing `"Editor"`. An **absent or
  empty** `includePlatforms` means "all platforms" and is treated as runtime — this also
  correctly treats `excludePlatforms: ["Editor"]` assemblies as runtime.
- Compound guards (`#if UNITY_EDITOR || DEBUG`) and `#else`/`#elif` branches are handled;
  negated guards (`#if !UNITY_EDITOR`) are correctly treated as unguarded.
- It is a text heuristic: `UnityEditor.` inside a comment or string can false-positive,
  and exotic preprocessor logic can slip through. The Unity Editor compile is the truth;
  this hook just saves a failed-edit-and-retry round trip.
- Files with no governing `.asmdef` (predefined assemblies like Assembly-CSharp) are
  skipped — there's no declared boundary to check against.

### block-asset-edits.cjs

Unity stores prefabs, scenes, ScriptableObject assets, materials, and animator
controllers as YAML full of GUIDs and fileIDs. They look text-editable but break silently
when hand-edited. Extend the extension list for project-specific serialized types
(e.g. `.spriteatlas`, `.lighting`, `.shadervariants`), and add binary types (`.fbx`,
images, audio) if the agent has any reason to touch those paths.

### protect-project-config.cjs

`ProjectSettings/` and `Packages/manifest.json` affect every team member; route changes
through a human. Add other shared config the team protects (e.g. `Packages/packages-lock.json`).

### block-bash.cjs

Standard destructive-command set. Adjust branch names to the repo's default branch and
relax rules the team explicitly allows (e.g. `--force-with-lease` to personal branches).

### check-orphan-meta.cjs

Catches the deletion half of the `.meta` pairing problem: removing an asset via Bash
without removing its `.meta`. Runs at Stop so one scan covers the whole session's work.

## Wiring

`scripts/settings.example.json` is the canonical wiring. Notes:

- `generate-meta.cjs` is matched on `Write` only — Edit/MultiEdit can't create files.
- Order within a matcher is the execution order; keep blockers (asset/config) before
  anything that mutates state.
- If a hook should only warn, change its `exit 2` to `exit 0` and keep the stderr message.
