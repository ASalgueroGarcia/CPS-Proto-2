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
| @puntusovdima | Player (with @Mariozetus), Enemies, Waves, Pickups, Core, enemy data, player/enemy/pickup prefabs, character art, Menus (with @IvanOoff) |
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

## 4. Enforcing approvals (optional)

Today the approval rule above is a team agreement; nothing blocks a merge. If we want GitHub to enforce it, the repo admin (Antonio) can turn on the built-in option: **Settings → Branches → Add branch protection rule** on `dev` → **Require a pull request before merging** → **Require review from Code Owners**. The repo is public, so this is free.

Note: GitHub then asks for an owner's approval for every owned path a PR touches, so a PR that crosses two owners' areas needs both.

## 5. Hygiene

- **Commits:** one clear message per change — what + why in one line
- **PR titles:** descriptive ("Phase 5 structural: shared AOE helper, event-driven HUD" — not "stuff")
- **Draft PRs welcome** — the author flips to ready when reviewable
- **Disposables welcome** — mark them in the PR body so nobody merges reluctantly
- **Small PRs:** one concern per PR; split by owner area so one contested file can't hold the rest back
- **Review comments say how they were checked:** a claim about behaviour ("X goes through Y", "this breaks Z") says whether it was tested in play mode / by a test, or is from reading the code. Opinions are marked as suggestions or nits
