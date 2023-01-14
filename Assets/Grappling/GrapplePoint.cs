using UnityEngine;

public class GrapplePoint : MonoBehaviour {
  void OnEnable() {
    GrapplePointManager.Instance.Points.Add(this);
  }
  void OnDisable() {
    GrapplePointManager.Instance.Points.Remove(this);
  }
}