using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dialogue UI (charter Step 12) -- NoticeDisplay's uGUI idiom for speaker/text/portrait
/// fields, MapScreen.Rebuild()'s instantiate-into-parent-loop pattern for choice buttons
/// (spawned instances tracked in a List, destroyed before the next Show()). Driven entirely by
/// DialogueRunner -- this class owns no dialogue-graph logic of its own (S.O.L.I.D.: display is
/// display, traversal is DialogueRunner's job).
/// </summary>
public class DialogueDisplay : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Text speakerText;
    [SerializeField] private Text bodyText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private RectTransform choiceParent;
    [SerializeField] private Button choiceButtonPrefab;

    private readonly List<GameObject> _spawnedChoiceButtons = new List<GameObject>();

    private void Awake()
    {
        Hide();
    }

    /// <summary>Displays `node`'s speaker/text/portrait and spawns one button per visible choice, wired to call `runner.SelectChoice`.</summary>
    public void Show(DialogueNode node, DialogueRunner runner)
    {
        if (node == null)
            return;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (speakerText != null)
            speakerText.text = node.speaker;

        if (bodyText != null)
            bodyText.text = node.text;

        if (portraitImage != null)
            portraitImage.sprite = node.portrait;

        RebuildChoices(node, runner);
    }

    /// <summary>Hides the panel and clears any spawned choice buttons.</summary>
    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        ClearChoiceButtons();
    }

    private void RebuildChoices(DialogueNode node, DialogueRunner runner)
    {
        ClearChoiceButtons();

        if (choiceParent == null || choiceButtonPrefab == null)
            return;

        List<DialogueChoice> visible = DialogueRunner.VisibleChoices(node);

        for (int i = 0; i < visible.Count; i++)
        {
            DialogueChoice choice = visible[i];
            Button buttonInstance = Instantiate(choiceButtonPrefab, choiceParent);

            // Stacks buttons top-to-bottom by index. Deliberately done here (not via a
            // UnityEngine.UI.VerticalLayoutGroup on choiceParent) -- this scene-wiring pass
            // has no live Editor available to verify a hand-authored built-in-package script
            // GUID reference, and a wrong GUID would silently produce a missing-script
            // reference. A manual offset needs no such reference and is directly testable.
            RectTransform rect = buttonInstance.GetComponent<RectTransform>();
            if (rect != null)
                rect.anchoredPosition = new Vector2(0f, -i * (rect.sizeDelta.y + 6f));

            Text label = buttonInstance.GetComponentInChildren<Text>();
            if (label != null)
                label.text = choice.text;

            DialogueChoice capturedChoice = choice;
            DialogueRunner capturedRunner = runner;
            buttonInstance.onClick.AddListener(() => capturedRunner?.SelectChoice(capturedChoice));

            _spawnedChoiceButtons.Add(buttonInstance.gameObject);
        }
    }

    private void ClearChoiceButtons()
    {
        for (int i = 0; i < _spawnedChoiceButtons.Count; i++)
        {
            if (_spawnedChoiceButtons[i] != null)
                Destroy(_spawnedChoiceButtons[i]);
        }
        _spawnedChoiceButtons.Clear();
    }
}
