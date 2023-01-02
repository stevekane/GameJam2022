using UnityEngine;

/*
We want our character to drive its locomotion from intended movement directions.
We want our cahracter to use navigation to find its way around the space.
We want our character to have a set of senses that may be used to detect threats

  visual threats may be seen in the view frustum of the character
  audio threats come from sounds of interest being emitted near the character
  allies may also detect threats and elect to notify other nearby enemies of the threat

We want our character to have custom abilities that may be active.
We want our character to have hurtboxes that may be struck by various affecting things.
We want our character to be able to respond to various gameplay events:

  OnHurt
  OnHit
  OnLand
  OnJump
  OnDeath
  OnDisable
*/
namespace Traditional {
  public class Character : MonoBehaviour {
  }
}