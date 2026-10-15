using System;
using System.IO;
using System.Text;

/// <summary>
/// Whole-file text storage in one folder (Application.persistentDataPath in the game, a temp
/// folder in tests). Writes go to a temp file first and then replace the real one, keeping
/// the previous version as a .bak - a crash mid-write can never leave a half-written save.
/// IO errors are thrown; the services decide what a failure means.
/// </summary>
public class JsonFileStore
{
    public const string BackupSuffix = ".bak";
    private const string TempSuffix = ".tmp";

    public string Folder { get; }

    public JsonFileStore(string folder)
    {
        Folder = folder;
    }

    public string PathOf(string fileName) => Path.Combine(Folder, fileName);

    public bool Exists(string fileName) => File.Exists(PathOf(fileName));

    public string Read(string fileName) => File.ReadAllText(PathOf(fileName), Encoding.UTF8);

    public void WriteAtomic(string fileName, string text)
    {
        Directory.CreateDirectory(Folder);

        string path = PathOf(fileName);
        string temp = path + TempSuffix;

        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
        {
            writer.Write(text);
            writer.Flush();
            stream.Flush(true);
        }

        if (File.Exists(path))
        {
            File.Replace(temp, path, path + BackupSuffix);
        }
        else
        {
            File.Move(temp, path);
        }
    }

    /// <summary>
    /// Moves an unreadable file aside (name.corrupt-yyyyMMdd-HHmmss) instead of deleting it,
    /// so a broken save can still be inspected or recovered by hand.
    /// </summary>
    public void Quarantine(string fileName)
    {
        string path = PathOf(fileName);
        if (!File.Exists(path)) return;

        string target = $"{path}.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}";
        for (int i = 1; File.Exists(target); i++)
        {
            target = $"{path}.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}-{i}";
        }

        File.Move(path, target);
    }

    /// <summary>Deletes the file together with its backup and any leftover temp file.</summary>
    public void Delete(string fileName)
    {
        // The main file goes last: if a locked backup stops the delete half-way, the slot is still
        // whole, instead of coming back from that backup one save older.
        string path = PathOf(fileName);
        File.Delete(path + TempSuffix);
        File.Delete(path + BackupSuffix);
        File.Delete(path);
    }
}
