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

  Color CurrentColor {
    get {
      var interpolant = Mathf.InverseLerp(-MaxHeight, MaxHeight, transform.position.y);
      var rgb = Color.Lerp(MinColor, MaxColor, interpolant);
      rgb.a = Opacity.Evaluate(Mathf.InverseLerp(0, MaxHeight, Mathf.Abs(transform.position.y)));
      return rgb;
    }
  }

  void FixedUpdate() {
    if (Defaults.Instance.ShowAltimeter && !Status.IsGrounded && transform.position.y >= 0) {
      Altimeter.gameObject.SetActive(true);
      Altimeter.SetPosition(0, transform.position);
      Altimeter.SetPosition(1, transform.position - Vector3.up * (transform.position.y + GroundOffsetEpsilon));
    } else if (Defaults.Instance.ShowAltimeter && !Status.IsGrounded && transform.position.y < -HeadHeight) {
      Altimeter.gameObject.SetActive(true);
      Altimeter.SetPosition(0, transform.position + Vector3.up * HeadHeight);
      Altimeter.SetPosition(1, transform.position - Vector3.up * (transform.position.y + GroundOffsetEpsilon));
    } else {
      Altimeter.gameObject.SetActive(false);
    }
    Surface.gameObject.SetActive(!Status.IsGrounded);
    Surface.transform.position = new Vector3(transform.position.x, GroundOffsetEpsilon, transform.position.z);
    var color = CurrentColor;
    Altimeter.material.color = color;
    Surface.material.color = color;
  }
}