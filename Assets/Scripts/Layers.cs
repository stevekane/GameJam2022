
using System.Collections.Generic;
using UnityEngine;

public static class Layers {
  private static Dictionary<int, LayerMask> MasksByLayer = new();

  public static void Init() {
    for (int i = 0; i < 32; i++) {
      int mask = 0;
      for (int j = 0; j < 32; j++) {
        if (!Physics.GetIgnoreLayerCollision(i, j)) {
          mask |= 1 << j;
        }
      }
      MasksByLayer.Add(i, mask);
    }
  }

  public static LayerMask CollidesWith(int layer) {
    if (MasksByLayer.Count == 0)
      Init();
    return MasksByLayer[layer];
  }
}
