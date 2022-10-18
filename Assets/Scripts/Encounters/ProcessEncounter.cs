using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public enum EncEvent { Base, Spawn, Birth, Death, Wait }

[Serializable]
public class Enc {
  public EncEvent Event;
  public SpawnRequest SpawnRequest;
  public string Name;
  public float Duration;
  public Enc[] Next;

  public static Enc Base(Enc[] next) {
    return new Enc {
      Event = EncEvent.Base,
      Next = next
    };
  }

  public static Enc Spawn(string name, SpawnRequest spawnRequest, Enc[] next) {
    return new Enc {
      Event = EncEvent.Spawn,
      Name = name,
      SpawnRequest = spawnRequest,
      Next = next
    };
  }

  public static Enc Birth(string name) {
    return new Enc {
      Event = EncEvent.Birth,
      Name = name,
    };
  }

  public static Enc Death(string name, Enc[] next) {
    return new Enc {
      Event = EncEvent.Death,
      Name = name,
      Next = next
    };
  }

  public static Enc Wait(float duration, Enc[] next) {
    return new Enc {
      Event = EncEvent.Wait,
      Next = next
    };
  }

  public override string ToString() {
    string RenderChildren(Enc[] children) {
      string cs = string.Join(" | ", children.Select(n => n.ToString()));
      return children.Length switch {
        0 => "",
        1 => "." + cs,
        _ => ".(" + cs + ")"
      };
    }
    string nodeString = Event switch {
      EncEvent.Base => $"Base",
      EncEvent.Spawn => $"Spawn({Name})",
      EncEvent.Birth => $"Birth({Name})",
      EncEvent.Death => $"Death({Name})",
      EncEvent.Wait => $"Wait({Duration})"
    };
    string childrenString = RenderChildren(Next);
    return nodeString + childrenString;
  }
}

public class ProcessEncounter : Encounter {
  public Enc Enc;

  IEnumerator RunEnc(Enc e, Dictionary<string, GameObject> d) {
    IEnumerator SpawnMob(SpawnRequest sr) {
      var p = sr.transform.position;
      var r = sr.transform.rotation;
      VFXManager.Instance.SpawnEffect(sr.config.PreviewEffect, p, r);
      yield return new WaitForSeconds(sr.config.PreviewEffect.Duration);
      VFXManager.Instance.SpawnEffect(sr.config.SpawnEffect, p, r);
      d.Add(e.Name, Instantiate(sr.config.Mob, p, r));
    }

    // Messsages to be sent to other systems based on the Event type
    if (e.Event == EncEvent.Spawn) {
      StartCoroutine(SpawnMob(e.SpawnRequest));
    }

    yield return e.Event switch {
      EncEvent.Birth => new WaitUntil(() => d.TryGetValue(e.Name, out var mob)),
      EncEvent.Death => new WaitUntil(() => d.TryGetValue(e.Name, out var mob) && mob == null),
      EncEvent.Wait => new WaitForSeconds(e.Duration),
      _ => null
    };
    // TODO: We should actually await the completion of all of these concurrently
    // running threads before exiting... the hack is to just fire them off but not wait
    // but that is jank
    // TODO: By actually waiting for all children to complete, an encounter can know if it
    // is complete (and potentially be augmented to also be stoppable)
    e.Next.ForEach(n => StartCoroutine(RunEnc(n, d)));
  }

  public override IEnumerator Run() {
    // TODO: Write in-line enc here using a sort of fluent API to test how viable/appealing it is
    yield return StartCoroutine(RunEnc(Enc, new()));
  }
}