using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

[Serializable]
public struct CoverPosition {
  public Vector3 Cover;
  public Vector3 Exposed;
  public CoverPosition(Vector3 c, Vector3 e) {
    Cover = c;
    Exposed = e;
  }
}

[Serializable]
public struct CoverOptions {
  public LayerMask CoverCubeLayerMask;
  public LayerMask ThreatLayerMask;
  public int PositionChecksPerCorner;
  public float TangentStep;
  public float OrthogonalStep;
  public float NavMeshMaxDistance;
}

public class Ranged : MonoBehaviour {
  public static int MAX_COVER_POSITIONS = 256;

  public static int CoverPositionsNonAlloc(
  CoverPosition[] coverPositions, 
  List<CoverCube> coverCubes,
  Transform threat) {
    return CoverPositionsNonAlloc(coverPositions, coverCubes, threat, new CoverOptions {
      CoverCubeLayerMask = new LayerMask(),
      ThreatLayerMask = new LayerMask(),
      PositionChecksPerCorner = 3,
      TangentStep = 1,
      OrthogonalStep = 1,
    });
  }

  public static int CoverPositionsNonAlloc(
  CoverPosition[] coverPositions, 
  List<CoverCube> coverCubes, 
  Transform threat, 
  CoverOptions options) {
    var tangentStep = options.TangentStep;
    var orthogonalStep = options.OrthogonalStep;
    var coverLayerMask = options.CoverCubeLayerMask;
    var threatLayermask = options.ThreatLayerMask;
    var navMeshDistance = options.NavMeshMaxDistance;
    var index = 0;
    foreach (var covercube in coverCubes) {
      foreach (var corner in covercube.Corners) {
        if (corner.transform.IsVisibleFrom(threat.position, coverLayerMask)) {
          var delta = corner.transform.position-threat.position;
          var tangent = delta.XZ().normalized;
          var orthogonal = new Vector3(tangent.z, 0, -tangent.x);
          for (var i = 0; i < options.PositionChecksPerCorner; i++) {
            var pRight = corner.transform.position+i*tangentStep*tangent+orthogonal*orthogonalStep;
            var pLeft = corner.transform.position+i*tangentStep*tangent-orthogonal*orthogonalStep;
            var pRightOnNavMesh = NavMesh.SamplePosition(pRight, out NavMeshHit hitRight, navMeshDistance, NavMesh.AllAreas);
            var pLeftOnNavMesh = NavMesh.SamplePosition(pLeft, out NavMeshHit hitLeft, navMeshDistance, NavMesh.AllAreas);
            if (pRightOnNavMesh && pLeftOnNavMesh) {
              var pRightVisible = threat.transform.IsVisibleFrom(hitRight.position, threatLayermask);
              var pLeftVisible = threat.transform.IsVisibleFrom(hitLeft.position, threatLayermask);
              if (pLeftVisible && !pRightVisible) {
                coverPositions[index++] = new CoverPosition(hitRight.position, hitLeft.position);
              } else if (!pLeftVisible && pRightVisible) {
                coverPositions[index++] = new CoverPosition(hitLeft.position, hitRight.position);
              }
            }
          }
        }
      }
    }
    return index;
  }

  [SerializeField] NavMeshAgent Agent;

  public int Index;
  public Transform Threat;
  public CoverOptions CoverOptions;
  public List<CoverCube> CoverCubes;

  int CoverPositionCount;
  CoverPosition[] CoverPositions = new CoverPosition[MAX_COVER_POSITIONS];

  void FixedUpdate() {
    CoverPositionCount = CoverPositionsNonAlloc(CoverPositions, CoverCubes, Threat, CoverOptions);
    if (CoverPositionCount > Index) {
      Agent.SetDestination(CoverPositions[Index].Cover);
    }
  }

  void OnDrawGizmos() {
    #if UNITY_EDITOR
    if (!Application.isPlaying) {
      CoverPositionCount = CoverPositionsNonAlloc(CoverPositions, CoverCubes, Threat, CoverOptions);
    }
    #endif
    Gizmos.color = Color.green;
    for (var i = 0; i < CoverPositionCount; i++) {
      Gizmos.DrawWireCube(CoverPositions[i].Cover, Vector3.one);
    }
    Gizmos.color = Color.red;
    for (var i = 0; i < CoverPositionCount; i++) {
      Gizmos.DrawWireCube(CoverPositions[i].Exposed, Vector3.one);
    }
  }
}