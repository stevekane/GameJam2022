using UnityEngine;

public class NoRootMotion : Condition {
  [SerializeField] RootMotion RootMotion;

  public override bool Satisfied => !RootMotion.enabled;
}