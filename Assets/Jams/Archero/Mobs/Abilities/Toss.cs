using System.Threading.Tasks;
using UnityEngine;

namespace Archero {
  public class Toss : ClassicAbility {
    public Grenade GrenadePrefab;
    public HitConfig HitConfig;

    Attributes Attributes => AbilityManager.GetComponent<Attributes>();
    Transform Target => Player.Instance.transform;
    Vector3 Origin => transform.position + .5f*Vector3.up;

    public override async Task MainAction(TaskScope scope) {
      await scope.Seconds(.5f); // windup anim
      Grenade.LobAt(GrenadePrefab, Origin, Target.position, Attributes, HitConfig);
    }
  }
}