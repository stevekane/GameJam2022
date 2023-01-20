using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshUtil : MonoBehaviour {
  public static NavMeshUtil Instance;

  // Returns closest point on the edge of the navmesh to the given point.
  public Vector3 FindClosestPointOnEdge(Vector3 point) {
    var best = (float.MaxValue, Edges[0]);
    foreach (var e in Edges) {
      var delta = point - e;
      if (delta.sqrMagnitude < best.Item1)
        best = (delta.sqrMagnitude, e);
    }
    return best.Item2;
  }

  // A list of points all around the edge of the navmesh. Contains both vertices and midpoints between them.
  List<Vector3> Edges;

  void Awake() {
    CreateNavMeshPoints();
  }

  void CreateNavMeshPoints() {
    var triangulation = NavMesh.CalculateTriangulation();
    Edges = triangulation.vertices.ToList();
    // Also add midpoints between triangle edges, for long triangles.
    for (int i = 0; i+2 < triangulation.indices.Length; i += 3) {
      Vector3 getVert(int ii) => triangulation.vertices[triangulation.indices[ii]];
      Vector3 getMid(Vector3 a, Vector3 b) => .5f*(a+b);
      var v1 = getVert(i);
      var v2 = getVert(i+1);
      var v3 = getVert(i+2);
      Edges.Add(getMid(v1, v2));
      Edges.Add(getMid(v1, v3));
      Edges.Add(getMid(v2, v3));
      //Edges.Add(1f/3f * (v1 + v2 + v3));
    }
  }

#if UNITY_EDITOR
  public bool DrawEdges = true;
  void OnValidate() {
    if (DrawEdges)
      CreateNavMeshPoints();
  }
  void OnDrawGizmos() {
    if (!DrawEdges)
      return;
    Gizmos.color = Color.magenta;
    foreach (var p in Edges)
      Gizmos.DrawWireSphere(p + Vector3.up, .5f);
    //Gizmos.color = Color.cyan;
    //for (int i = 0; i+2 < triangulation.indices.Length; i += 3) {
    //  Vector3 getVert(int ii) => triangulation.vertices[triangulation.indices[ii]];
    //  var v1 = getVert(i);
    //  var v2 = getVert(i+1);
    //  var v3 = getVert(i+2);
    //  Gizmos.DrawLine(v1 + Vector3.up, v2 + Vector3.up);
    //  Gizmos.DrawLine(v2 + Vector3.up, v3 + Vector3.up);
    //  Gizmos.DrawLine(v1 + Vector3.up, v3 + Vector3.up);
    //}
  }
#endif
}