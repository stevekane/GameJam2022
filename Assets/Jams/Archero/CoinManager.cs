using System;
using UnityEngine;
using UnityEngine.Pool;

namespace Archero {
  public static class ObjectPoolExtensions {
    public static T[] Fill<T>(this ObjectPool<T> pool, int count) where T : class {
      T[] contents = new T[count];
      for (var i = 0; i < count; i++)
        contents[i] = pool.Get();
      return contents;
    }

    public static void Drain<T>(this ObjectPool<T> pool, T[] contents) where T : class {
      for (var i = 0; i < contents.Length; i++)
        pool.Release(contents[i]);
    }
  }

  public class CoinManager : MonoBehaviour {
    public static CoinManager Instance;

    [SerializeField] Coin CoinPrefab;
    [SerializeField] bool CheckPoolSize;
    [SerializeField] int InitialPoolSize = 500;
    [SerializeField] int MaxPoolSize = 1000;

    public ObjectPool<Coin> CoinPool;

    Coin CreateCoin() => Instantiate(CoinPrefab, transform);
    void OnGetCoin(Coin coin) => coin.gameObject.SetActive(true);
    void OnReleaseCoin(Coin coin) => coin.gameObject.SetActive(false);
    void OnDestroyCoin(Coin coin) => Destroy(coin.gameObject);

    void Awake() {
      Instance = this;
      CoinPool = new ObjectPool<Coin>(CreateCoin, OnGetCoin, OnReleaseCoin, OnDestroyCoin, CheckPoolSize, InitialPoolSize, MaxPoolSize);
      CoinPool.Drain(CoinPool.Fill(InitialPoolSize));
    }
    void OnDestroy() {
      Instance = null;
      CoinPool.Dispose();
    }
  }
}