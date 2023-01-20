using UnityEngine;

public class ShockWave : MonoBehaviour {
  [SerializeField] MeshRenderer MeshRenderer;
  [SerializeField] AnimationCurve InterpolationCurve;
  [SerializeField] float Duration = 1;
  [SerializeField] float MinRadius = 0;
  [SerializeField] float MaxRadius = 10;
  [SerializeField] float MinOpacity = 0;
  [SerializeField] float MaxOpacity = 1;

  float Opacity = 1;
  float Radius = 0;
  float Elapsed = 0;

  void Start() {
    Opacity = MaxOpacity;
    Radius = MinRadius;
    Elapsed = 0;
  }

  void Update() {
    var dt = Time.deltaTime;
    Elapsed += dt;
    var interpolant = InterpolationCurve.Evaluate(Elapsed/Duration);
    Opacity = Mathf.Lerp(MaxOpacity, MinOpacity, interpolant);
    Radius = Mathf.Lerp(MinRadius, MaxRadius, interpolant);
    MeshRenderer.transform.localScale = Radius*Vector3.one;
    MeshRenderer.material.SetFloat("_Opacity", Opacity);
    if (Elapsed > Duration) {
      Destroy(gameObject);
    }
  }
}