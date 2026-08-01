/// <summary>
/// Dependency-inversion seam for anything that exposes a "current combat stance" (charter
/// S.O.L.I.D. split, matching the existing IMovementInput/IStaminaSource pattern). Implemented
/// by StanceController (the player's real stance) and BossStanceMirror (a boss's Phase-2
/// "mirror the player's stance" enrage behavior) so AttackController/WeaponHitbox never need
/// to know which kind of source they're reading from.
/// </summary>
public interface IStanceSource
{
    StanceData CurrentStance { get; }
}
