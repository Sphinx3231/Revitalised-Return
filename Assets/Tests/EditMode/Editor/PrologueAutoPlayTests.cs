using NUnit.Framework;
using UnityEngine;

public class PrologueAutoPlayTests
{
    [TearDown]
    public void TearDown()
    {
        GameState.SetState(GameState.State.Playing);
        SaveSystem.Current = null;
        SaveSystem.CurrentSlot = -1;
        SaveSystem.CurrentPlayerData = null;
    }

    [Test]
    public void Start_SetsGameStateToPlaying()
    {
        var go = new GameObject("PrologueAutoPlay");
        var component = go.AddComponent<PrologueAutoPlay>();

        GameState.SetState(GameState.State.Initializing);
        TestReflectionUtil.InvokeMethod(component, "Start");

        Assert.AreEqual(GameState.State.Playing, GameState.CurrentState);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Start_NoExistingSaveContext_SeedsOne()
    {
        SaveSystem.CurrentPlayerData = null;

        var go = new GameObject("PrologueAutoPlay");
        var component = go.AddComponent<PrologueAutoPlay>();

        TestReflectionUtil.InvokeMethod(component, "Start");

        Assert.IsNotNull(SaveSystem.CurrentPlayerData);
        Assert.IsNotNull(SaveSystem.Current);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Start_ExistingSaveContext_DoesNotOverwrite()
    {
        var existingData = new PlayerData { level = 7 };
        SaveSystem.CurrentPlayerData = existingData;

        var go = new GameObject("PrologueAutoPlay");
        var component = go.AddComponent<PrologueAutoPlay>();

        TestReflectionUtil.InvokeMethod(component, "Start");

        Assert.AreSame(existingData, SaveSystem.CurrentPlayerData);

        Object.DestroyImmediate(go);
    }
}
