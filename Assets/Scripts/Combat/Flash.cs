using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flash : MonoBehaviour {
  public Timeval Duration = Timeval.FromMillis(500);
  public string ColorName = "_EmissionColor";
  [ColorUsage(showAlpha: true, hdr: true)]
  public Color FlashColor = Color.white;
  public List<Renderer> Renderers;
  List<Material> Materials = new();
  Dictionary<Material, Color> PreviousColors = new();

  #if UNITY_EDITOR
  [ContextMenu("Test Flash")]
  public void TestFlash() {
    if (Application.IsPlaying(this)) {
      EndFlash();
      StopAllCoroutines();
      StartCoroutine(FlashRoutine(Timeval.FromMillis(1000)));
    }
  }
  #endif

  void OnHurt(HitParams hitParams) {
    EndFlash();
    StopAllCoroutines();
    StartCoroutine(FlashRoutine(Duration));
  }

  void StartFlash(Timeval duration) {
    foreach (var material in PreviousColors.Keys) {
      material.SetVector(ColorName, FlashColor);
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

  IEnumerator WaitNTicks(int ticks) {
    for (var i = 0; i < ticks; i++) {
      yield return new WaitForFixedUpdate();
    }
  }

  IEnumerator FlashRoutine(Timeval duration) {
    StorePreviousColors();
    StartFlash(duration);
    yield return StartCoroutine(WaitNTicks(duration.Ticks));
    EndFlash();
  }
}