using UnityEngine;

[CreateAssetMenu(fileName = "NewStanceData", menuName = "Return/Stance Data")]
public class StanceData : ScriptableObject
{
    public string stanceName;
    public Sprite icon;

    // Combat tuning fields (charter 5.1).
    public float baseDamageMultiplier = 1.0f;
    public float postureDamageMultiplier = 1.0f;
    public float attackSpeedScalar = 1.0f;
    public float parryWindowDuration = 0.12f;
}
