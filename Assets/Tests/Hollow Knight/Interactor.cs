using UnityEngine;

public class Interactor : MonoBehaviour {
  // TODO: If multiple interactables try to set this it will be last-writer wins
  // this is ok if we are intending for this not to happen but otherwise this
  // will need a system to consistently decide who is interacted with
  public Interactable Target { get; private set; }

  public void SetTarget(Interactable target) {
    Target = target;
  }
}