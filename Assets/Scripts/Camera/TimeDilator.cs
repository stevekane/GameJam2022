using UnityEngine;

public class TimeDilator : MonoBehaviour {
  [SerializeField]
  CameraConfig Config;

  public static TimeDilator Instance;

  void Awake() {
    Instance = this;
  }

  void OnDestroy() {
    Instance = null;
  }

  public void Dilate(float timeScale) {
    Time.timeScale = Mathf.Min(Time.timeScale,timeScale);
  }

  void FixedUpdate() {
    var current = Time.timeScale;
    var interpolant = Mathf.Exp(Config.TIME_DELATION_DECAY_EPSILON*Time.fixedDeltaTime);
    Time.timeScale = Mathf.Lerp(1,current,interpolant);
  }
}