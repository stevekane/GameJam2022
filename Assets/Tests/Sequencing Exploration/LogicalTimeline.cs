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
  [SerializeField, Range(0, 1)] float HitStopSpeed = .1f;

  [Header("Attack")]
  [SerializeField] AnimationClip Clip;
  [SerializeField] float AnimationSpeed = 1;
  [SerializeField] float BlendInFraction = .05f;
  [SerializeField] float BlendOutFraction = .05f;
  [SerializeField] WeaponTrailTrack WeaponTrailTrack;
  [SerializeField] HitboxTrack HitboxTrack;
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

  TaskScope Scope;
  AnimationLayerMixerPlayable LayerMixer;
  PlayableGraph Graph;

  int HitStopFramesRemaining;
  public bool HitboxStillActive = true;

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
    Controller.Move(Animator.deltaPosition);
  }

  void FixedUpdate() {
    var dt = LocalTimeScale * Time.fixedDeltaTime;
    Graph.Evaluate(dt);
    if (HitStopFramesRemaining > 0) {
      LocalTimeScale = HitStopSpeed;
      HitStopFramesRemaining--;
    } else {
      LocalTimeScale = 1;
    }
    FixedFrame++;
    FixedTick.Fire();
  }

  void OnHit(TestHurtBox testHurtBox) {
    Debug.Log("Hit");
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