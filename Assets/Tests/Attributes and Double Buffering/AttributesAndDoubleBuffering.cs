using UnityEngine;

public class AttributesAndDoubleBuffering : MonoBehaviour {
  [SerializeField] BooleanAttribute CanMove;
  [SerializeField] BooleanAttribute CanRotate;

  void OnDrawGizmos() {
    var log = "";
    if (CanMove.Current.Value) {
      log += "CAN_MOVE\n";
    }
    if (CanRotate.Current.Value) {
      log += "CAN_ROTATE";
    }
    DebugUI.Log(this, log);
  }
}