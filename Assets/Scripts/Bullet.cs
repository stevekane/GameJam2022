using UnityEngine;

public class Bullet : MonoBehaviour {
  public Vector3 Direction;
  public float Speed = 5;

  public static void Fire(Bullet prefab, Vector3 position, Vector3 direction, float speed = 5) {
    var bullet = Instantiate(prefab, position, Quaternion.FromToRotation(Vector3.forward, direction));
    bullet.Direction = direction;
    bullet.Speed = speed;
  }

  void Update() {
    transform.position += Speed * Time.deltaTime * Direction;
  }

  void OnCollisionEnter(Collision collision) {
    var player = collision.gameObject.GetComponentInParent<Player>();
    if (player) {
      Debug.Log($"Bullet hit PLAYER: {player}");
    }
    Destroy(gameObject);
  }
}
