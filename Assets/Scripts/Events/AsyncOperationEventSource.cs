using System;
using UnityEngine;

public class AsyncOperationEventSource : IEventSource<AsyncOperation> {
  public AsyncOperation Operation;
  public AsyncOperationEventSource(AsyncOperation operation) => Operation = operation;
  public void Listen(Action<AsyncOperation> handler) => Operation.completed += handler;
  public void Unlisten(Action<AsyncOperation> handler) => Operation.completed -= handler;
  public void Fire(AsyncOperation op) {}
}