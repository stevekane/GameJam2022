using System.Collections.Generic;

public class EventSource : IEventSource {
  public List<IEventSource> Connected { get; set; } = new();
  public System.Action Action;

  public void Fire() {
    Action?.Invoke();
    Connected.ForEach(c => c.Fire());
  }
}

public class EventSource<T> : IEventSource<T> {
  public List<IEventSource<T>> Connected { get; set; } = new();
  public System.Action<T> Action;
  public void Fire(T t) {
    Action?.Invoke(t);
    Connected.ForEach(c => c.Fire(t));
  }
}