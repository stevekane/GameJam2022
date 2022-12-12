using System.Collections;
using UnityEngine;

public class Missile : MonoBehaviour {
  public GameObject PayloadPrefab;
  public Timeval Duration = Timeval.FromSeconds(1);
  public Vector3 Target;
}