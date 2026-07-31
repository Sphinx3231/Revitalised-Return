/// <summary>
/// Dependency-inversion seam for anything that spends stamina (charter S.O.L.I.D. split,
/// matching the existing IMovementInput/IInvulnerabilityProvider pattern). Implemented by
/// PlayerVitals; consumed by DodgeAbility so it never tracks its own stamina copy.
/// </summary>
public interface IStaminaSource
{
    bool TrySpend(float amount);
}
