using UnityEngine;

public static class DebugExtensions {
  public static void WithTick(string s) => Debug.Log($"{Timeval.TickCount} {s}");
}