using System.Collections;
using System.Collections.Generic;
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
    var t = 2f * Mathf.Min(Damage.Points / 50f, 1f);
    Renderer.materials[1].SetFloat("_Damage", (Mathf.Exp(t*t) - 1f));
  }
}
