# Known Behavior Quirks

Bugs and surprising-but-real behaviors found while writing the Layer 1 characterization suite
(`Assets/Tests/`, see `Docs/VERIFICATION_PLAN.md`). Per that plan's guiding principle #2
("characterize, don't correct"), these are pinned as-is rather than fixed mid-snapshot, so that
refactor regressions and intentional behavior changes stay distinguishable. Fix or intentionally
change one of these only in its own commit, updating the corresponding entry here.

## Shuffle mode doesn't strictly avoid repeats within a round

`Runtime/Utility/ClipSelection/ShuffleClipStrategy.cs` — `MulticlipsPlayMode.Shuffle`.

The coverage matrix's own wording ("Shuffle (no repeats within a round)") describes the intent, but
`ShuffleClipStrategy.Use()` only rejects a candidate pick when it equals `_lastUsed` — the single
clip that ended the *previous* round. It does not check membership in `_used` (the set of clips
already picked *this* round). `_lastUsed` itself is only ever assigned at a round boundary, not
after every pick. As a result, a fast-path pick partway through a round can legitimately re-select
a clip already used earlier in that same round; only the transition *between* rounds is guaranteed
non-repeating (the round's last pick vs. the next round's first pick).

`Assets/Tests/Scenarios/ClipSelection/ClipSelectionScenarios.cs`'s `ShuffleClipSelectionScenario`
asserts the weaker, actually-true property instead (every clip appears at least once over enough
plays), to avoid a flaky test built on a false assumption.

## `LayeredClipStrategy` is unreachable from `MulticlipsPlayMode`

`Runtime/Utility/ClipSelection/LayeredClipStrategy.cs` implements `IClipSelectionStrategy`, but
`MulticlipsPlayMode` (`Runtime/Enums/MulticlipsPlayMode.cs`) has no `Layered` value, and
`AudioEntity.PickNewClip`'s mode switch never constructs a `LayeredClipStrategy`. The type compiles
and looks like a selectable strategy, but nothing in the public API can currently select it.

The coverage matrix lists "Layered" as one of the clip-selection strategies to verify; it's left
unchecked/waived in `Docs/VERIFICATION_PLAN.md` rather than given a scenario, since there's no way
to exercise it through the public contract as it stands today.
