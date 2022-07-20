using UnityEngine;

public class AbilityMan : MonoBehaviour {
  abstract class Ability {
    public abstract void Step(Action action, float dt);
    public void Start() {}
    public void Stop() {}
    public abstract bool Complete { get; }
    public abstract bool Cancellable { get; }
  }

  class SayHello : Ability {
    int FramesPerRep;
    int Frames;    
    int Count;
    public SayHello(int framesPerRep, int count) {
      FramesPerRep = framesPerRep;
      Frames = framesPerRep;
      Count = count;
    }
    public override bool Complete { get => Count == 0; }
    public override bool Cancellable { get => true; }
    public override void Step(Action action, float dt) {
      if (Count > 0) {
        if (Frames <= 0) {
          Debug.Log("hello");
          Count--;
          Frames = FramesPerRep;
        } else {
          Frames--;
        }
      }
    }
  }

  Ability CurrentAbility;

  void Start() {
    CurrentAbility = new SayHello(500, 5);
  }

  void FixedUpdate() {
    CurrentAbility?.Step(Inputs.Action, Time.fixedDeltaTime);
    if (CurrentAbility != null && CurrentAbility.Complete) {
      CurrentAbility.Stop();
      CurrentAbility = null;
      Debug.Log("Ability complete");
    }
  }
}