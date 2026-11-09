using System;
using System.IO;

/// <summary>
/// A throwaway folder for save tests, so they never touch the real persistentDataPath.
/// Dispose in TearDown.
/// </summary>
public sealed class TempSaveFolder : IDisposable
{
    public string Path { get; }
    public JsonFileStore Store { get; }

    public TempSaveFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "spectracle-save-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
        Store = new JsonFileStore(Path);
    }

    public string FilePath(string fileName) => System.IO.Path.Combine(Path, fileName);

    public void Write(string fileName, string text) => File.WriteAllText(FilePath(fileName), text);

    public string ReadText(string fileName) => File.ReadAllText(FilePath(fileName));

    public bool Exists(string fileName) => File.Exists(FilePath(fileName));

    public string[] Files() => Directory.GetFiles(Path);

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, true);
        }
    }
}
