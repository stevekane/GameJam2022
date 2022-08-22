using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PhysicsBuffers {
  public static int MAX_COLLIDERS = 256;
  public static int MAX_RAYCAST_HITS = 256;
  public static Collider[] Colliders = new Collider[MAX_COLLIDERS];
  public static RaycastHit[] RaycastHits = new RaycastHit[MAX_RAYCAST_HITS];
}