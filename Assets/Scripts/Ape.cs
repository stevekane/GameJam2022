using System.Collections.Generic;
using UnityEngine;

public class Ape : MonoBehaviour {
  float BulletTimeMax = 6;
  float BulletTimeRemaining = 0;
  bool Attached = false;
  bool BulletTime = false;
  Transform Origin = null;
  Transform Target = null;
  List<GameObject> Targets = new List<GameObject>(3);
}