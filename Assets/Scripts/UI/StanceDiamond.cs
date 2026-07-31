using UnityEngine;
using UnityEngine.UI;

// Single-responsibility: displays EventBus.StanceSwapped only, highlighting the matching icon
// in a 4-icon diamond layout (bottom-right per charter Step 11). See
// docs/Tasks/2026-07-31-ui-systems-phase2.md.
public class StanceDiamond : MonoBehaviour
{
    [SerializeField] private StanceData[] stanceOrder = new StanceData[4];
    [SerializeField] private Image[] icons = new Image[4];

    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color dimmedColor = new Color(1f, 1f, 1f, 0.4f);

    private void OnEnable()
    {
        EventBus.StanceSwapped += HandleStanceSwapped;
    }

    private void OnDisable()
    {
        EventBus.StanceSwapped -= HandleStanceSwapped;
    }

    private void HandleStanceSwapped(StanceData stance)
    {
        int selectedIndex = stance != null ? System.Array.IndexOf(stanceOrder, stance) : -1;

        for (int i = 0; i < icons.Length; i++)
        {
            if (icons[i] == null)
                continue;

            icons[i].color = i == selectedIndex ? selectedColor : dimmedColor;
        }
    }
}
