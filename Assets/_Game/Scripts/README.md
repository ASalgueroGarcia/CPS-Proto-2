# Scripts
Code by feature, not by person. Same feature names as `Prefabs/`.
Each feature folder is its own assembly (`Spectracle.<Feature>.asmdef`), see Docs/AssemblyDefinitions.md.
Editor tools go in `Scripts/Editor/` (no asmdef), never in a feature's own `Editor/` subfolder:
inside a feature folder that one compiles into the game and breaks the build.
Owner: see each feature folder. Naming: normal C#, one class per file, file name = class name.
