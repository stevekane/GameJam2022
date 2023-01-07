using UnityEngine;

public class HeightIndicator : MonoBehaviour {
  [SerializeField] Status Status;
  [SerializeField] float HeadHeight = 1;
  [SerializeField] float GroundOffsetEpsilon = .1f;
  [SerializeField] float MaxHeight = 10;
  [SerializeField] Color MaxColor = Color.green;
  [SerializeField] Color MinColor = Color.red;
  [SerializeField] LineRenderer Altimeter;
  [SerializeField] MeshRenderer Surface;
  [SerializeField] AnimationCurve Opacity;
  float LastKnownAltitude;

  Color CurrentColor {
    get {
      var interpolant = Mathf.InverseLerp(-MaxHeight, MaxHeight, transform.position.y);
      var rgb = Color.Lerp(MinColor, MaxColor, interpolant);
      rgb.a = Opacity.Evaluate(Mathf.InverseLerp(0, MaxHeight, Mathf.Abs(transform.position.y)));
      return rgb;
    }
  }

  void FixedUpdate() {
    var color = CurrentColor;
    if (Defaults.Instance.ShowAltimeter && !Status.IsGrounded && transform.position.y >= 0) {
      Altimeter.gameObject.SetActive(true);
      Altimeter.SetPosition(0, transform.position);
      Altimeter.SetPosition(1, transform.position - Vector3.up * (transform.position.y + GroundOffsetEpsilon));
      Altimeter.material.color = color;
    } else if (Defaults.Instance.ShowAltimeter && !Status.IsGrounded && transform.position.y < -HeadHeight) {
      Altimeter.gameObject.SetActive(true);
      Altimeter.SetPosition(0, transform.position + Vector3.up * HeadHeight);
      Altimeter.SetPosition(1, transform.position - Vector3.up * (transform.position.y + GroundOffsetEpsilon));
      Altimeter.material.color = color;
    } else {
      Altimeter.gameObject.SetActive(false);
    }
    const float MAX_RAYCAST_DISTANCE = 1000;
    var didHit = Physics.Raycast(transform.position, Vector3.down, out var hit, MAX_RAYCAST_DISTANCE, Defaults.Instance.EnvironmentLayerMask);
    var position = (didHit ? hit.point : (transform.position.XZ() + LastKnownAltitude * Vector3.up)) + GroundOffsetEpsilon * Vector3.up;
    LastKnownAltitude = position.y;
    Surface.gameObject.SetActive(!Status.IsGrounded);
    Surface.transform.position = position;
    Surface.material.color = color;
  }
}