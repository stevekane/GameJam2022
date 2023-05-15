using System.Collections;
using UnityEngine;

/*
p0 initial
c0 after character move

p1 after physics
c1 after character move

p2 after physics
c2 after character move

*/
public class TestPhysicsPenetration : MonoBehaviour {
  void Awake() {
    Application.targetFrameRate = 60;
    Time.fixedDeltaTime = ((float)1f)/60;
  }

  int count;
  void FixedUpdate() {
    if (count == 0) {
      var character = FindObjectOfType<CharacterController>();
      var platform = FindObjectOfType<Rigidbody>();
      character.Move(Vector3.zero);
      platform.MovePosition(platform.position + .25f * Vector3.up);
      character.transform.Translate(.25f * Vector3.up);
      Debug.Log("Draw 0 0");
    }

    if (count == 1) {
      var character = FindObjectOfType<CharacterController>();
      var platform = FindObjectOfType<Rigidbody>();
      character.Move(Vector3.zero);
      platform.MovePosition(platform.position + .25f * Vector3.up);
      character.transform.Translate(.25f * Vector3.up);
      Debug.Log("Draw .25 .25");
      // draw character at .25 platform at .25
    }

    count++;
  }
}