# Spectracle
Top-down arena roguelite prototype. Unity **6000.3.5f2**.

## Open the project
Unity Hub -> Unity 6000.3.5f2 -> Add project from disk -> this folder.

## Where things are
- All game content: `Assets/_Game` - start at [Assets/_Game/README.md](Assets/_Game/README.md)
- Your personal test scenes: `Assets/_Sandbox/<your name>/`
- Asset Store packs: `Assets/ThirdParty` - never edit in place, copy into `_Game`
- Full folder rules: [Docs/RepoLayout.md](Docs/RepoLayout.md)

## Workflow
1. Never commit to `main` directly. One branch per feature: `feature/<name>`.
2. Open a PR (draft until ready). CODEOWNERS requests review from the folder owner.
3. Unity `.meta` files always commit together with their asset - move files inside Unity, or move file + .meta together.
4. Rebase on the latest `main` before asking for review.
