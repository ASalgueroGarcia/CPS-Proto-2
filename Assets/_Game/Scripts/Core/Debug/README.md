# Scripts / Core / Debug
Runtime dev cheats panel, toggle with F1. Compiles only in editor and
development builds (`UNITY_EDITOR || DEVELOPMENT_BUILD`) - never in release.
Not here: anything needed by the shipped game.
Owner: Dima (ask before editing)
Behavior: self-spawns via RuntimeInitializeOnLoadMethod, no scene/prefab
wiring. Room jumps, wave skips, coins, heal, timescale. Routes through
the same public game APIs (SceneController, PlayerStatsManager) - when
pause gets unified, route the timescale buttons through that API.
