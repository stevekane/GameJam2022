using System;
using UnityEngine;

[Serializable]
public class Track {
  public int StartFrame;
  public int EndFrame;
}

public class MeleeAttack : MonoBehaviour {
  public int TotalFrames;
  public AnimationClip Clip;
  public Track Track;
}