using System;
using UnityEngine;

[Serializable]
public class Timeval {
  public static int FixedUpdatePerSecond = 60;
  public static int TickCount = 0;

  [SerializeField] public float Millis = 1;

  public static Timeval FromSeconds(float seconds) => new Timeval { Millis = seconds*1000f };
  public static Timeval FromMillis(float millis) => new Timeval { Millis = millis };
  public static Timeval FromTicks(int frames) => new Timeval { Millis = (float)frames * 1000f / FixedUpdatePerSecond };

  public int Ticks {
    set { Millis = value * 1000f / FixedUpdatePerSecond; }
    get { return Mathf.RoundToInt(Seconds * FixedUpdatePerSecond); }
  }
  public float Seconds => Millis * .001f;
}