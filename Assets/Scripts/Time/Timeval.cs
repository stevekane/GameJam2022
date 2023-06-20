using System;
using UnityEngine;

[Serializable]
public class Timeval {
  public static int FixedUpdatePerSecond = 60;
  public static int TickCount = 0;
  public static EventSource TickEvent = new();

  [SerializeField] public float Millis = 1;
  [SerializeField] public int FramesPerSecond = 60;

  public static Timeval FromSeconds(float seconds, int fps = 60) {
    return new Timeval { Millis = seconds*1000f, FramesPerSecond = fps };
  }
  public static Timeval FromMillis(float millis, int fps = 60) {
    return new Timeval { Millis = millis, FramesPerSecond = fps };
  }
  public static Timeval FromAnimFrames(int frames, int fps) {
    return new Timeval { Millis = (float)frames * 1000f / fps, FramesPerSecond = fps };
  }
  public static Timeval FromTicks(int ticks) {
    return FromAnimFrames(ticks, FixedUpdatePerSecond);
  }

  public int Ticks {
    set { Millis = value * 1000f / FixedUpdatePerSecond; }
    get { return Mathf.RoundToInt(Millis * FixedUpdatePerSecond / 1000f); }
  }
  public int AnimFrames {
    set { Millis = value * 1000f / FramesPerSecond; }
    get { return Mathf.RoundToInt(Millis * FramesPerSecond / 1000f); }
  }
  public float Seconds {
    get { return Millis * .001f; }
  }
}