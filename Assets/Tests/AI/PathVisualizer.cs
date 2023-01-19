using UnityEngine;
using UnityEngine.AI;

public class PathVisualizer : MonoBehaviour {
  [SerializeField] NavMeshAgent NavMeshAgent;
  [SerializeField] Material Material;
  [SerializeField] float OffsetHeight = 1;
  LineRenderer LineRenderer;
  Vector3[] Corners = new Vector3[16];
  void Awake() {
    LineRenderer = gameObject.AddComponent<LineRenderer>();
    LineRenderer.startWidth = .1f;
    LineRenderer.endWidth = .1f;
    LineRenderer.useWorldSpace = true;
    LineRenderer.material = Material;
  }
  void LateUpdate() {
    LineRenderer.positionCount = NavMeshAgent.path.GetCornersNonAlloc(Corners);
    for (var i = 0; i < LineRenderer.positionCount; i++) {
      LineRenderer.SetPosition(i, Corners[i]+OffsetHeight*Vector3.up);
    }
  }
}