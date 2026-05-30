# Investigation Plan: Removing the "Entity Asset" Addressable Toggle (Toggle B)

> **Status:** Proposed / not yet executed
> **Goal:** Determine whether it is *safe* and *beneficial* to remove the second
> Addressable toggle in the AudioEntity inspector — the one that registers the
> `AudioEntity` **asset itself** in Unity's Addressables system — and, if so,
> remove it cleanly.
> **Audience:** A fresh session with no prior context. This document is
> self-contained; read it top to bottom before touching code.

---

## 0. TL;DR / Background

BroAudio's entity inspector currently exposes **two** Addressable-related toggles
that look similar but operate on different axes:

- **Toggle A — "Addressables" (Clips tab).**
  Backing field: `AudioEntity.UseAddressables`
  (`Assets/BroAudio/Runtime/DataStruct/Core/AudioEntity.Addressables.cs:13`).
  Switches each clip's storage between a **hard** `AudioClip` reference and a
  **soft** `AssetReferenceT<AudioClip>`. This is the mechanism that actually
  changes BroAudio's runtime clip loading and the clips' build footprint. **Keep.**

- **Toggle B — "Addressable" (Overall tab).** *(removal candidate)*
  Drawn in `DrawEntityAddressableProperty`
  (`Assets/BroAudio/Editor/EntityPropertyDrawer/AudioEntityEditor.AdditionalProperties.cs:89-167`).
  It calls `AddressableAssetSettings.CreateOrMoveEntry` / `RemoveAssetEntry` on the
  `AudioEntity` **`.asset` file itself** and exposes its Address + Group fields.

### Why Toggle B is suspect (the hypothesis to verify)

`AudioEntity` is a `ScriptableObject` saved as its own `.asset`
(`AudioAssetEditor.cs:329-340`). At runtime the entity is **always** reached
through a hard-reference chain rooted in a `Resources` prefab:

```
Resources/SoundManager.prefab          (Resources.Load in SoundManager.cs:21)
   └─ _data : BroAudioData              (SoundManager.cs:69, [SerializeField])
        └─ _assets : List<AudioAsset>   (BroAudioData.cs:20)
             └─ ConvertedEntities : List<AudioEntity>  (AudioAsset.cs:154, [SerializeField])
                  └─ BroAudioClip.AudioClip (hard AudioClip ref when Toggle A is OFF)
```

Consequences that make Toggle B look inert/harmful **for BroAudio's own pipeline**:

1. **Runtime never loads the entity via Addressables.** Entity lookup goes through
   `SoundManager.TryConvertIdToEntity` → iterates `_data.Assets` →
   `AudioAsset.TryGetEntityFromId` (`SoundManager.cs:506-528`,
   `SoundID.cs:53`). There is **no** `Addressables.LoadAssetAsync<AudioEntity>`
   or load-by-address anywhere in `Runtime/`.
2. **Nothing in `Runtime/` reads the entity's addressable registration**
   (no `FindAssetEntry` / address usage in runtime code).
3. Because the entity is force-included via the `Resources`-rooted graph,
   marking it Addressable **duplicates** it (and its hard-referenced clips) into
   both the player build and the Addressables bundle — it does **not** strip
   anything from the build. So enabling Toggle B alone gives you the worst case,
   not on-demand loading.
4. The drawer even has a commented-out `//entity.address = entity.name;`
   (`AudioEntityEditor.AdditionalProperties.cs:129`), suggesting the feature was
   never finished.

**Therefore the working thesis is:** Toggle B is not used by BroAudio internally;
its only legitimate purpose is letting a user register the entity asset for their
*own* external Addressables loading. Removing it would simplify the UI and remove
a duplication footgun — *if* no one depends on it. **This plan exists to confirm
that thesis before acting, not to assume it.**

> ⚠️ All line numbers above were accurate at authoring time. **Re-verify them**
> at the start of execution (grep, don't trust offsets) — the file may have moved.

---

## 1. Scope

**In scope**
- Confirm Toggle B has no runtime consumers and no internal editor consumers
  beyond its own drawer.
- Decide remove vs. keep-and-relabel based on evidence.
- If removing: delete the drawer method + its call site + any now-orphaned helpers,
  and verify compilation under all package-define permutations.

**Out of scope**
- Toggle A (`UseAddressables`) — must remain fully functional.
- Migration of existing user data (Toggle B writes to Unity's global
  `AddressableAssetSettings`, not to BroAudio assets — see §5).
- Any change to clip-level Addressables behavior.

---

## 2. Pre-work: re-establish ground truth

Run these and confirm findings still match §0 before proceeding. Do **not** edit
anything in this phase.

1. **Locate Toggle B and its call site.**
   ```
   grep -rn "DrawEntityAddressableProperty" Assets/BroAudio
   ```
   Expect: definition in `AudioEntityEditor.AdditionalProperties.cs`, one call in
   `DrawAdditionalBaseProperties` (Overall tab).

2. **Confirm no runtime use of entity-level Addressables.**
   ```
   grep -rn "LoadAssetAsync<AudioEntity>\|FindAssetEntry\|CreateOrMoveEntry\|RemoveAssetEntry\|\.address" Assets/BroAudio/Runtime
   ```
   Expect: **no matches in `Runtime/`** (matches only in `Editor/`).

3. **Confirm entity retrieval is direct-reference only.**
   Read `SoundManager.cs` `TryConvertIdToEntity` (~`:506`) and
   `SoundID.cs` (`_entity`, ~`:18` and `TryConvertIdToEntity` ~`:223`).
   Confirm the entity comes from `_data.Assets`, never from Addressables.

4. **Confirm Toggle A is independent.**
   ```
   grep -rn "UseAddressables" Assets/BroAudio
   ```
   Confirm Toggle B's drawer does **not** read/write `UseAddressables`, and that
   `UseAddressables` is consumed in `SoundManager.Playback.cs` (~`:103`) and
   `SoundManager.Addressables.cs` (~`:125`). These must stay untouched.

5. **Confirm `AudioEntity` is a `ScriptableObject` saved as its own asset.**
   ```
   grep -rn "class AudioEntity" Assets/BroAudio/Runtime
   grep -rn "CreateAsset(newEntity" Assets/BroAudio/Editor
   ```

**Gate:** If any of 1–4 contradicts §0 (e.g. some runtime code *does* load entities
via Addressables, or Toggle A and B are coupled), **stop and reassess** — the
removal thesis is wrong and this plan must be revised.

---

## 3. Safety analysis (decision gate)

Answer each explicitly in the eventual PR/notes:

- [ ] **Compilation safety.** Toggle B code lives behind `#if PACKAGE_ADDRESSABLES`
  (`AudioEntityEditor.AdditionalProperties.cs:95`). Confirm removal leaves the file
  valid both with and without the define, and with/without `PACKAGE_LOCALIZATION`.
- [ ] **No internal callers.** From §2.1, the only caller is the Overall-tab draw
  path. Confirm removing it doesn't leave a dangling reference or an empty
  conditional block that warns.
- [ ] **No public API surface.** Confirm `DrawEntityAddressableProperty` and any
  helpers it uses exclusively are `private`/internal to the editor and not part of
  any `public` API or referenced by tests/samples:
  ```
  grep -rn "DrawEntityAddressableProperty" Assets        # incl. Assets/Tests, Samples~
  ```
- [ ] **Orphaned members.** Identify anything used *only* by Toggle B (private
  `GUIContent` fields, layout offsets like the `Offset -= SingleLineSpace * 0.5f`
  tweak, helper methods) so they're removed too — but verify each is not shared
  with other drawers before deleting.
- [ ] **User-data impact.** Toggle B mutates Unity's global
  `AddressableAssetSettings` (entries pointing at entity `.asset` GUIDs). Removing
  the toggle does **not** remove existing entries a user already created. Decide
  whether that's acceptable (recommended: yes — leave existing entries alone; they
  are harmless and user-owned) and document it. Do **not** auto-delete user
  Addressables entries.

**Benefit analysis (the "beneficial?" half):**
- [ ] UI simplification: one fewer confusing, similarly-named toggle in the
  inspector.
- [ ] Removes a duplication footgun (entity + clips duplicated into a bundle while
  still hard-referenced by the `Resources` graph).
- [ ] Net code reduction; less surface to maintain behind `#if PACKAGE_ADDRESSABLES`.
- [ ] **Counter-consideration:** does any *documentation, changelog, sample, or
  asset-store description* advertise entity-asset addressability as a feature?
  ```
  grep -rni "addressable" Assets/BroAudio --include=*.md --include=*.txt
  ```
  If it's a documented/sold feature, prefer **keep-and-relabel** over removal.

**Decision:** Proceed to §4 (remove) **only if** every safety box is checked and
no counter-consideration blocks it. Otherwise fall through to §6 (alternatives).

---

## 4. Removal implementation (only if §3 passes)

Develop on a dedicated branch (do **not** push to `main`):
`claude/remove-entity-asset-addressable-toggle`.

1. **Remove the call site** in `DrawAdditionalBaseProperties`
   (`AudioEntityEditor.AdditionalProperties.cs`, ~`:76`).
2. **Remove the method** `DrawEntityAddressableProperty`
   (`AudioEntityEditor.AdditionalProperties.cs:89-167`), including its
   `#if PACKAGE_ADDRESSABLES` wrapper.
3. **Remove orphaned helpers/fields** identified in §3 (only those used solely by
   Toggle B). Re-grep each before deleting.
4. **Leave Toggle A entirely untouched** — `DrawUseAddressablesToggle`,
   `SwitchAddressable`, `UseAddressables`, and all `SoundManager` clip-loading code.
5. **Do not** touch `AutoGeneratedCode/`, Unity YAML assets, `.meta` files,
   `ProjectSettings/`, or `Packages/manifest.json` (all hook-blocked — see
   `CLAUDE.md` / `.claude/settings.json`).

---

## 5. Verification

BroAudio has no CLI build/test pipeline; full verification needs the Unity Editor.
At minimum:

1. **Static checks (this session):**
   ```
   grep -rn "DrawEntityAddressableProperty" Assets          # expect: 0 matches
   grep -rn "UseAddressables" Assets/BroAudio               # unchanged, still wired
   ```
2. **Compile permutations (Unity Editor, manual):** open the project with
   - neither `com.unity.addressables` nor `com.unity.localization` installed,
   - Addressables installed,
   - Addressables + Localization installed.
   Confirm no errors in each (the `versionDefines` toggle `PACKAGE_ADDRESSABLES` /
   `PACKAGE_LOCALIZATION` — see runtime asmdef and `CLAUDE.md`).
3. **Editor smoke test (Unity):** open `Tools > BroAudio > Library Manager`,
   select an entity, confirm the Overall tab renders correctly with Toggle B gone
   and Toggle A on the Clips tab still toggles clip reference type without errors.
4. **Regression on Toggle A:** flip Clips-tab "Addressables" on/off and confirm
   clip reference conversion still works (`SwitchAddressable` /
   `ReorderableClips.ConvertReferences`).
5. Confirm new/changed `.cs` files have paired `.meta` (hook-generated) and all
   `.claude/settings.json` hooks pass.

---

## 6. Alternatives if removal is not safe/beneficial

- **Keep + relabel/tooltip.** If Toggle B is a real (if niche) feature for users
  doing external Addressables loading of entity assets, keep it but make the
  tooltip explicit: it registers *the asset itself* and does **not** affect how
  BroAudio loads clips (use the Clips-tab toggle for that), and warn about
  duplication when the asset is also reachable via the `Resources` graph.
  *(Tooltips for both toggles were added on branch
  `claude/addressable-toggles-tooltips-3igJP`; build on that if relabeling.)*
- **Gate behind a Preferences/advanced flag** so it's hidden by default.
- **Defer** pending a maintainer decision on whether the unfinished
  entity-address feature (`//entity.address = entity.name;`) was ever intended to
  ship.

---

## 7. Deliverables

- [ ] Branch with the removal (or relabel) commit(s), descriptive messages.
- [ ] PR **only if explicitly requested** by the maintainer.
- [ ] A short summary in the PR/notes covering: the safety checklist results, the
  user-data note (existing `AddressableAssetSettings` entries left intact), and the
  verification performed (and which steps require the Unity Editor and were not run
  headlessly).

---

## Appendix: key references (re-verify before use)

| Item | Location |
| --- | --- |
| Toggle B drawer | `Assets/BroAudio/Editor/EntityPropertyDrawer/AudioEntityEditor.AdditionalProperties.cs:89-167` |
| Toggle B call site | same file, `DrawAdditionalBaseProperties` (~`:76`) |
| Toggle A drawer | `Assets/BroAudio/Editor/EntityPropertyDrawer/AudioEntityEditor.cs:306-338` |
| Toggle A conversion | `AudioEntityEditor.cs` `SwitchAddressable` (~`:340`); `ReorderableClips.cs` `ConvertReferences` |
| `UseAddressables` field | `Assets/BroAudio/Runtime/DataStruct/Core/AudioEntity.Addressables.cs:13` |
| Runtime use of Toggle A | `SoundManager.Playback.cs:~103`, `SoundManager.Addressables.cs:~125` |
| Entity lookup (no Addressables) | `SoundManager.cs:506-528`, `SoundID.cs:18,53,223` |
| Resources prefab load | `SoundManager.cs:21` |
| Data hard-reference chain | `SoundManager.cs:69` → `BroAudioData.cs:20` → `AudioAsset.cs:154` |
| Entity is a ScriptableObject asset | `AudioEntity.cs:10`; created in `AudioAssetEditor.cs:329-340` |
