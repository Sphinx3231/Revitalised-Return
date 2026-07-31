using NUnit.Framework;
using UnityEngine;

public class MainMenuAutoStateTests
{
    [TearDown]
    public void TearDown()
    {
        GameState.SetState(GameState.State.Playing);
    }

    [Test]
    public void Start_SetsGameStateToMainMenu()
    {
        var go = new GameObject("MainMenuAutoState");
        var component = go.AddComponent<MainMenuAutoState>();

        GameState.SetState(GameState.State.Playing);
        TestReflectionUtil.InvokeMethod(component, "Start");

        Assert.AreEqual(GameState.State.MainMenu, GameState.CurrentState);

        Object.DestroyImmediate(go);
    }
}
