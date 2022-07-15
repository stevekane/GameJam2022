using UnityEngine;

public class WireRider : MonoBehaviour {
  public Wire Wire;
  public Timeval TravelTime = Timeval.FromMillis(1000);

  int Traveled;

  void Update() {
    if (Input.GetKeyDown(KeyCode.Space)) {
      Traveled = 0;
    }
  }

  void FixedUpdate() {
    if (Traveled < TravelTime.Frames) {
      var distance = 1f-(float)Traveled/(float)TravelTime.Frames;
      var pathData = Wire.Waypoints.ToWorldSpace(distance);
      transform.SetPositionAndRotation(pathData.Position, pathData.Rotation);
      Traveled++;
    }
  }
}