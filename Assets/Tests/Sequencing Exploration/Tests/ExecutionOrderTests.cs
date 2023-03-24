using UnityEngine;

public class ExecutionOrderTests : MonoBehaviour {
  void Awake() => Debug.Log($"{FixedFrame.Instance.Tick} AWAKE");
  void Start() => Debug.Log($"{FixedFrame.Instance.Tick} START");
  void OnDestroy() => Debug.Log($"{FixedFrame.Instance.Tick} ON_DESTROY");
  void FixedUpdate() => Debug.Log($"{FixedFrame.Instance.Tick} FIXED_UPDATE");
  void OnAnimatorMove() => Debug.Log($"{FixedFrame.Instance.Tick} ON_ANIMATOR_MOVE");
  void OnAnimatorIK() => Debug.Log($"{FixedFrame.Instance.Tick} ON_ANIMATOR_IK");
  void OnTriggerStay(Collider c) => Debug.Log($"{FixedFrame.Instance.Tick} ON_TRIGGER_STAY {c.name}");
  void Update() => Debug.Log($"{FixedFrame.Instance.Tick} UPDATE");
  void LateUpdate() => Debug.Log($"{FixedFrame.Instance.Tick} LATE_UPDATE");
}