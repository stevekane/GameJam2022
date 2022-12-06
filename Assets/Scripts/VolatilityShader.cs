using UnityEngine;

public class VolatilityShader : MonoBehaviour {
  MeshRenderer Renderer;
  [SerializeField] Damage Damage;

  void Awake() {
    Renderer = GetComponent<MeshRenderer>();
  }

  void Update() {
    if (Damage == null)
      return;
    var t = 2f * Mathf.Min(Damage.Points / 70f, 1.4f);
    Renderer.materials[1].SetFloat("_Damage", (Mathf.Exp(t*t) - 1f));
  }
}