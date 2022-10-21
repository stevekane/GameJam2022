using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FiberEncounter : Encounter {
  [Header("Systems")]
  public AudioSource MusicSource;

  [Header("Paramaters")]
  public int InitialWaitFrames = Timeval.FramesPerSecond*4;
  public AudioClip IntroMusicClip;
  public SpawnRequest MattParry1;
  public SpawnRequest MattParry2;
  public SpawnRequest Badger1;
  public int WaspWaitFrames = Timeval.FramesPerSecond*2;
  public SpawnRequest Wasp1;
  public SpawnRequest[] AutoRespawns;
  public SpawnRequest[] StaggeredSpawns;
  public SpawnRequest BossSpawn;
  public AudioClip IntenseMusicClip;
  public int StaggerPeriod = Timeval.FramesPerSecond*1;
  public AudioClip NormalMusicClip;

  public class SpawnMob : IEnumerator, IValue<GameObject>, IStoppable {
    SpawnRequest SpawnRequest;
    IEnumerator Enumerator;

    public object Current { get => Value; }
    public bool IsRunning { get => Enumerator != null; }
    public GameObject Value { get; internal set; }
    public void Reset() => Enumerator = new Fiber(Run(SpawnRequest));
    public bool MoveNext() => Enumerator != null ? Enumerator.MoveNext() : false;
    public void Stop() => Enumerator = null;
    public SpawnMob(SpawnRequest sr) => (SpawnRequest, Enumerator) = (sr, new Fiber(Run(sr)));

    IEnumerator Run(SpawnRequest sr) {
      var p = sr.transform.position;
      var r = sr.transform.rotation;
      VFXManager.Instance.SpawnEffect(sr.config.PreviewEffect, p, r);
      yield return Fiber.Wait(sr.config.PreviewEffect.Duration.Frames);
      VFXManager.Instance.SpawnEffect(sr.config.SpawnEffect, p, r);
      Value = Instantiate(sr.config.Mob, p, r);
      Stop();
    }
  }

  IEnumerator AutoRevive(SpawnRequest sr) {
    GameObject occupant = null;
    while (true) {
      yield return Fiber.Until(() => IsDead(occupant));
      var spawnJob = new SpawnMob(sr);
      yield return spawnJob;
      occupant = spawnJob.Value;
    }
  }

  bool IsDead(GameObject g) => g == null;

  public override IEnumerator Run() {
    yield return Fiber.Wait(InitialWaitFrames);
    MusicSource.PlayOptionalOneShot(IntroMusicClip);
    var mattParry1 = new SpawnMob(MattParry1);
    var mattParry2 = new SpawnMob(MattParry2);
    var firstBadger = new SpawnMob(Badger1);
    yield return Fiber.All(mattParry1, mattParry2, firstBadger);
    yield return firstBadger;
    yield return Fiber.Wait(WaspWaitFrames);
    yield return new SpawnMob(Wasp1);
    var autoRespawns = AutoRespawns.Select(ar => new Fiber(AutoRevive(ar)));
    autoRespawns.ForEach(Bundle.StartRoutine);
    yield return Fiber.Until(() => IsDead(firstBadger.Value));
    var staggeredSpawns = new List<GameObject>(StaggeredSpawns.Length);
    foreach (var sr in StaggeredSpawns) {
      yield return Fiber.Wait(StaggerPeriod);
      var spawnMob = new SpawnMob(sr);
      yield return spawnMob;
      staggeredSpawns.Add(spawnMob.Value);
    }
    yield return Fiber.Until(() => staggeredSpawns.All(IsDead));
    MusicSource.PlayOptionalOneShot(IntenseMusicClip);
    var bossSpawn = new SpawnMob(BossSpawn);
    yield return bossSpawn;
    yield return Fiber.Until(() => IsDead(bossSpawn.Value));
    autoRespawns.ForEach(Bundle.StopRoutine);
    MusicSource.PlayOptionalOneShot(NormalMusicClip);
  }
}