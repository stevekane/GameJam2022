using UnityEngine;

namespace Traditional {
  public class HeightIndicator : MonoBehaviour {
    [SerializeField] CharacterController CharacterController;
    [SerializeField] Dimensions Dimensions;
    [SerializeField] float GroundOffsetEpsilon = .1f;
    [SerializeField] float MaxHeight = 10;
    [SerializeField] Color MaxColor = Color.green;
    [SerializeField] Color MinColor = Color.red;
    [SerializeField] LineRenderer Altimeter;
    [SerializeField] MeshRenderer Surface;

    Color CurrentColor {
      get {
        var interpolant = Mathf.InverseLerp(-MaxHeight, MaxHeight, transform.position.y);
        var rgba = Color.Lerp(MinColor, MaxColor, interpolant);
        return rgba;
      }
    }

    void LateUpdate() {
      var color = CurrentColor;
      if (!CharacterController.isGrounded && transform.position.y >= 0) {
        Altimeter.gameObject.SetActive(true);
        Altimeter.SetPosition(0, transform.position);
        Altimeter.SetPosition(1, transform.position - Vector3.up * (transform.position.y + GroundOffsetEpsilon));
        Altimeter.material.color = color;
      } else if (!CharacterController.isGrounded && transform.position.y < -Dimensions.Value.y) {
        Altimeter.gameObject.SetActive(true);
        Altimeter.SetPosition(0, transform.position + Vector3.up * Dimensions.Value.y);
        Altimeter.SetPosition(1, transform.position - Vector3.up * (transform.position.y + GroundOffsetEpsilon));
        Altimeter.material.color = color;
      } else {
        Altimeter.gameObject.SetActive(false);
      }
      Surface.gameObject.SetActive(!CharacterController.isGrounded);
      Surface.transform.position = new Vector3(transform.position.x, GroundOffsetEpsilon, transform.position.z);
      Surface.material.color = color;
    }
  }
}