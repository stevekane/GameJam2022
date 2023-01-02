namespace Traditional {
  public class MaxFallSpeed : Attribute<float> {
    public override float Base { get; set; } = -10;
    public override float Evaluate(float t) {
      return t;
    }
  }
}