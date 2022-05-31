using UnityEngine;

public class Chain : MonoBehaviour {
  public Transform Source;
  public Transform Dest;
  public LineRenderer LineRenderer;

  void Update() {
    LineRenderer.SetPosition(0,Source.position);
    LineRenderer.SetPosition(1,Dest.position);
  }
}