using UnityEngine;

/// <summary>
/// Single-responsibility owner of the player's current-stance state (charter S.O.L.I.D.
/// split). Does not read input directly — PlayerRoot calls CycleNext()/CyclePrevious() in
/// response to PlayerInputReader's unbuffered stance events. No Update() of its own.
/// </summary>
public sealed class StanceController : MonoBehaviour
{
    [SerializeField] private StanceData[] stances;

    private int _currentIndex = 0;

    /// <summary>The currently active stance, or null if no stances are configured.</summary>
    public StanceData CurrentStance => stances != null && stances.Length > 0 ? stances[_currentIndex] : null;

    public void CycleNext()
    {
        if (stances == null || stances.Length == 0)
            return;

        _currentIndex = (_currentIndex + 1) % stances.Length;
        EventBus.RaiseStanceSwapped(stances[_currentIndex]);
    }

    public void CyclePrevious()
    {
        if (stances == null || stances.Length == 0)
            return;

        _currentIndex = (_currentIndex - 1 + stances.Length) % stances.Length;
        EventBus.RaiseStanceSwapped(stances[_currentIndex]);
    }
}
