using System.Collections.Generic;
using UnityEngine;

public struct ProjectileCollision {
  public Collider Collider;
  public Vector3 Point;
  public ProjectileCollision(Collider collider, Vector3 point) {
    Collider = collider;
    Point = point;
  }
}

public class ProjectileManager : MonoBehaviour {
  public static string ENTER_MESSAGE = "OnProjectileEnter";
  public static string STAY_MESSAGE = "OnProjectileStay";
  public static string EXIT_MESSAGE = "OnProjectileExit";
  public static int MAX_RAYCAST_HITS = 256;
  public static int INITIAL_PROJECTILE_OCCUPANCY = 256;
  public static ProjectileManager Instance;

  RaycastHit[] Hits = new RaycastHit[MAX_RAYCAST_HITS];
  List<Projectile> Projectiles = new(INITIAL_PROJECTILE_OCCUPANCY);
  Dictionary<Projectile,HashSet<Collider>> Contacts = new(INITIAL_PROJECTILE_OCCUPANCY);

  public void AddProjectile(Projectile p) {
    p.PreviousPosition = p.transform.position;
    Projectiles.Add(p);
    Contacts.Add(p, new());
  }

  public void RemoveProjectile(Projectile p) {
    Projectiles.Remove(p);
    Contacts.Remove(p);
  }

  void Awake() => Instance = this;
  void OnDestroy() => Instance = null;

  void FixedUpdate() {
    Projectiles.ForEach(SendEnterEvents);
    Projectiles.ForEach(SendStayEvents);
    Projectiles.ForEach(SendExitEvents);
    Projectiles.ForEach(SetPreviousPosition);
  }

  void SendEnterEvents(Projectile p) {
    var delta = p.transform.position-p.PreviousPosition;
    var ray = new Ray(p.PreviousPosition, delta.normalized);
    var hits = Physics.RaycastNonAlloc(ray, Hits, delta.magnitude, p.LayerMask, p.TriggerInteraction);
    var colliders = Contacts.GetValueOrDefault(p);
    for (var i = 0; i < hits; i++) {
      var hit = Hits[i];
      var collision = new ProjectileCollision(hit.collider, hit.point);
      p.gameObject.SendMessage(ENTER_MESSAGE, collision, SendMessageOptions.DontRequireReceiver);
      colliders.Add(hit.collider);
    }
  }

  void SendStayEvents(Projectile p) {
    var colliders = Contacts.GetValueOrDefault(p);
    foreach (var collider in colliders) {
      var collision = new ProjectileCollision(collider, p.transform.position);
      p.gameObject.SendMessage(STAY_MESSAGE, collision, SendMessageOptions.DontRequireReceiver);
    }
  }

  void SendExitEvents(Projectile p) {
    var delta = p.PreviousPosition-p.transform.position;
    var ray = new Ray(p.PreviousPosition, delta.normalized);
    var hits = Physics.RaycastNonAlloc(ray, Hits, delta.magnitude, p.LayerMask, p.TriggerInteraction);
    var colliders = Contacts.GetValueOrDefault(p);
    for (var i = 0; i < hits; i++) {
      var hit = Hits[i];
      var collision = new ProjectileCollision(hit.collider, hit.point);
      p.gameObject.SendMessage(EXIT_MESSAGE, collision, SendMessageOptions.DontRequireReceiver);
      colliders.Remove(hit.collider);
    }
  }

  void SetPreviousPosition(Projectile p) {
    p.PreviousPosition = p.transform.position;
  }
}