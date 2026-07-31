using NUnit.Framework;
using UnityEngine;

public class SandboxAutoPlayTests
{
    [TearDown]
    public void TearDown()
    {
        GameState.SetState(GameState.State.Playing);
    }

    [Test]
    public void Start_SetsGameStateToPlaying()
    {
        var go = new GameObject("SandboxAutoPlay");
        var component = go.AddComponent<SandboxAutoPlay>();

        GameState.SetState(GameState.State.MainMenu);
        TestReflectionUtil.InvokeMethod(component, "Start");

        Assert.AreEqual(GameState.State.Playing, GameState.CurrentState);

        Object.DestroyImmediate(go);
    }
}
