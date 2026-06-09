---
name: unity-context-file-generator
description: Generate, audit, or improve a Unity project's CLAUDE.md for Claude Code. Use this skill whenever the user wants to create, audit, refactor, or debug a CLAUDE.md file for a Unity (C#/.NET) project
---

# Unity CLAUDE.md Generator & Auditor

Generate, audit, or refactor a Unity project's CLAUDE.md so Claude Code works effectively in the repo. Unity-specific edition — covers assembly definitions, serialization, Editor/Runtime boundaries, and the unique constraints of Unity's compilation pipeline.

Bundled with this skill (read/copy on demand — don't paste their contents into chat unprompted):

- `references/hooks.md` — hook protocol (stdin JSON, exit-code semantics), per-hook docs, installation. **Read it before doing Stage 5.**
- `scripts/*.cjs` + `scripts/settings.example.json` — working, cross-platform Node hook implementations, ready to copy into the target project's `.claude/hooks/`.

## Why this matters

Auto-generated context files (e.g. `/init` output left untouched) tend to hurt agent performance: they spend the context budget on what the agent could infer from the repo and bury the few facts it can't. Unity projects compound this: the agent may guess wrong about assembly boundaries, forget to create `.meta` files when working outside Unity, reference Editor-only APIs from Runtime code, or break serialization — failures that often surface only in the Unity Editor, not at the CLI.

CLAUDE.md is advisory — the harness injects it with a reminder that it may or may not be relevant, and the model weighs it against everything else in context, so treat compliance as probabilistic. Hooks run deterministically on every tool call. Unity has many deterministic failure modes that hooks catch better than prose ever will. Bias toward hooks for anything that would break the build or corrupt the asset database, and reserve CLAUDE.md for the higher-level conventions the agent needs in order to compose code correctly in the first place.

---

## Core principles (internalize before writing a single line)

1. **Less is more.** Compose toward **~150 lines**; treat **200 lines** as the hard ceiling for the root file. Well-performing production CLAUDE.md files are often under 60 lines. Past the ceiling, split into `.claude/rules/*.md` with path-scoped frontmatter — never pad the root.

2. **Prefer the non-inferable.** For every candidate line, apply the litmus test: *"Does this cost fewer tokens to declare than for the agent to discover?"* A one-line Unity version declaration is cheaper than parsing `ProjectSettings/ProjectVersion.txt`; a directory tree dump is not. What the agent definitely cannot infer on its own: assembly definition boundaries, custom Editor tooling locations, team serialization conventions, build pipeline quirks, and lessons from past agent failures — always high-value.

3. **Hooks over prose for deterministic rules.** Anything that must happen 100% of the time belongs in `.claude/settings.json` hooks. For Unity this especially means: assembly boundary violations, missing `.meta` files for new scripts, edits to serialized/binary assets, and writes to `ProjectSettings/`. Don't repeat in prose what a hook already enforces — state the rule once and let the hook do the rest.

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
| Existing file > 200 lines | **Refactor** — split into root + `.claude/rules/*.md` |
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

This table is the single inclusion filter for the whole skill — Stage 6 and Audit mode verify against it rather than defining separate lists.

| Drop (agent can infer, or a hook enforces) | Keep (agent cannot infer) |
|---|---|
| Verbose Unity feature descriptions / API docs (the agent knows Unity; link instead) | Concise tech stack: "Unity 2022.3 LTS, URP, C# (.NET Standard 2.1)" |
| Full directory tree / `tree` output | Key directories with *purpose* annotations (1 line each) |
| Standard Unity API usage | Internal wrappers: "use `AudioManager.Play()`, never raw `AudioSource`" |
| C# style rules a linter / `.editorconfig` handles | Non-obvious conventions: "all ScriptableObjects live under `Assets/Data/`" |
| README intro / project description | Assembly definition map (helps agent compose correct `using` directives) |
| Full `Packages/manifest.json` contents (agent can read it) | Editor tooling locations: "custom inspectors in `Assets/Editor/Inspectors/`" |
| Rules already enforced by hooks (one passing mention max) | Hard-won lessons from past agent failures |
| "Write clean code" / "be careful with X" / empty platitudes | Why a key architectural decision was made (if non-obvious) |
| Task- or scenario-specific instructions in the root | The same instructions as `.claude/rules/*.md` with `paths:` frontmatter |

Two absolute rules, regardless of the table: **no secrets** (API keys, tokens, internal URLs, customer data — CLAUDE.md is committed to git), and **no contradictory priorities** without an explicit ordering.

### Stage 3 — Compose in priority order

Build sections in this order. Stop adding when you hit ~150 lines; anything remaining goes into `.claude/rules/*.md` with path-scoped frontmatter.

**Priority 1 — Commands (most valuable signal)**

Unity projects typically lack a single CLI build command. Adapt to what the project actually uses:

````markdown
## Commands
```bash
# Tests (Edit Mode)
unity-editor -runTests -testPlatform EditMode -projectPath . -testResults results.xml

# Tests (Play Mode)
unity-editor -runTests -testPlatform PlayMode -projectPath . -testResults results.xml

# Build (if a CLI build script exists)
unity-editor -batchmode -nographics -executeMethod BuildScript.PerformBuild -quit

# Format check
dotnet format <solution>.sln --verify-no-changes
```
````

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
- 🚫 Never: force-push, push to `main`, commit `Library/` or `Temp/`, hand-edit YAML asset files (the asset hook will block this — but state it for human readers too)
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

````markdown
## Style
✅
```csharp
[SerializeField] private float moveSpeed = 5f;

private void Awake()
{
    _rigidbody = GetComponent<Rigidbody>();  // cache once
}
```

❌
```csharp
public float MoveSpeed = 5f;  // expose via [SerializeField] instead

void Awake()
{
    _rigidbody = FindObjectOfType<Rigidbody>();  // scene-wide search, expensive
}
```
````

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
- **Scenario-specific guidance** (testing conventions, shader rules, networking) goes in `.claude/rules/*.md` with `paths:` frontmatter, not in conditional prose in the root — it then loads only when the agent touches matching files.

### Stage 5 — Recommend hooks (do this thoroughly)

For Unity projects, hooks do most of the safety work. Frame them to the user as *the primary enforcement mechanism* — CLAUDE.md is just the human-readable conventions on top.

**Read `references/hooks.md` first**, then copy the scripts from `scripts/` into the project's `.claude/hooks/` and merge `scripts/settings.example.json` into its `.claude/settings.json`, adapting paths, extension lists, and branch names to the project.

| Script | Event | Purpose |
|---|---|---|
| `block-bash.cjs` | PreToolUse (Bash) | Block force-push, push to main, `rm -rf` |
| `block-asset-edits.cjs` | PreToolUse (Edit\|Write\|MultiEdit) | Block hand-edits to Unity YAML assets and `.meta` files |
| `protect-project-config.cjs` | PreToolUse (Edit\|Write\|MultiEdit) | Block writes to `ProjectSettings/`, `Packages/manifest.json` |
| `check-assembly-boundaries.cjs` | PostToolUse (Edit\|Write\|MultiEdit) | Report unguarded `UnityEditor` usage in runtime assemblies |
| `generate-meta.cjs` | PostToolUse (Write) | Mint fresh-GUID `.meta` for new scripts — **highest impact** |
| `check-orphan-meta.cjs` | Stop | Catch `.meta` files orphaned by asset deletion |

Key protocol facts (full detail in `references/hooks.md`): hooks receive a **JSON payload on stdin** (`tool_input.file_path`, `tool_input.command`) — not CLI arguments or per-field environment variables; exit code 2 blocks (PreToolUse) or reports back to Claude (PostToolUse/Stop); the scripts are Node for Windows/macOS/Linux/web parity.

### Stage 6 — Self-check before delivery

Before presenting the output, verify:

1. **Line count**: root file ≤ 200 lines? If not, extract to `.claude/rules/*.md`.
2. **Stage 2 filter**: re-scan every line against the Stage 2 table — anything in the "Drop" column gets deleted, including rules a hook already enforces (at most one passing mention, e.g. in Boundaries).
3. **Command test**: mentally invoke "recite the build/test commands." If the file doesn't make them obvious in the first 30 lines, restructure.
4. **No secrets**: no API keys, internal URLs, customer data, or vulnerability details.
5. **Emphasis budget**: at most 2 uses of IMPORTANT / YOU MUST / NEVER across the whole file.

---

## Audit mode (existing file)

When the user has an existing CLAUDE.md, score it against these criteria:

| Check | Pass condition |
|---|---|
| Length | ≤ 200 lines in root file |
| Commands present | Build/test commands in fenced bash blocks within first 30 lines |
| Stage 2 filter | Nothing from the Stage 2 "Drop" column (no README duplication, `tree` dumps, lint-able style rules, platitudes, inferable content) |
| No hook duplication | Assembly boundaries, .meta handling, asset protection live in hooks rather than prose |
| Boundaries defined | ✅/⚠️/🚫 tiers present |
| Definition of Done | Exit-code-verifiable checklist present |
| Assembly map | Present if project has 3+ `.asmdef` files |
| Emphasis budget | ≤ 2 uses of IMPORTANT / MUST / NEVER |
| No secrets | No API keys, tokens, internal URLs |
| Hooks present | `.claude/settings.json` enforces the deterministic Unity rules |

If hooks are missing, propose adding them (Stage 5) as part of the audit response. Many existing CLAUDE.md files compensate for the lack of hooks with verbose prose rules; replacing those with hooks shrinks the file *and* improves reliability — a double win.

Output a summary table with ✅ / ❌ per check, then propose specific edits. Prefer surgical fixes over full rewrites — respect the user's existing structure and intent.

---

## Refactor mode (file > 200 lines)

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
5. Remind user: path-scoped rules cost no context when the agent isn't touching matching files — this is how to scale past the ceiling without polluting every session.

---

## Reference: memory hierarchy (load order, last wins)

1. **Managed policy** — system-wide, set by IT, users cannot override
2. **User** — `~/.claude/CLAUDE.md`, personal prefs across all projects
3. **Project** — `./CLAUDE.md` or `./.claude/CLAUDE.md`, team-shared via git
4. **Local** — `./CLAUDE.local.md`, personal project notes, gitignored
5. **Subdirectory** — loaded on-demand when agent reads files in that subtree
6. **Path-scoped rules** — `.claude/rules/*.md` with `paths:` frontmatter, loaded only when matching files are touched

`@import` syntax: relative paths resolve against the containing file, max 5 hops of recursion. Instructions that exist only in conversation don't persist across sessions or compaction — durable rules belong in CLAUDE.md, rules files, or hooks.
