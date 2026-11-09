using System.IO;
using System.Linq;
using NUnit.Framework;

/// <summary>The atomic write is what keeps a crash mid-save from destroying the slot.</summary>
public class JsonFileStoreTests
{
    private TempSaveFolder _folder;

    [SetUp]
    public void SetUp() => _folder = new TempSaveFolder();

    [TearDown]
    public void TearDown() => _folder.Dispose();

    [Test]
    public void Write_ThenRead_ReturnsTheSameText()
    {
        _folder.Store.WriteAtomic("a.json", "{\"x\":1}");

        Assert.AreEqual("{\"x\":1}", _folder.Store.Read("a.json"));
    }

    [Test]
    public void SecondWrite_KeepsThePreviousVersionAsBackup()
    {
        _folder.Store.WriteAtomic("a.json", "first");
        _folder.Store.WriteAtomic("a.json", "second");

        Assert.AreEqual("second", _folder.ReadText("a.json"));
        Assert.AreEqual("first", _folder.ReadText("a.json" + JsonFileStore.BackupSuffix));
    }

    [Test]
    public void Write_LeavesNoTempFileBehind()
    {
        _folder.Store.WriteAtomic("a.json", "first");
        _folder.Store.WriteAtomic("a.json", "second");

        Assert.IsFalse(_folder.Files().Any(f => f.EndsWith(".tmp")));
    }

    [Test]
    public void Write_CreatesTheFolderIfMissing()
    {
        var store = new JsonFileStore(Path.Combine(_folder.Path, "nested", "deeper"));

        store.WriteAtomic("a.json", "x");

        Assert.IsTrue(store.Exists("a.json"));
    }

    [Test]
    public void MissingFile_DoesNotExist()
    {
        Assert.IsFalse(_folder.Store.Exists("nothing.json"));
    }

    [Test]
    public void Quarantine_MovesTheFileAside()
    {
        _folder.Write("a.json", "broken");

        _folder.Store.Quarantine("a.json");

        Assert.IsFalse(_folder.Exists("a.json"));
        string moved = _folder.Files().Single();
        StringAssert.Contains("a.json.corrupt-", moved);
        Assert.AreEqual("broken", File.ReadAllText(moved));
    }

    [Test]
    public void Delete_RemovesFileAndBackup()
    {
        _folder.Store.WriteAtomic("a.json", "first");
        _folder.Store.WriteAtomic("a.json", "second");

        _folder.Store.Delete("a.json");

        Assert.IsEmpty(_folder.Files());
    }
}
