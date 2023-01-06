namespace Traditional {
  public abstract class AttributeBool : AbstractState {
    int Accumulator;
    bool Current;
    public bool Value { get => Current; set => Accumulator += (value ? 1 : -1); }
    void FixedUpdate() {
      Current = Accumulator > 0;
      Accumulator = 0;
    }
  }
}