using UnityEngine;

public class Cannon : MonoBehaviour {
  static int MAX_HITS = 32;  

  enum CannonState { Ready, Charging, Firing, Cooldown }

  [SerializeField] Hurtbox OwnerHurtbox;
  [SerializeField] Timeval BeamThreshold = Timeval.FromMillis(1000);
  [SerializeField] Timeval BeamDuration = Timeval.FromMillis(3000);
  [SerializeField] Timeval BeamCooldown = Timeval.FromMillis(6000);
  [SerializeField] Timeval BeamHitStop = Timeval.FromMillis(250);
  [SerializeField] Timeval BurstHitStop = Timeval.FromMillis(250);
  [SerializeField] Timeval BurstCooldown = Timeval.FromMillis(1000);
  [SerializeField] float BurstRange = 5;
  [SerializeField] float BeamRange = 1000;

  [SerializeField] GameObject BurstVFXPrefab;
  [SerializeField] Transform BeamOrigin;
  [SerializeField] LineRenderer[] Beams;
  [SerializeField] LayerMask HitLayerMask;

  RaycastHit[] BeamHits = new RaycastHit[MAX_HITS];
  Collider[] BurstHits = new Collider[MAX_HITS];
  CannonState State;
  int Frames;

  public bool IsReady { get => State == CannonState.Ready; }
  public bool IsCharging { get => State == CannonState.Charging; }
  public bool IsFiring { get => State == CannonState.Firing; }
  public bool IsCoolingDown { get => State == CannonState.Cooldown; }

  public void DepressTrigger() {
    if (State == CannonState.Ready) {
      State = CannonState.Charging;
      Frames = 0;
    }
  }

  public void HoldTrigger() {
    if (State == CannonState.Charging) {
      Frames++;
    }
  }

  public void ReleaseTrigger() {
    if (State == CannonState.Charging) {
      if (Frames < BeamThreshold.Frames) {
        var hitCount = Physics.OverlapSphereNonAlloc(transform.position, BurstRange, BurstHits);
        for (var i = 0; i < hitCount; i++) {
          var hit = BurstHits[i];
          if (hit.transform.position.IsInFrontOf(transform) && hit.TryGetComponent(out Hurtbox hurtbox)) {
            if (hurtbox != OwnerHurtbox) {
              var direction = transform.position.TryGetDirection(hit.transform.position) ?? transform.forward;
              var directionXZ = direction.XZ().normalized;
              hurtbox.Damage?.TakeDamage(directionXZ, BurstHitStop.Frames, 0, 20);
            }
          }
        }
        var effect = Instantiate(BurstVFXPrefab, BeamOrigin.position, transform.rotation);
        effect.transform.localScale = 2*new Vector3(BurstRange, .5f, BurstRange);
        Destroy(effect, 3f);
        State = CannonState.Cooldown;
        Frames = BurstCooldown.Frames;
      } else {
        State = CannonState.Firing;
        Frames = BeamDuration.Frames;
      }
    }
  }

  void FixedUpdate() {
    Frames = State switch {
      CannonState.Ready => 0,
      CannonState.Charging => Frames+1,
      CannonState.Firing => Frames-1,
      CannonState.Cooldown => Frames-1
    };

    Beams.ForEach(beam => beam.enabled = State == CannonState.Firing);

    if (State == CannonState.Firing) {
      foreach (var beam in Beams) {
        var hitCount = Physics.RaycastNonAlloc(beam.transform.position, BeamOrigin.forward, BeamHits, BeamRange);
        for (var j = 0; j < hitCount; j++) {
          var hit = BeamHits[j];
          if (hit.collider.TryGetComponent(out Hurtbox hurtbox)) {
            hurtbox.Damage?.TakeDamage(BeamOrigin.forward, BeamHitStop.Frames, 10, 0);
          }
        }
        var durationFraction = (float)Frames/(float)BeamDuration.Frames;
        var width = Mathf.Sin(Mathf.PI*durationFraction);
        beam.startWidth = width; 
        beam.endWidth = width;
        beam.SetPosition(1, new Vector3(0, 0, BeamRange));
      }
    }

    if (State == CannonState.Firing && Frames <= 0) {
      State = CannonState.Cooldown;
      Frames = BeamCooldown.Frames;
    } else if (State == CannonState.Cooldown && Frames <= 0) {
      State = CannonState.Ready;
      Frames = 0;
    }
  }
}