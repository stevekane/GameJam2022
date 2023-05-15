using UnityEngine;
using UnityEngine.InputSystem;

/*
What we know:

  - RigidBody.position is fixed during the frame.
  - Calling RigidBody.MovePosition queues the physics engine to move the physics body and transform
    during the next physics step
  - RigidBody.Transform.position is variable during a frame
  - Calling RigidBody.Transform.Translate will immediately update the transform
    BUT the RigidBody.position will be unchanged until after the next physics update

What we can test:

  1. Move controller as well as platform
  2. Affect the controller's transform first
  3. Use Controller.Move for all calls other than translation with the platform

Why this order?

  1. Controller.Move against the CURRENT position of the platform
  2. Tell the platform to move
  3. Tell the controller its new position

*/
public class AllInOneUberMover : MonoBehaviour {
  [SerializeField, Range(10, 60)] int TicksPerSecond = 10;
  [SerializeField] Rigidbody Platform;
  [SerializeField] CharacterController Controller;
  [SerializeField] float MoveSpeed = 10;
  [SerializeField] float JumpStrength = 10;

  public Vector3 PlatformVelocity;
  public Vector3 CharacterVelocity;

  PlayerInputActions Controls;

  void Start() {
    Controls = new();
    Controls.Enable();
    Controls.Player.South.performed += OnJump;
  }

  void OnDestroy() {
    Controls.Dispose();
  }

  void OnJump(InputAction.CallbackContext ctx) {
    CharacterVelocity = PlatformVelocity;
    CharacterVelocity.y = JumpStrength;
  }

  void FixedUpdate() {
    Time.fixedDeltaTime = (float)1 / TicksPerSecond;
    PlatformVelocity = MoveSpeed * new Vector3(Mathf.Sin(Time.time), Mathf.Sin(Time.time / 4), Mathf.Cos(Time.time));
    var deltaPositionPlatform = PlatformVelocity * Time.fixedDeltaTime;
    var nextPositionPlatform = Platform.position + deltaPositionPlatform;
    var deltaVelocityCharacter = Physics.gravity * Time.fixedDeltaTime;
     // Physics verlet update
    if (Controller.isGrounded)
      if (CharacterVelocity.y <= 0)
        CharacterVelocity = new Vector3(0, Physics.gravity.y * Time.fixedDeltaTime, 0);
      else
        CharacterVelocity += new Vector3(0, Physics.gravity.y * Time.fixedDeltaTime, 0);
    else
      CharacterVelocity += new Vector3(0, Physics.gravity.y * Time.fixedDeltaTime, 0);

    Platform.MovePosition(nextPositionPlatform);
    Controller.Move(CharacterVelocity * Time.fixedDeltaTime);
    if (Controller.isGrounded)
      Controller.transform.Translate(deltaPositionPlatform);
  }
}