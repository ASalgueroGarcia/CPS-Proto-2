using NUnit.Framework;

/// <summary>Act unlocks and which scene a run plays (GDD: three acts of three scenes each).</summary>
public class ProgressRulesTests
{
    private static SaveData Cleared(int actOne, int actTwo = 0, int actThree = 0)
    {
        SaveData data = SaveData.CreateDefault();
        data.scenesCleared[0] = actOne;
        data.scenesCleared[1] = actTwo;
        data.scenesCleared[2] = actThree;
        return data;
    }

    [Test]
    public void ActOne_IsAlwaysOpen_AndLaterActsOpenWhenThePreviousIsFinished()
    {
        Assert.IsTrue(ProgressRules.IsUnlocked(Cleared(0), 0));
        Assert.IsFalse(ProgressRules.IsUnlocked(Cleared(2), 1));
        Assert.IsTrue(ProgressRules.IsUnlocked(Cleared(3), 1));
        Assert.IsFalse(ProgressRules.IsUnlocked(Cleared(3), 2));
        Assert.IsFalse(ProgressRules.IsUnlocked(Cleared(3, 3, 3), 3), "There is no fourth act.");
    }

    [Test]
    public void SceneToPlay_IsTheFirstSceneNotClearedYet()
    {
        Assert.AreEqual(0, ProgressRules.SceneToPlay(Cleared(0), 0));
        Assert.AreEqual(2, ProgressRules.SceneToPlay(Cleared(2), 0));
    }

    [Test]
    public void FinishedAct_IsReplayedFromSceneOne()
    {
        Assert.AreEqual(0, ProgressRules.SceneToPlay(Cleared(3), 0));

        SaveData midReplay = Cleared(3);
        midReplay.replayScene[0] = 1;
        Assert.AreEqual(1, ProgressRules.SceneToPlay(midReplay, 0));
    }

    [Test]
    public void DefaultAct_IsTheFirstOpenUnfinishedAct_OrTheLastOneWhenAllAreDone()
    {
        Assert.AreEqual(0, ProgressRules.DefaultAct(Cleared(1)));
        Assert.AreEqual(1, ProgressRules.DefaultAct(Cleared(3, 0)));
        Assert.AreEqual(2, ProgressRules.DefaultAct(Cleared(3, 3, 1)));
        Assert.AreEqual(2, ProgressRules.DefaultAct(Cleared(3, 3, 3)));
    }

    [Test]
    public void NoSave_ReadsAsAFreshStart()
    {
        Assert.AreEqual(0, ProgressRules.DefaultAct(null));
        Assert.IsTrue(ProgressRules.IsUnlocked(null, 0));
        Assert.IsFalse(ProgressRules.IsUnlocked(null, 1));
        Assert.AreEqual(0, ProgressRules.SceneToPlay(null, 0));
    }

    [Test]
    public void TakeIndex_IsActTimesThreePlusScene()
    {
        Assert.AreEqual(0, ProgressRules.TakeIndex(0, 0));
        Assert.AreEqual(5, ProgressRules.TakeIndex(1, 2));
        Assert.AreEqual(8, ProgressRules.TakeIndex(2, 2));
    }
}
