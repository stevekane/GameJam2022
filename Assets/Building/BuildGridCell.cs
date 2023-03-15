using System;
using UnityEngine;

public class BuildGridCell : MonoBehaviour {
  public enum State { Valid, Invalid, Empty };

  [SerializeField] MeshRenderer Surface;
  [SerializeField] MeshRenderer SurfaceOuter;
  Color InnerColor;
  Color OuterColor;

  Color AsValid(Color c) => new(0f, c.g, c.b, 1f);
  Color AsInvalid(Color c) => new(c.g, 0f, c.b, 1f);
  Color AsEmpty(Color c) => new(c.r, c.g, c.b, 0f);

  public void SetState(State state) {
    Func<Color, Color> changeColor = state switch {
      State.Valid => AsValid,
      State.Invalid => AsInvalid,
      State.Empty => AsEmpty,
      _ => AsEmpty,
    };
    Surface.material.color = changeColor(InnerColor);
    SurfaceOuter.material.color = changeColor(OuterColor);
  }

  void Awake() {
    InnerColor = Surface.sharedMaterial.color;
    OuterColor = SurfaceOuter.sharedMaterial.color;
    SetState(State.Empty);
  }
}