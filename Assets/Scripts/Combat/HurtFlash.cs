using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HurtFlash : MonoBehaviour {
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
      UnFlash();
      StopAllCoroutines();
      StartCoroutine(FlashRoutine(Timeval.FromMillis(1000)));
    }
  }
  #endif

  void OnDamage(HitParams hitParams) {
    UnFlash();
    StopAllCoroutines();
    StartCoroutine(FlashRoutine(Timeval.FromMillis(hitParams.HitStopDuration.Ticks*100)));
  }

  void Flash(Timeval duration) {
    foreach (var material in PreviousColors.Keys) {
      material.SetVector(ColorName, FlashColor);
    }
  }

  void UnFlash() {
    foreach (var pair in PreviousColors) {
      pair.Key.SetVector(ColorName, pair.Value);
    }
  }

  void StorePreviousColors() {
    Materials.Clear();
    PreviousColors.Clear();
    foreach (var meshRenderer in Renderers) {
      meshRenderer.GetMaterials(Materials);
      foreach (var material in Materials) {
        if (material.HasColor(ColorName)) {
          PreviousColors.Add(material, material.GetVector(ColorName));
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
    Flash(duration);
    yield return StartCoroutine(WaitNTicks(duration.Ticks));
    UnFlash();
  }
}