using UnityEngine;

struct RaycastOrigins {
  public Vector2 TopLeft;
  public Vector2 TopRight;
  public Vector2 BottomLeft;
  public Vector2 BottomRight;
}

public struct CollisionInfo {
  public bool Top;
  public bool Bottom;
  public bool Left;
  public bool Right;

  public void Reset() => Top = Bottom = Left = Right = false;
}

[RequireComponent(typeof(BoxCollider2D))]
public class CharacterController2D : MonoBehaviour {
  [SerializeField] LayerMask CollisionMask;
  [SerializeField] float SkinWidth = .015f;
  [SerializeField] int HorizontalRayCount = 4;
  [SerializeField] int VerticalRayCount = 4;

  public CollisionInfo Collisions { get; internal set; }

  BoxCollider2D Collider;
  RaycastOrigins RaycastOrigins;
  float HorizontalRaySpacing;
  float VerticalRaySpacing;

  void Awake() {
    Collider = GetComponent<BoxCollider2D>();
  }

  void Start() {
    CalculateRaySpacing();
  }

  public void Move(Vector3 velocity) {
    var collisions = new CollisionInfo();
    UpdateRaycastOrigins();
    if (velocity.x != 0) {
      HorizontalCollisions(ref velocity, ref collisions);
    }
    if (velocity.y != 0) {
      VerticalCollisions(ref velocity, ref collisions);
    }
    Collisions = collisions;
    transform.Translate(velocity);
  }

  void HorizontalCollisions(ref Vector3 velocity, ref CollisionInfo collisions) {
    float directionX = Mathf.Sign(velocity.x);
    float rayLength = Mathf.Abs(velocity.x) + SkinWidth;
    for (int i = 0; i < HorizontalRayCount; i++) {
      Vector2 rayOrigin = (directionX == -1) ? RaycastOrigins.BottomLeft : RaycastOrigins.BottomRight;
      rayOrigin += Vector2.up * (HorizontalRaySpacing * i);
      RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, CollisionMask);
      if (hit) {
        velocity.x = (hit.distance - SkinWidth) * directionX;
        rayLength = hit.distance;
        collisions.Left = directionX == -1;
        collisions.Right = directionX == 1;
      }
    }
  }

  void VerticalCollisions(ref Vector3 velocity, ref CollisionInfo collisions) {
    float directionY = Mathf.Sign(velocity.y);
    float rayLength = Mathf.Abs(velocity.y) + SkinWidth;
    for (int i = 0; i < VerticalRayCount; i++) {
      Vector2 rayOrigin = (directionY == -1) ? RaycastOrigins.BottomLeft : RaycastOrigins.TopLeft;
      rayOrigin += Vector2.right * (VerticalRaySpacing * i + velocity.x);
      RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, CollisionMask);
      if (hit) {
        velocity.y = (hit.distance - SkinWidth) * directionY;
        rayLength = hit.distance;
        collisions.Bottom = directionY == -1;
        collisions.Top = directionY == 1;
      }
    }
  }

  void UpdateRaycastOrigins() {
    Bounds bounds = Collider.bounds;
    bounds.Expand(SkinWidth * -2);
    RaycastOrigins.BottomLeft = new Vector2(bounds.min.x, bounds.min.y);
    RaycastOrigins.BottomRight = new Vector2(bounds.max.x, bounds.min.y);
    RaycastOrigins.TopLeft = new Vector2(bounds.min.x, bounds.max.y);
    RaycastOrigins.TopRight = new Vector2(bounds.max.x, bounds.max.y);
  }

  void CalculateRaySpacing() {
    Bounds bounds = Collider.bounds;
    bounds.Expand(SkinWidth * -2);
    HorizontalRayCount = Mathf.Clamp(HorizontalRayCount, 2, int.MaxValue);
    VerticalRayCount = Mathf.Clamp(VerticalRayCount, 2, int.MaxValue);
    HorizontalRaySpacing = bounds.size.y / (HorizontalRayCount - 1);
    VerticalRaySpacing = bounds.size.x / (VerticalRayCount - 1);
  }
}