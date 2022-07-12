using UnityEngine;

public class VoidLordPushable : Pushable {
  [SerializeField] VoidLord VoidLord;

  public override void Push(Vector3 velocity) {
    VoidLord.Push(velocity);
  }
}