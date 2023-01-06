namespace Traditional {
  public class Grounded : AbstractState {
    public bool Value { get => Next; set => Next = value; }
    bool Next;
    void FixedUpdate() {
      Value = Next;
    }
  }
}