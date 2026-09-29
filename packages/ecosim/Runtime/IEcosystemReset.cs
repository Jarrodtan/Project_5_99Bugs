/// <summary>
/// Resets simulation state when the island swaps. Full reset — no entity remapping.
/// (Moved here from the app's IslandPlatformInterfaces so EcoSim has no app dependency.)
/// </summary>
public interface IEcosystemReset
{
    /// <summary>Clears and re-initializes ecosystem state for a new island.</summary>
    void ResetEcosystem();
}
