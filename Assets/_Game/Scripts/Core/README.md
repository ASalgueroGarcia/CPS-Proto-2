# Scripts / Core
Code shared by several features: Health, IBreakable, IHitFlash, CameraFollow, ActProgress, Managers/SoundManager.
Not here: anything used by only one feature - that goes in `Scripts/<that feature>`.
Owner: Dima (ask before adding). New file: if two features need it, it belongs here.

Save system (GDD > Gameplay > Save System): `Save/` (3 slots, JSON files in persistentDataPath),
`Settings/` (settings.json, applied to the mixer and screen), `Run/` (what a run start / death / victory
does to the save). Reach them through `Services`. The menus that use them live in `Scripts/Menus`.
