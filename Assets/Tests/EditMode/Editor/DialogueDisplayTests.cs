using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

// DialogueDisplay (charter Step 12) -- NoticeDisplay's uGUI idiom for speaker/text/portrait,
// MapScreen.Rebuild()'s instantiate-into-parent-loop for choice buttons. Follows
// MapScreenTests' exact template: private fields wired via TestReflectionUtil, spawned-instance
// list read via TestReflectionUtil, LogAssert.Expect for the edit-mode Destroy() error log.
public class DialogueDisplayTests
{
    private GameObject _go;
    private DialogueDisplay _display;

    private GameObject _panelRoot;
    private Text _speakerText;
    private Text _bodyText;
    private Image _portraitImage;
    private GameObject _choiceParentGo;
    private RectTransform _choiceParent;
    private GameObject _choiceButtonPrefabGo;
    private Button _choiceButtonPrefab;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("DialogueDisplay");
        _display = _go.AddComponent<DialogueDisplay>();

        _panelRoot = new GameObject("Panel");
        _panelRoot.SetActive(true);

        var speakerGo = new GameObject("Speaker", typeof(Text));
        _speakerText = speakerGo.GetComponent<Text>();

        var bodyGo = new GameObject("Body", typeof(Text));
        _bodyText = bodyGo.GetComponent<Text>();

        var portraitGo = new GameObject("Portrait", typeof(Image));
        _portraitImage = portraitGo.GetComponent<Image>();

        _choiceParentGo = new GameObject("ChoiceParent", typeof(RectTransform));
        _choiceParent = _choiceParentGo.GetComponent<RectTransform>();

        _choiceButtonPrefabGo = new GameObject("ChoiceButtonPrefab", typeof(RectTransform), typeof(Button));
        _choiceButtonPrefab = _choiceButtonPrefabGo.GetComponent<Button>();
        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGo.transform.SetParent(_choiceButtonPrefabGo.transform);

        TestReflectionUtil.SetField(_display, "panelRoot", _panelRoot);
        TestReflectionUtil.SetField(_display, "speakerText", _speakerText);
        TestReflectionUtil.SetField(_display, "bodyText", _bodyText);
        TestReflectionUtil.SetField(_display, "portraitImage", _portraitImage);
        TestReflectionUtil.SetField(_display, "choiceParent", _choiceParent);
        TestReflectionUtil.SetField(_display, "choiceButtonPrefab", _choiceButtonPrefab);
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);
        if (_panelRoot != null) Object.DestroyImmediate(_panelRoot);
        if (_speakerText != null) Object.DestroyImmediate(_speakerText.gameObject);
        if (_bodyText != null) Object.DestroyImmediate(_bodyText.gameObject);
        if (_portraitImage != null) Object.DestroyImmediate(_portraitImage.gameObject);
        if (_choiceParentGo != null) Object.DestroyImmediate(_choiceParentGo);
        if (_choiceButtonPrefabGo != null) Object.DestroyImmediate(_choiceButtonPrefabGo);
    }

    private static DialogueNode MakeNode(string speaker, string text)
    {
        return new DialogueNode { id = "n", speaker = speaker, text = text };
    }

    private System.Collections.Generic.List<GameObject> SpawnedButtons()
    {
        return TestReflectionUtil.GetField<System.Collections.Generic.List<GameObject>>(_display, "_spawnedChoiceButtons");
    }

    [Test]
    public void Show_ActivatesPanelRoot()
    {
        _panelRoot.SetActive(false);

        _display.Show(MakeNode("NPC", "Hello."), null);

        Assert.IsTrue(_panelRoot.activeSelf);
    }

    [Test]
    public void Show_SetsSpeakerAndBodyText()
    {
        _display.Show(MakeNode("Ashen Wanderer", "Ho there, traveler."), null);

        Assert.AreEqual("Ashen Wanderer", _speakerText.text);
        Assert.AreEqual("Ho there, traveler.", _bodyText.text);
    }

    [Test]
    public void Show_NoChoices_SpawnsNoButtons()
    {
        _display.Show(MakeNode("NPC", "Hello."), null);

        Assert.AreEqual(0, SpawnedButtons().Count);
    }

    [Test]
    public void Show_WithChoices_SpawnsOneButtonPerVisibleChoice()
    {
        var node = MakeNode("NPC", "Hello.");
        node.choices.Add(new DialogueChoice { text = "Ask", targetNodeId = "a" });
        node.choices.Add(new DialogueChoice { text = "Leave", targetNodeId = "" });

        _display.Show(node, null);

        Assert.AreEqual(2, SpawnedButtons().Count);
    }

    [Test]
    public void Show_ChoiceButtonLabel_SetToChoiceText()
    {
        var node = MakeNode("NPC", "Hello.");
        node.choices.Add(new DialogueChoice { text = "Ask about the shrine", targetNodeId = "a" });

        _display.Show(node, null);

        var button = SpawnedButtons()[0];
        var label = button.GetComponentInChildren<Text>();
        Assert.AreEqual("Ask about the shrine", label.text);
    }

    [Test]
    public void Show_ChoiceButtons_ParentedUnderChoiceParent()
    {
        var node = MakeNode("NPC", "Hello.");
        node.choices.Add(new DialogueChoice { text = "Ask", targetNodeId = "a" });

        _display.Show(node, null);

        var button = SpawnedButtons()[0];
        Assert.AreEqual(_choiceParent, button.transform.parent);
    }

    [Test]
    public void Show_CalledAgain_ClearsPreviousChoiceButtons()
    {
        var first = MakeNode("NPC", "Hello.");
        first.choices.Add(new DialogueChoice { text = "Ask", targetNodeId = "a" });
        _display.Show(first, null);
        Assert.AreEqual(1, SpawnedButtons().Count);

        UnityEngine.TestTools.LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex("Destroy may not be called from edit mode.*", System.Text.RegularExpressions.RegexOptions.Singleline));

        var second = MakeNode("NPC", "Bye.");
        second.choices.Add(new DialogueChoice { text = "A", targetNodeId = "a" });
        second.choices.Add(new DialogueChoice { text = "B", targetNodeId = "b" });
        _display.Show(second, null);

        Assert.AreEqual(2, SpawnedButtons().Count, "The list must reflect only the latest Show(), not accumulate across calls.");
    }

    [Test]
    public void Hide_DeactivatesPanelRoot()
    {
        _panelRoot.SetActive(true);

        _display.Hide();

        Assert.IsFalse(_panelRoot.activeSelf);
    }

    [Test]
    public void Hide_ClearsSpawnedChoiceButtons()
    {
        var node = MakeNode("NPC", "Hello.");
        node.choices.Add(new DialogueChoice { text = "Ask", targetNodeId = "a" });
        _display.Show(node, null);
        Assert.AreEqual(1, SpawnedButtons().Count);

        UnityEngine.TestTools.LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex("Destroy may not be called from edit mode.*", System.Text.RegularExpressions.RegexOptions.Singleline));

        _display.Hide();

        Assert.AreEqual(0, SpawnedButtons().Count);
    }

    [Test]
    public void Awake_CallsHide()
    {
        _panelRoot.SetActive(true);

        TestReflectionUtil.InvokeMethod(_display, "Awake");

        Assert.IsFalse(_panelRoot.activeSelf);
    }

    [Test]
    public void Show_NullNode_DoesNotThrow_DoesNotActivatePanel()
    {
        _panelRoot.SetActive(false);

        Assert.DoesNotThrow(() => _display.Show(null, null));
        Assert.IsFalse(_panelRoot.activeSelf);
    }

    [Test]
    public void Show_NullPanelRoot_DoesNotThrow()
    {
        TestReflectionUtil.SetField(_display, "panelRoot", null);

        Assert.DoesNotThrow(() => _display.Show(MakeNode("NPC", "Hello."), null));
    }

    [Test]
    public void Show_NullChoiceParentOrPrefab_DoesNotThrow_SpawnsNoButtons()
    {
        TestReflectionUtil.SetField(_display, "choiceParent", null);
        var node = MakeNode("NPC", "Hello.");
        node.choices.Add(new DialogueChoice { text = "Ask", targetNodeId = "a" });

        Assert.DoesNotThrow(() => _display.Show(node, null));
        Assert.AreEqual(0, SpawnedButtons().Count);
    }
}
