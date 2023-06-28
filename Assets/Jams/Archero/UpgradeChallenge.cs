using UnityEngine;

namespace Archero {
  // Upgrade that unlocks if player clears a room without taking damage.
  [CreateAssetMenu(fileName = "Upgrade", menuName = "ArcheroUpgrade/Challenge")]
  public class UpgradeChallenge : Upgrade {
    public Upgrade UnlockOnSuccess;
    public bool IsMaxed(Upgrades us) => (us.GetUpgradeData(UnlockOnSuccess)?.CurrentLevel ?? 0) >= UnlockOnSuccess.MaxLevel;
    public void OnSuccess(Upgrades us) {
      if (!IsMaxed(us))
        us.AddUpgrade(UnlockOnSuccess);
    }
  }
}