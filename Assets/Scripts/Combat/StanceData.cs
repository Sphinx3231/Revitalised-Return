using UnityEngine;

// TODO(Step 5): real tuning fields (baseDamageMultiplier, postureDamageMultiplier,
// attackSpeedScalar, parryWindowDuration).
[CreateAssetMenu(fileName = "NewStanceData", menuName = "Return/Stance Data")]
public class StanceData : ScriptableObject
{
    public string stanceName;
    public Sprite icon;
}
