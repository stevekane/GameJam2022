using System.Collections.Generic;
using UnityEngine;

public class GrapplePointManager : MonoBehaviour {
  public static GrapplePointManager Instance;

  public List<GrapplePoint> Points = new();
}