namespace Traditional {
  public class GroundDistance : AbstractState {
    public float Value { get => Next; set => Next = value; }
    float Next;
    void FixedUpdate() {
      Value = Next;
    }
  }
}