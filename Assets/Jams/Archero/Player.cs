using UnityEngine;

namespace Archero {
  public class Player : MonoBehaviour {
    public static Player Instance;
    public int Deaths = 0;

    void Awake() {
      if (Instance) {
        Instance.OnNewRoom(this);
        Destroy(gameObject);
      } else {
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(Instance.gameObject);
      }
    }

    void OnNewRoom(Player scenePlayer) {
      GetComponent<Upgrades>().OnNewRoom();
      GetComponent<CharacterController>().Warp(scenePlayer.transform.position, scenePlayer.transform.rotation);
      GetComponent<PersonalCamera>().Current = Camera.main;
    }

    void OnDeath() {
      Deaths++;
      if (Deaths <= (int)GetComponent<Attributes>().GetValue(AttributeTag.ExtraLives, 0)) {
        var d = GetComponent<Damageable>();
        d.Heal(d.MaxHealth);
        WorldSpaceMessageManager.Instance.SpawnMessage($"Extra Life", transform.position + 2*Vector3.up, 2f);
      } else {
        GameManager.Instance.OnPlayerDied();
        Destroy(gameObject);
      }
    }
  }
}