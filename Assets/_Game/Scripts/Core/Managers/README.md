# Scripts / Core / Managers
Managers that span several features.
SoundManager: core public entry point for all audio — plays clips and named SoundLibrary sounds, syncs settings volumes to MasterAudioMixer, and controls runtime mix effects (SetMusicPitch, SetMusicSpeed, SetSfxPitch, ResetPitchAndSpeed, SetPauseNoise, SetMenuDistortion).
AudioManager: runtime mix/pool backend coordinated by SoundManager. Limits and filter/noise tuning live in Data/Audio/AudioTuning.
Not here: feature-local managers (WaveManager -> Waves/).
Owner: Antonio. New manager: keep the singleton pattern used by SoundManager.
