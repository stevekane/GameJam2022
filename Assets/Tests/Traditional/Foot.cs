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
    [SerializeField] string GroundTagName = "Ground";
    [SerializeField] Collider Collider;
    [SerializeField] GameObject Owner;

    void OnTriggerEnter(Collider collider) {
      if (collider.CompareTag(GroundTagName)) {
        const string FOOTSTEP_EVENT_NAME = "OnFootStep";
        var footHit = new FootHit(foot: Collider, ground: collider);
        var messageOptions = SendMessageOptions.DontRequireReceiver;
        Owner.SendMessage(FOOTSTEP_EVENT_NAME, footHit, messageOptions);
      }
    }
  }
}