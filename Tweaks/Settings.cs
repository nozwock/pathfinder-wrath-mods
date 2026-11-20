using UnityModManagerNet;

namespace Nozwock.Tweaks;

public class Settings : UnityModManager.ModSettings
{
    public bool NoFriendlyFire = true;

    public bool InfiniteArmyMovement = false;

    public override void Save(UnityModManager.ModEntry modEntry)
    {
        Save(this, modEntry);
    }
}
