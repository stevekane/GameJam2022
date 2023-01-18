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
    for (int i = 0; i+2 < triangulation.indices.Length; i += 3) {
      Vector3 getVert(int ii) => triangulation.vertices[triangulation.indices[ii]];
      Vector3 getMid(Vector3 a, Vector3 b) => .5f*(a+b);
      var v1 = getVert(i);
      var v2 = getVert(i+1);
      var v3 = getVert(i+2);
      Edges.Add(getMid(v1, v2));
      Edges.Add(getMid(v1, v3));
      Edges.Add(getMid(v2, v3));
    }
  }
}