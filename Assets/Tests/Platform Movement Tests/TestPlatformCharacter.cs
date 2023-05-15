using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(1)]
public class TestPlatformCharacter : MonoBehaviour {
  [SerializeField] PersonalCamera PersonalCamera;
  [SerializeField] InputManager InputManager;
  [SerializeField] CharacterController Character;
  [SerializeField] float JumpStrength = 10;
  [SerializeField] float MoveSpeed = 10;
  [SerializeField] float GroundCheckDistance = 1;

  public Vector3 Velocity;
  public TestPlatform Platform;

  void Start() {
    InputManager.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Listen(OnJump);
    InputManager.AxisEvent(AxisCode.AxisLeft).Listen(OnMove);
  }

  void OnDestroy() {
    InputManager.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Unlisten(OnJump);
    InputManager.AxisEvent(AxisCode.AxisLeft).Unlisten(OnMove);
  }

  void OnJump() {
    InputManager.Consume(ButtonCode.South, ButtonPressType.JustDown);
    if (Platform && Platform.Velocity.y <= 0)
      Velocity.y = JumpStrength;
    else if (Platform && Platform.Velocity.y > 0)
      Velocity.y = Platform.Velocity.y + JumpStrength;
  }

  void OnMove(AxisState state) {
    var v = PersonalCamera.CameraScreenToWorldXZ(state.XY);
    Velocity.x = v.x * MoveSpeed;
    Velocity.z = v.z * MoveSpeed;
  }

  void FixedUpdate() {
    var dt = Time.deltaTime;
    var g = Physics.gravity;
    if (Platform && Platform.Velocity.y >= Velocity.y)
      Velocity.y = dt * g.y;
    else
      Velocity.y += dt * g.y;
    Platform?.OnMove.Unlisten(Character.transform.Translate);
    Platform = null;
    var flags = Character.Move(dt * Velocity);
    var includesBelow = (flags & CollisionFlags.Below) != 0;
    Platform = includesBelow ? Platform : null;
    Platform?.OnMove.Listen(Character.transform.Translate);
  }

  void OnControllerColliderHit(ControllerColliderHit hit) {
    if (!Platform && hit.collider.TryGetComponent(out TestPlatformPart part)) {
      Platform = part.Platform;
    }
  }
}