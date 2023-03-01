using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

[Serializable]
public struct Clip {
  public int StartFrame;
  public int EndFrame;
  public Clip(int start, int end) {
    StartFrame = start;
    EndFrame = end;
  }
}

[Serializable]
public class WeaponTrailTrack {
  public List<Clip> Clips;
}

public enum HitDirection {
  Forward,
  Left,
  Right,
}

[Serializable]
public struct HitboxClip : IEquatable<HitboxClip> {
  public int StartFrame;
  public int EndFrame;
  public Timeval HitStopDuration;
  public float KnockbackStrength;
  public HitDirection HitDirection;
  public float CameraShakeIntensity;
  public static bool operator ==(HitboxClip a, HitboxClip b) {
    return a.StartFrame == b.StartFrame
        && a.EndFrame == b.EndFrame
        && a.HitStopDuration == b.HitStopDuration
        && a.KnockbackStrength == b.KnockbackStrength
        && a.HitDirection == b.HitDirection
        && a.CameraShakeIntensity == b.CameraShakeIntensity;
  }
  public static bool operator !=(HitboxClip a, HitboxClip b) => !(a==b);
  public bool Equals(HitboxClip other) => this == other;
}

public enum AttackPhase {
  Windup,
  Active,
  Recovery
}

[Serializable]
public struct PhaseClip {
  public int StartFrame;
  public int EndFrame;
  public AttackPhase Phase;
}

[Serializable]
public class PhaseClipTrack {
  public List<PhaseClip> Clips;
}

[Serializable]
public class HitboxTrack {
  public List<HitboxClip> Clips;
}

[Serializable]
public struct AudioOneShotClip {
  public int Frame;
  public AudioClip Clip;
}

[Serializable]
public class AudioOneShotTrack {
  public List<AudioOneShotClip> Clips;
}

public class LogicalTimeline : MonoBehaviour {
  public static int FixedFrame;
  public static EventSource FixedTick = new();

  [Header("Input")]
  [SerializeField] InputManager InputManager;

  [Header("Animation")]
  [SerializeField] RuntimeAnimatorController AnimatorController;

  [Header("Visual Effects")]
  [SerializeField] GameObject OnHitVFX;

  [Header("Attack")]
  [SerializeField] AnimationClip Clip;
  [SerializeField] float AnimationSpeed = 1;
  [SerializeField] float BlendInFraction = .05f;
  [SerializeField] float BlendOutFraction = .05f;
  [SerializeField] WeaponTrailTrack WeaponTrailTrack;
  [SerializeField] HitboxTrack HitboxTrack;
  [SerializeField] PhaseClipTrack PhaseClipTrack;
  [SerializeField] AudioOneShotTrack AudioOneShotTrack;

  [Header("Components")]
  [SerializeField] CharacterController Controller;
  [SerializeField] Animator Animator;
  [SerializeField] AudioSource AudioSource;
  [SerializeField] WeaponTrail WeaponTrail;
  [SerializeField] Hitbox Hitbox;
  [SerializeField] Vibrator Vibrator;

  [Header("State")]
  [SerializeField] float LocalTimeScale = 1;
  [SerializeField] List<GameObject> Targets;


  TaskScope Scope;
  AnimationLayerMixerPlayable LayerMixer;
  PlayableGraph Graph;

  int HitStopFramesRemaining;
  public bool HitboxStillActive = true;
  public AttackPhase Phase;

  /*
  1. Pull to best target optimal position
  2. Transfer Linker root motion to targets
  3. Align attacker with best target for subsequent hits
  4. Allow stick motion outside some cone to steer the attacker
  5. Turn toward / Away from attacker on hit
  */

  /*
  Melee Aim Assist

    On attack start, search for target in a cone in front of the player.
    Pick the best target to attack based on angle and distance.
      We have a desired position to be to hit this target.
        Snap the attacker to this position on each frame of windup (stupid solution)
        Snap the attacker's orientation to face this target on each frame of windup
    This system runs during Windup phase of an attack
    The active phase will populate a set of hit targets
    The recovery phase will move with root motion only
  */

  /*
  Capture unique targets for a given attack.
  Reset these targets at attack start and end.
  */

  void Start() {
    Time.fixedDeltaTime = 1f / Timeval.FixedUpdatePerSecond;
    Scope = new TaskScope();
    InputManager.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Listen(StartAttack);
    Graph = PlayableGraph.Create("Logical Timeline");
    Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    Graph.Play();
    var animController = AnimatorControllerPlayable.Create(Graph, AnimatorController);
    LayerMixer = AnimationLayerMixerPlayable.Create(Graph);
    LayerMixer.SetInputCount(2);
    LayerMixer.ConnectInput(0, animController, 0, 1);
    var output = AnimationPlayableOutput.Create(Graph, "Animation", Animator);
    output.SetSourcePlayable(LayerMixer);
  }

  void OnDestroy() {
    InputManager.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Unlisten(StartAttack);
    Scope.Dispose();
    Graph.Destroy();
  }

  void OnAnimatorMove() {
    const SendMessageOptions MESSAGE_OPTIONS = SendMessageOptions.DontRequireReceiver;
    var dp = Animator.deltaPosition;
    var message = "OnSynchronizedMove";
    Controller.Move(dp);
    Targets.ForEach(target => target.SendMessage(message, dp, MESSAGE_OPTIONS));
  }

  void FixedUpdate() {
    var dt = LocalTimeScale * Time.fixedDeltaTime;
    Graph.Evaluate(dt);
    if (HitStopFramesRemaining > 0) {
      LocalTimeScale = 0;
      HitStopFramesRemaining--;
    } else {
      LocalTimeScale = 1;
    }
    FixedFrame++;
    FixedTick.Fire();
  }

  void OnHit(TestHurtBox testHurtBox) {
    if (!Targets.Contains(testHurtBox.Owner)) {
      Targets.Add(testHurtBox.Owner);
    }
    var vfx = Instantiate(OnHitVFX, testHurtBox.transform.position + Vector3.up, transform.rotation);
    Destroy(vfx, 3);
    Vibrator.VibrateOnHit(transform.forward, Hitbox.HitStopDuration.Ticks);
    HitStopFramesRemaining = Hitbox.HitStopDuration.Ticks;
    HitboxStillActive = false;
  }

  void StartAttack() {
    InputManager.Consume(ButtonCode.West, ButtonPressType.JustDown);
    Scope.Start(Attack);
  }

  public T? FirstFound<T>(IEnumerable<T> ts, Predicate<T> predicate) where T : struct {
    foreach (var t in ts) {
      if (predicate(t))
        return t;
    }
    return null;
  }

  async Task Attack(TaskScope scope) {
    var playable = AnimationClipPlayable.Create(Graph, Clip);
    playable.SetSpeed(AnimationSpeed);
    playable.SetTime(0);
    playable.SetDuration(Clip.length);
    try {
      LayerMixer.DisconnectInput(1);
      LayerMixer.ConnectInput(1, playable, 0, 1);
      var ticks = Mathf.RoundToInt(Clip.length * Timeval.FixedUpdatePerSecond);
      var activeHitbox = default(HitboxClip);
      while (!playable.IsDone()) {
        var fraction = (float)(playable.GetTime() / playable.GetDuration());
        var i = (int)(fraction * ticks);
        var weight = BlendWeight(BlendInFraction, BlendOutFraction, fraction) ;
        LayerMixer.SetInputWeight(1, weight);
        var phase = FirstFound(PhaseClipTrack.Clips, clip => i > clip.StartFrame && i <= clip.EndFrame);
        Phase = phase.HasValue ? phase.Value.Phase : AttackPhase.Windup;
        var wt = FirstFound(WeaponTrailTrack.Clips, clip => i >= clip.StartFrame && i <= clip.EndFrame);
        WeaponTrail.Emitting = wt.HasValue;
        var hb = FirstFound(HitboxTrack.Clips, clip => i >= clip.StartFrame && i <= clip.EndFrame);
        if (hb.HasValue) {
          // new hitbox
          if (hb != activeHitbox) {
            Hitbox.KnockbackStrength = hb.Value.KnockbackStrength;
            Hitbox.HitStopDuration = hb.Value.HitStopDuration;
            Hitbox.HitDirection = hb.Value.HitDirection;
            Hitbox.CameraShakeIntensity = hb.Value.CameraShakeIntensity;
            HitboxStillActive = true;
            Hitbox.Collider.enabled = true;
          // same hitbox
          } else if (hb == activeHitbox && HitboxStillActive) {
            HitboxStillActive = true;
            Hitbox.Collider.enabled = true;
          } else {
            Hitbox.Collider.enabled = false;
          }
          activeHitbox = hb.Value;
        } else {
          Hitbox.Collider.enabled = false;
        }
        var oneshot = AudioOneShotTrack.Clips.FirstOrDefault(clip => i == clip.Frame);
        if (oneshot.Clip) {
          AudioSource.PlayOneShot(oneshot.Clip);
        }
        await scope.ListenFor(FixedTick);
      }
    } catch (Exception e) {
      Debug.LogWarning(e.Message);
    } finally {
      LayerMixer.DisconnectInput(1);
      LayerMixer.SetInputWeight(1, 0);
      Targets.Clear();
      WeaponTrail.Emitting = false;
      HitboxStillActive = true;
      Hitbox.enabled = false;
      playable.Destroy();
    }
  }

  float BlendWeight(float blendInFraction, float blendOutFraction, float fraction) {
    if (blendOutFraction > 0 && fraction >= (1-blendOutFraction)) {
      return 1-(fraction-(1-blendOutFraction))/blendOutFraction;
    } else if (blendInFraction > 0 && fraction <= blendInFraction) {
      return fraction/blendInFraction;
    } else {
      return 1;
    }
  }
}