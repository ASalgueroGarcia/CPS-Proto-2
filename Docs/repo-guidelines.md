# Repo Guidelines — Branches, PRs & Ownership

One page. `CODEOWNERS` is the authoritative path → owner map (matches `Docs/RepoLayout.md` §7).

## 1. Branches — push your own, only your own

| Action | Allowed? |
|---|---|
| Push to a branch you created for your work | ✅ |
| Push anything to someone else's branch | ❌ never |
| Force-push someone else's branch | ❌ NEVER, under any circumstances |
| Force-push your own feature branch (history cleanup) | ⚠️ allowed, announce it in the team chat |

Need a change in code you don't own? Don't push. Use one of the three routes below.

## 2. Ownership map

`CODEOWNERS` is the single source of truth — summary:

| Owner | Areas |
|---|---|
| @puntusovdima | Player, Enemies, Waves, Pickups, Core, player/enemy/pickup prefabs, character art |
| @ASalgueroGarcia | Map, Core/Managers, Audio, **all Scenes**, map art |
| @IvanOoff | Shop, UI, Obstacles code, item data, shop/UI prefabs, UI art |
| @AnaLopezLanda | Rooms, environment art, obstacle prefabs |

## 3. You need something in code you don't own?

| Route | When to use |
|---|---|
| Open an **issue** | Default. The owner does the work |
| Open a **draft PR** against the main line | You did the work, the owner reviews it |
| Open a **stacked PR** targeting the owner's feature branch | Your change depends on their in-flight work |

In every case: the code owner's review + **APPROVAL is obligatory** before merge. The owner is free to close a PR built for them ("ready to die" PRs are fine — say so in the PR body).

## 4. The approval gate (on request)

The `Code-owner approval gate` workflow (`.github/workflows/codeowner-approval.yml`) runs **only when requested**: Actions → *Code-owner approval gate* → *Run workflow* → PR number. Kalashnikov-simple — **exactly one approval is ever required**:

- sums the **changed lines** (additions + deletions) inside each owner's paths
- the single **most-touched owner** (if their paths carry ≥ 10 changed lines) must have an **APPROVED review** — nobody else is asked
- the PR author cannot cover themselves; if the most-touched paths are the author's own, the gate passes
- below the threshold, or no owned paths at all → gate passes, zero approvals

The automatic triggers (run on every PR / every review) are **disabled and preserved commented** in the workflow file — uncomment the two trigger blocks to restore them. To make requested runs blocking, the repo owner (Antonio — only the repo admin can) opens **Settings → Branches → Add branch protection rule** on the default branch → **Require status checks** → select `Code-owner approval gate`.

## 5. Hygiene

- **Commits:** one clear message per change — what + why in one line
- **PR titles:** descriptive ("Phase 5 structural: shared AOE helper, event-driven HUD" — not "stuff")
- **Draft PRs welcome** — the author flips to ready when reviewable
- **Disposables welcome** — mark them in the PR body so nobody merges reluctantly

## 6. Free-tier reality

- No organization → no team owners and no built-in required-reviews automation; that's why the gate is a plain Python job + branch protection (free on public repos, flipped by the repo admin)
- Public repo → GitHub Actions minutes are free; the gate runs in seconds
