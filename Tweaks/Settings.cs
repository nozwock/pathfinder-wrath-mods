using UnityModManagerNet;

namespace Nozwock.Tweaks;

public class Settings : UnityModManager.ModSettings
{
    public bool NoFriendlyFire = true;

    public bool InfiniteArmyMovement = false;
    public bool VillageTeleportCircle = true;
    public bool NoTeleportCooldown = true;

    public bool DisableWeatherVfx = false;
    public bool DisableAmbientParticles = false;
    public bool DisableNonEssentialCombatVfx = false;
    public bool DisableCosmeticDecals = false;

    public bool DisableClothPhysics = false;
    public bool HideAllCapes = false;

    // XXX Maybe consider downscaling other atlases that game doesn't such as accessories, weapons, capes, etc.
    public bool FixAtlasDoubleDownscale = true;

    public override void Save(UnityModManager.ModEntry modEntry)
    {
        Save(this, modEntry);
    }
}