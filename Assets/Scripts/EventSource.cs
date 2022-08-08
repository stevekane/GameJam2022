using System.Collections.Generic;

public class EventSource {
  public static void Fire(EventSource source) {
    source.Action?.Invoke();
    source.Connected.ForEach(Fire);
  }

  public List<EventSource> Connected = new();
  public System.Action Action;
}

public class EventSource<T> {
  public static void Fire(EventSource<T> source, T t) {
    source.Action?.Invoke(t);
    source.Connected.ForEach(s => Fire(s,t));
  }

  public System.Action<T> Action;
  public List<EventSource<T>> Connected;
}