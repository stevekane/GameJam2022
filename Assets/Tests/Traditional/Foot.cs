using System;
using UnityEngine;

namespace Traditional {
  [Serializable]
  public class FootHit {
    public readonly Collider Foot;
    public readonly Collider Ground;
    public FootHit(Collider foot, Collider ground) {
      Foot = foot;
      Ground = ground;
    }
  }

  public class Foot : MonoBehaviour {
    [SerializeField] Collider Collider;
    [SerializeField] GameObject Owner;

    void OnTriggerEnter(Collider collider) {
      if (collider.CompareTag(Globals.GROUND_TAG_NAME)) {
        var footHit = new FootHit(foot: Collider, ground: collider);
        var messageOptions = SendMessageOptions.DontRequireReceiver;
        Owner.SendMessage(Globals.FOOTSTEP_EVENT_NAME, footHit, messageOptions);
      }
    }
  }
}