# Scripts / Core / Managers
Managers that span several features.
SoundManager: plays clips, applies the settings volumes to the mixer.
AudioManager: plays named sounds from SoundLibrary assets, and changes the mix at runtime: SetMusicPitch, SetMusicSpeed, SetSfxPitch, ResetPitchAndSpeed, SetPauseNoise. Limits and the noise clip are in Data/Audio/AudioTuning.
Not here: feature-local managers (WaveManager -> Waves/).
Owner: Antonio. New manager: keep the singleton pattern used by SoundManager.
