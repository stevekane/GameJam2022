using System;
using UnityEngine;

public class AbilitySwapper : MonoBehaviour {
  public Ability Ability;

  [Header("SwapOnStart")]
  public AbilityUser SwapWithUser;
  public int SwapAbilityIndex;

  void SwapWith(AbilityUser user, int index) {
    var (myParent, usersParent) = (Ability.transform.parent, user.Abilities[index].transform.parent);
    (user.Abilities[index], Ability) = (Ability, user.Abilities[index]);
    (user.Abilities[index].gameObject.layer, Ability.gameObject.layer) = (Ability.gameObject.layer, user.Abilities[index].gameObject.layer);
    user.Abilities[index].transform.SetParent(usersParent, false);
    Ability.transform.SetParent(myParent, false);
  }

  void OnTriggerStay(Collider other) {
    if (other.TryGetComponent(out Hurtbox hurtbox) && hurtbox.Defender.TryGetComponent(out AbilityUser user)) {
      var activeIndex = Array.FindIndex(user.Abilities, (a) => a.IsRunning);
      if (activeIndex >= 0) {
        user.Abilities[activeIndex].Stop();
        SwapWith(user, activeIndex);
      }
    }
  }

  int WaitFrames = Timeval.FromMillis(2000).Frames;
  void FixedUpdate() {
    if (SwapWithUser != null) {
      SwapWith(SwapWithUser, SwapAbilityIndex);
      SwapWithUser = null;
    }
    if (!Ability.IsRunning && WaitFrames-- < 0) {
      WaitFrames = Timeval.FromMillis(2000).Frames;
      Ability.Activate();
    }
  }
}
