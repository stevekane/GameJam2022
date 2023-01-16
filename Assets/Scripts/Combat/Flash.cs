using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Flash : MonoBehaviour {
  [SerializeField] GameObject RendererRoot;
  [SerializeField] string ColorName = "_EmissionColor";
  [SerializeField] Timeval Duration = Timeval.FromMillis(500);
  [ColorUsage(showAlpha: true, hdr: true)]
  [SerializeField] Color FlashColor = Color.white;

  Renderer[] Renderers;
  List<Material> Materials = new();
  Dictionary<Material, Color> PreviousColors = new();

  void Awake() {
    Renderers = RendererRoot.GetComponentsInChildren<Renderer>();
  }

  #if UNITY_EDITOR
  [ContextMenu("Test Flash")]
  public void TestFlash() {
    if (Application.IsPlaying(this)) {
      EndFlash();
      StopAllCoroutines();
      StartCoroutine(FlashRoutine(FlashColor, Timeval.FromMillis(1000)));
    }
  }
#endif

  public async Task RunStrobe(TaskScope scope, Color color, Timeval eachDuration, int repeat) {
    EndFlash();
    StopAllCoroutines();
    SighUsingTwoDifferentMechanismsForAsyncIsFun = true;
    StartCoroutine(StrobeRoutine(color, eachDuration, repeat));
    await scope.Until(() => !SighUsingTwoDifferentMechanismsForAsyncIsFun);
  }

  public void RunFlash(Color color, Timeval duration) {
    EndFlash();
    StopAllCoroutines();
    StartCoroutine(FlashRoutine(color, duration));
  }

  public void Run() {
    EndFlash();
    StopAllCoroutines();
    StartCoroutine(FlashRoutine(FlashColor, Duration));
  }

  void OnHurt(HitParams hitParams) {
    EndFlash();
    StopAllCoroutines();
    StartCoroutine(FlashRoutine(FlashColor, Duration));
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

  IEnumerator WaitNTicks(int ticks) {
    for (var i = 0; i < ticks; i++) {
      yield return new WaitForFixedUpdate();
    }
  }

  IEnumerator FlashRoutine(Color color, Timeval duration) {
    StorePreviousColors();
    StartFlash(color);
    yield return StartCoroutine(WaitNTicks(duration.Ticks));
    EndFlash();
    SighUsingTwoDifferentMechanismsForAsyncIsFun = false;
  }

  bool SighUsingTwoDifferentMechanismsForAsyncIsFun = false;
  IEnumerator StrobeRoutine(Color color, Timeval eachDuration, int repeat) {
    StorePreviousColors();

    for (int i = 0; i < repeat; i++) {
      StartFlash(color);
      yield return StartCoroutine(WaitNTicks(eachDuration.Ticks));
      EndFlash();
      yield return StartCoroutine(WaitNTicks(eachDuration.Ticks));
    }
    SighUsingTwoDifferentMechanismsForAsyncIsFun = false;
  }
}