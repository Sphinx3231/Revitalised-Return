using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Single-responsibility: displays EventBus.ShowNotice only (toast text, auto-hides after
// `duration`). Uses legacy UnityEngine.UI.Text since TextMeshPro is not yet a project
// dependency (com.unity.ugui only) — see docs/Tasks/2026-07-31-ui-systems-phase2.md.
public class NoticeDisplay : MonoBehaviour
{
    [SerializeField] private Text noticeText;

    private Coroutine _hideRoutine;

    private void Awake()
    {
        if (noticeText != null)
            noticeText.text = string.Empty;
    }

    private void OnEnable()
    {
        EventBus.ShowNotice += HandleNotice;
    }

    private void OnDisable()
    {
        EventBus.ShowNotice -= HandleNotice;

        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }
    }

    private void HandleNotice(string text, float duration)
    {
        if (noticeText == null)
            return;

        noticeText.text = text;

        if (_hideRoutine != null)
            StopCoroutine(_hideRoutine);
        _hideRoutine = StartCoroutine(ClearAfterDelay(duration));
    }

    private IEnumerator ClearAfterDelay(float duration)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, duration));
        if (noticeText != null)
            noticeText.text = string.Empty;
        _hideRoutine = null;
    }
}
