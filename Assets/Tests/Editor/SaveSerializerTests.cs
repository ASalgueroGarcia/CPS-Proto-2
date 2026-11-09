using NUnit.Framework;

/// <summary>
/// Reading a slot file must never throw and never trust the file: JsonUtility replaces arrays
/// wholesale and turns "null" into defaults, so these guard the repairs SaveData.Parse makes.
/// </summary>
public class SaveSerializerTests
{
    [Test]
    public void RoundTrip_KeepsEveryField()
    {
        SaveData original = SaveData.CreateDefault();
        original.tickets = 24;
        original.bossTickets = 3;
        original.ownedUpgradeIds.Add("spare_stuffing");
        original.scenesCleared[0] = 3;
        original.scenesCleared[1] = 1;
        original.replayScene[0] = 2;
        original.takes[4] = 5;
        original.firstLaunchDone = true;
        original.lastSavedUtcTicks = 638640000000000000L;

        Assert.AreEqual(SaveParseResult.Ok, SaveData.Parse(SaveSerializer.ToJson(original), out SaveData copy));

        Assert.AreEqual(24, copy.tickets);
        Assert.AreEqual(3, copy.bossTickets);
        CollectionAssert.AreEqual(new[] { "spare_stuffing" }, copy.ownedUpgradeIds);
        CollectionAssert.AreEqual(new[] { 3, 1, 0 }, copy.scenesCleared);
        CollectionAssert.AreEqual(new[] { 2, 0, 0 }, copy.replayScene);
        Assert.AreEqual(5, copy.takes[4]);
        Assert.IsTrue(copy.firstLaunchDone);
        Assert.AreEqual(638640000000000000L, copy.lastSavedUtcTicks);
    }

    [Test]
    public void MissingFields_KeepTheirDefaults()
    {
        Assert.AreEqual(SaveParseResult.Ok, SaveData.Parse("{\"version\":1,\"tickets\":7}", out SaveData data));

        Assert.AreEqual(7, data.tickets);
        Assert.AreEqual(ProgressRules.ActCount, data.scenesCleared.Length);
        Assert.AreEqual(ProgressRules.SceneCount, data.takes.Length);
        Assert.IsNotNull(data.ownedUpgradeIds);
        Assert.IsFalse(data.firstLaunchDone);
    }

    [Test]
    public void ShortArrays_AreResizedAndKeepTheirValues()
    {
        SaveData.Parse("{\"version\":1,\"scenesCleared\":[2],\"takes\":[1,2]}", out SaveData data);

        CollectionAssert.AreEqual(new[] { 2, 0, 0 }, data.scenesCleared);
        Assert.AreEqual(ProgressRules.SceneCount, data.takes.Length);
        Assert.AreEqual(1, data.takes[0]);
        Assert.AreEqual(2, data.takes[1]);
    }

    [Test]
    public void OutOfRangeValues_AreClamped()
    {
        SaveData.Parse(
            "{\"version\":1,\"tickets\":-5,\"bossTickets\":-1,\"scenesCleared\":[9,-2,1],\"replayScene\":[7,0,-1],\"takes\":[-3]}",
            out SaveData data);

        Assert.AreEqual(0, data.tickets);
        Assert.AreEqual(0, data.bossTickets);
        CollectionAssert.AreEqual(new[] { 3, 0, 1 }, data.scenesCleared);
        CollectionAssert.AreEqual(new[] { 2, 0, 0 }, data.replayScene);
        Assert.AreEqual(0, data.takes[0]);
    }

    [Test]
    public void UpgradeIds_DropEmptyAndDuplicates()
    {
        SaveData.Parse("{\"version\":1,\"ownedUpgradeIds\":[\"a\",\"\",\" \",\"a\",\"b\"]}", out SaveData data);

        CollectionAssert.AreEqual(new[] { "a", "b" }, data.ownedUpgradeIds);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("{")]
    [TestCase("null")]
    [TestCase("[]")]
    [TestCase("{\"tickets\":\"lots\"")]
    [TestCase("not json at all")]
    public void DamagedText_IsCorrupt_WithoutThrowing(string json)
    {
        Assert.AreEqual(SaveParseResult.Corrupt, SaveData.Parse(json, out SaveData data));
        Assert.IsNull(data);
    }

    [Test]
    public void FileWithoutVersion_IsReadAsTheCurrentFormat()
    {
        Assert.AreEqual(SaveParseResult.Ok, SaveData.Parse("{\"tickets\":2}", out SaveData data));

        Assert.AreEqual(SaveData.CurrentVersion, data.version);
        Assert.AreEqual(2, data.tickets);
    }

    [Test]
    public void FileFromANewerBuild_IsFlagged_ButStillRead()
    {
        string json = "{\"version\":" + (SaveData.CurrentVersion + 1) + ",\"tickets\":9,\"futureField\":true}";

        Assert.AreEqual(SaveParseResult.NewerVersion, SaveData.Parse(json, out SaveData data));
        Assert.AreEqual(9, data.tickets);
    }
}
