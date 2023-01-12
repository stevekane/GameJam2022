using UnityEngine;

/*
Confirm mathematics for finding dynamic value of input force needed
to achieve a desired resultant velocity.

I  ≡ Input Direction
F  ≡ Required Input Force
Vd ≡ Desired Velocity
Vc ≡ Current Velocity
V1 ≡ Next Velocity
V2 ≡ Current Velocity
dt ≡ Delta Time
A1 ≡ Next Acceleration
A0 ≡ Current Acceleration

A1 = F
V1 = V0 + A1 * dt

V1 = Vd

Vd = V0 + A1 * dt
Vd = V0 + F * dt

F = ((Vd - V0) / dt)
*/
[ExecuteInEditMode]
public class MomentumTests : MonoBehaviour {
  [field:SerializeField]
  public Vector3 F { get; private set; }
  public Vector3 V0;
  public Vector3 Vd;
  public float dt = 1;

  void Update() {
    F = ((Vd - V0) / dt);
  }

  void OnDrawGizmos() {
    Debug.DrawRay(transform.position, V0, Color.blue);
    Debug.DrawRay(transform.position, Vd, Color.green);
    Debug.DrawRay(transform.position + .25f * Vector3.up, F, Color.red);
  }
}