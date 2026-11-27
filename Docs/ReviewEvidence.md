# Review Evidence and Merge Danger

> Draft guideline proposed in PR #82. The goal is a small PR body that tells a reviewer what
> to check, what evidence exists, and what could make the change risky to merge.

## 1. Acceptance criteria

Write 3–8 criteria for the behavior the PR changes. Each criterion must include:

- **Behavior:** what the player or system should do.
- **Disproof:** the observation that would show the criterion is false.
- **Base result:** what happens on `dev` before the PR.
- **Evidence:** the test, PlayMode log, clip, screenshot or deterministic check that proves it.

Example:

```text
AC-1: Timed i-frames expire after 0.5 seconds.
Disproof: a second hit after 0.6 seconds leaves HP unchanged.
Base: dev has no timed i-frames.
Evidence: PlayMode HP log with timestamps; second hit reduces HP after the window.
```

## 2. Unity behavior map

Map only the paths this PR touches. Do not turn this into a test of the whole game.

| Feature | Entry point | Modes / variants | Paths to cover | Expected evidence |
|---|---|---|---|---|
| Combat | `SetScene`, `Player.prefab` | keyboard, gamepad; light, heavy, dash | hit registration, i-frames, death, pause, room reload | PlayMode log or short clip |
| Movement | `SetScene`, `PlayerConfig` | idle, run, dash, knockback | acceleration, stop, walls, interrupted attack | PlayMode log or clip |
| Enemies | room layout, enemy prefab | each changed enemy state | spawn, attack, burrow, death, room transition | PlayMode log or clip |
| Scene flow | menu, map, room door | normal entry, return, reload | loading, failure/retry, state restoration | scripted flow log |
| Save / load | save slot, run hooks | save, quit, reload, new run | success, cancel, missing/corrupt slot, schema migration | file diff plus runtime log |
| UI / shop | canvas, input mode | keyboard, gamepad, pointer | open, close, cancel, empty/full state | screenshot or input log |
| Performance | representative scene | normal and stress case | frame time, GC, physics/cloth cost | profiler capture with before/after numbers |

If a path is not testable, say why and name the manual check that covers it.

## 3. PR body template

```markdown
## Summary

One paragraph and, when useful, one small diagram or call tree.

## Acceptance criteria

- AC-1: ...
- AC-2: ...

## Evidence

- [ ] Compile: no new errors relative to `dev`
- [ ] EditMode tests: ...
- [ ] PlayMode / build: ...
- [ ] Before / after clip or log: ...

## Merge Danger

- **Door:** one-way / two-way
- **Blast radius:** scenes, prefabs, assets, saves, input modes, platforms
- **Migration:** none / serialized fields / save schema / asset GUIDs
- **Rollback:** what restores the previous state, including player data
- **Known risk:** the main thing a reviewer should try to break

## Out of scope / known issues

Anything deliberately not fixed in this PR.
```

## 4. Review rules

- Runtime claims need a test, PlayMode run, build run or log. Static claims name the
  deterministic check. Opinions are marked as suggestions or nits.
- The author implements and supplies evidence. The reviewer verifies and posts the review.
- A new commit does not clear an old **Changes requested** review. The reviewer posts a new
  review on the latest head after checking the fix.
- Keep unrelated features in separate PRs, even when the extra work is intentional and good.
