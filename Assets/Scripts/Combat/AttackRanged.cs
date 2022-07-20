using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackRanged : Attack {
  public Bullet BulletPrefab;
  public int NumBullets = 1;

  bool IsFiring = false;
  int FramesRemaining;

  public void EnterActive() {
    IsFiring = true;
    FramesRemaining = Config.ActiveDurationRuntime.Frames / NumBullets;
  }

  public void ExitActive() {
    IsFiring = false;
  }

  private void FixedUpdate() {
    if (IsFiring && --FramesRemaining <= 0) {
      FramesRemaining = Config.ActiveDurationRuntime.Frames / NumBullets;
      Bullet.Fire(BulletPrefab, transform.position, transform.forward, this);
    }
  }
}
