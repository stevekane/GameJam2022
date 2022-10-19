using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
  -----------------
  Default Encounter
  -----------------
  A basic encounter with various example eventCalls is present for you to reference.
  Here is a step-by-step summary of what the default encounter does in order: (through eventCalls)
  - Wait 4 seconds (just so the demons do not immediately spawn the second the map finishes loading)
  - Sets Music to Light Blood Swamps
  - Spawns 2 Tentacles to hide in the Courtyard
  - Spawns 1 Hell Knight
  - Wait 2 seconds
  - Spawns 1 Cacodemon
  - Continuously respawn 4 Imps across the entire map
  - Make the AI aware of the Player
  - Waits for all demons who have the "main" string to be killed, which is the Hell Knight and Cacodemon
  - Sprinkle spawns 4 Hell Soldiers, with the spawns starting after 1 second and ending 1 second later
  - The encounter waits for all 4 Hell Soldiers to spawn before progressing
  - Spawns 1 Buff Totem in the Courtyard
  - Waits for all demons who have the "totem" string to be killed, which is the Buff Totem
  - Spawns 2 Prowlers with no exact spawn position
  - Waits for all demons who have the "main" string to be killed, which are the Prowlers
  - Waits for the player to enter a trigger in the Courtyard
  - Disables the center Meathook Node
  - Activates Energy Barriers that traps the player in the Courtyard
  - Sets Music to Heavy Blood Swamps
  - Wait 1 second
  - Spawns 1 Baron
  - Waits for all demons who have the "main" string to be killed, which is the Baron
  - Wait 1 second
  - Enables the center Meathook Node
  - Remove all the Energy Barriers in the Courtyard
  - Spawns 1 Possessed Arachnotron - the Spirit will only possess demons with the "main" string
  - Waits for the Arachnotron to be roughly 25% health
  - Spawns 1 Hell Knight with no exact spawn position
  - Waits for all demons who have the "main" string to be killed, which is the Possessed Arachnotron and Hell Knight
  - Stops all demons from respawning, which are the Imps
  - Kills all the Tentacles who have not been killed yet
  - Triggers all remaining demons to charge at the player
  - Waits for all demons to be killed
  - Sets Music to Ambient Blood Swamps
  - Saves a Checkpoint
  - Wait 3 seconds
  - Activates the next encounter
*/

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
    IEnumerator Enumerator;

    public object Current { get => Value; }
    public bool IsRunning { get; internal set; }
    public GameObject Value { get; internal set; }
    public void Reset() => throw new NotSupportedException();
    public bool MoveNext() => Enumerator != null ? Enumerator.MoveNext() : false;
    public void Stop() => (IsRunning, Enumerator) = (false, null);
    public SpawnMob(SpawnRequest sr) => (Enumerator) = new Fiber(Run(sr)); // TODO: Why does this need wrapping?

    IEnumerator Run(SpawnRequest sr) {
      IsRunning = true;
      var p = sr.transform.position;
      var r = sr.transform.rotation;
      VFXManager.Instance.SpawnEffect(sr.config.PreviewEffect, p, r);
      yield return Fiber.Wait(sr.config.PreviewEffect.Duration.Frames);
      VFXManager.Instance.SpawnEffect(sr.config.SpawnEffect, p, r);
      Value = Instantiate(sr.config.Mob, p, r);
      IsRunning = false;
    }
  }

  IEnumerator AutoRevive(SpawnRequest sr) {
    bool spawning = false;
    GameObject occupant = null;
    while (true) {
      if (occupant == null && !spawning) {
        spawning = true;
        var spawnJob = new SpawnMob(sr);
        yield return spawnJob;
        Debug.Log("Spawned via autorevive");
        occupant = spawnJob.Value;
        spawning = false;
      } else {
        yield return null;
      }
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
    // TODO: Implement ability to wait for all remaining demons to be dead
    // TODO: Make sure this is run in a fiber runner not standard coroutine
    // TODO: Stop the encounter?
  }
}