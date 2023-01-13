// Simple utility class to allow null chaining for unity game objects.
// Example usage:
//   Optional<Animator> animator = GetComponent<Animator>();
//   Animator?.Value.SetBool("Walking");
// If there is no animator component, Animator will be null and the second statement will be a no-op.
public class Optional<T> where T : UnityEngine.Object {
  public T Value = null;
  Optional(T value) => Value = value;
  public static implicit operator Optional<T>(T t) => !t ? null : new Optional<T>(t);
}