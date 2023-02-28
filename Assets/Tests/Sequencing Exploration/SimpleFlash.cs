using System.Collections.Generic;
using UnityEngine;

public class SimpleFlash : MonoBehaviour {
  [SerializeField] GameObject RendererRoot;
  [SerializeField] string ColorName = "_EmissionColor";
  [ColorUsage(showAlpha: true, hdr: true)]
  [SerializeField] Color FlashColor = Color.white;

  public int TicksRemaining;
  public int OnTicks;
  public int OffTicks;

  Renderer[] Renderers;
  List<Material> Materials = new();
  Dictionary<Material, Color> PreviousColors = new();
  int StateTicksRemaining;
  bool On;

  void Awake() {
    Renderers = RendererRoot.GetComponentsInChildren<Renderer>();
  }

  void FixedUpdate() {
    if (TicksRemaining > 0) {
      if (On) {
        if (StateTicksRemaining <= 0) {
          On = false;
          StateTicksRemaining = OffTicks;
          EndFlash();
        }
      } else {
        if (StateTicksRemaining <= 0) {
          On = true;
          StateTicksRemaining = OnTicks;
          StorePreviousColors();
          StartFlash(FlashColor);
        }
      }
    } else {
      On = false;
      StateTicksRemaining = 0;
      TicksRemaining = 0;
      EndFlash();
    }
    StateTicksRemaining = Mathf.Max(0, StateTicksRemaining-1);
    TicksRemaining = Mathf.Max(0, TicksRemaining-1);
  }

  void StartFlash(Color color) {
    foreach (var material in PreviousColors.Keys) {
      material.SetVector(ColorName, color);
    }
  }

  void EndFlash() {
    foreach (var pair in PreviousColors) {
      pair.Key.SetVector(ColorName, pair.Value);
    }
  }

  void StorePreviousColors() {
    Materials.Clear();
    PreviousColors.Clear();
    foreach (var meshRenderer in Renderers) {
      if (meshRenderer) {
        meshRenderer.GetMaterials(Materials);
        foreach (var material in Materials) {
          if (material.HasColor(ColorName)) {
            PreviousColors.Add(material, material.GetVector(ColorName));
          }
        }
      }
    }
  }
}