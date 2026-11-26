# Repo Guidelines — Branches, PRs & Ownership

One page. `CODEOWNERS` is the authoritative path → owner map (matches `Docs/RepoLayout.md` §7).

## 1. Branches — push your own, only your own

| Action | Allowed? |
|---|---|
| Push to a branch you created for your work | ✅ |
| Push anything to someone else's branch | ❌ never |
| Force-push someone else's branch | ❌ NEVER, under any circumstances |
| Force-push your own feature branch (history cleanup) | ⚠️ allowed, announce it in the team chat |

**Branch names:** keep the existing `feature/<name>` / `fix/<name>` style. Stack layers use `feature/<name>/1-<layer>`, `feature/<name>/2-<layer>` (or `fix/...`); immutable playtest snapshots use `playtest/<name>-<YYYY-MM-DD>`.

Need a change in code you don't own? Don't push. Use one of the three routes below.

## 2. Ownership map

`CODEOWNERS` is the single source of truth — summary:

| Owner | Areas |
|---|---|
| @puntusovdima | Player (with @mariozetus), Waves, Pickups, Core, player/pickup prefabs, character art (except Enemies), Menus (with @IvanOoff) |
| @mariozetus | Player (with @puntusovdima), Enemies, enemy data/prefabs/art, dialog system |
| @ASalgueroGarcia | Map, Core/Managers, Audio, **all Scenes**, map art |
| @IvanOoff | Shop, UI, Obstacles code, item data, shop/UI prefabs, UI art |

## 3. You need something in code you don't own?

| Route | When to use |
|---|---|
| Open an **issue** | Default. The owner does the work |
| Open a **PR** against the main line | You did the work, the owner reviews it |
| Open a **native stacked PR** targeting the owner's feature branch | Your change depends on their in-flight work |

Draft PRs are welcome when the author is not ready for review; they are not required for cross-owner changes.

In every case: the code owner's review + **APPROVAL is obligatory** before merge. The owner is free to close a PR built for them ("ready to die" PRs are fine — say so in the PR body).

## 4. One PR per feature — stacks are the exception

**Default:** one PR = one feature. Split the work into clear commits (code, animations, prefabs, VFX, tests) and review commit-by-commit. Different disciplines contributing to one feature is not a reason for separate PRs.

**Exception:** use a **native GitHub stack** only when layers must merge separately (cross-owner dependency, prerequisite refactor, long-running work). The trunk is `dev`; create/link it with the GitHub stack UI or `gh stack submit`.

- The author owns the whole stack and merges bottom-up.
- Every PR in a stack names the actual tip branch, e.g. `Playtest branch: feature/character-animation/2-assets`.
- Designers check out the tip only; it contains the whole feature.
- During active playtests, publish an immutable `playtest/<name>-<YYYY-MM-DD>` snapshot branch instead of making designers follow a moving tip.
- Native stacks rebase/force-push remaining branches after partial merges. If GitHub Desktop shows a stack branch as ahead/behind, do not click Pull: switch away, delete the local branch, fetch, and check it out again.

**For the PR author / merger (CLI only):** never run `gh pr merge --delete-branch` on a hand-rolled stack base with open child PRs — it can close them. Merge without deletion, retarget each child to `dev`, then delete the old base. GitHub Desktop users do not run this command; Desktop is for branch checkout, while PR merges happen on GitHub.com or via `gh`.

## 5. Enforcing approvals (optional)

Today the approval rule above is a team agreement; nothing blocks a merge. If we want GitHub to enforce it, the repo admin (Antonio) can turn on the built-in option: **Settings → Branches → Add branch protection rule** on `dev` → **Require a pull request before merging** → **Require review from Code Owners**. The repo is public, so this is free.

Note: GitHub then asks for an owner's approval for every owned path a PR touches, so a PR that crosses two owners' areas needs both.

## 6. Hygiene

- **Commits:** one clear message per change — what + why in one line
- **PR titles:** descriptive ("Phase 5 structural: shared AOE helper, event-driven HUD" — not "stuff")
- **Draft PRs welcome** — the author flips to ready when reviewable
- **Disposables welcome** — mark them in the PR body so nobody merges reluctantly
- **PR size:** one feature per PR; keep it reviewable with clean commits. Split PRs only when layers must merge separately (see §4)
- **Review comments say how they were checked:** a claim about behaviour ("X goes through Y", "this breaks Z") says whether it was tested in play mode / by a test, or is from reading the code. Opinions are marked as suggestions or nits
