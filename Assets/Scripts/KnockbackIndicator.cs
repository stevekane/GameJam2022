using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KnockbackIndicator : MonoBehaviour {
  public LineRenderer LowRenderer;

  AbilityManager AbilityManager;
  Attributes Attributes;
  Ability LastAttack;
  HitParams LastHitParams;
  Dictionary<Defender, LineRenderer[]> Indicators = new();

  void UpdateIndicators(HitParams hitParams) {
    foreach (var d in Indicators.Keys) {
      UpdateIndicator(hitParams, d, 0, Indicators[d][0]);
      UpdateIndicator(hitParams, d, 50, Indicators[d][1]);
      UpdateIndicator(hitParams, d, 100, Indicators[d][2]);
    }
  }

  void UpdateIndicator(HitParams hitParams, Defender defender, float defenderDamage, LineRenderer line) {
    defenderDamage += hitParams.Damage;
    hitParams.Defender = defender.gameObject;
    hitParams.DefenderAttributes = defender.GetComponent<Attributes>();
    var knockbackStrength = hitParams.GetKnockbackStrength(defenderDamage);
    var knockbackVector = knockbackStrength * hitParams.KnockbackVector;
    Debug.Log($"Knockback at {defenderDamage} = {knockbackStrength}");

    const float DRAG = 5f;
    const float DONE_SPEED = 5f;
    var velocity = knockbackVector;
    var gravity = Time.fixedDeltaTime * hitParams.DefenderAttributes.GetValue(AttributeTag.Gravity);
    var maxFall = hitParams.DefenderAttributes.GetValue(AttributeTag.MaxFallSpeed);
    var fallSpeed = 0f;
    var position = defender.transform.position;
    List<Vector3> points = new();
    while (velocity.sqrMagnitude > DONE_SPEED*DONE_SPEED) {
      points.Add(position);
      velocity = Mathf.Exp(-Time.fixedDeltaTime * DRAG) * velocity;
      fallSpeed = Mathf.Min(fallSpeed + gravity, maxFall);
      position += Time.fixedDeltaTime * (velocity + new Vector3(0, -fallSpeed, 0));
    }
    line.positionCount = points.Count;
    line.SetPositions(points.ToArray());
  }

  void Awake() {
    AbilityManager = GetComponentInParent<AbilityManager>();
    Attributes = AbilityManager.GetComponent<Attributes>();
    LowRenderer.startColor = LowRenderer.endColor = Color.green;

    var defenders = FindObjectsOfType<Defender>();
    foreach (var d in defenders) {
      if (d.gameObject != AbilityManager.gameObject) {
        Indicators[d] = new[] {
          CreateIndicator(Color.green),
          CreateIndicator(Color.yellow),
          CreateIndicator(Color.red),
        };
      }
    }
  }

  LineRenderer CreateIndicator(Color color) {
    var line = Instantiate(LowRenderer, Vector3.zero, Quaternion.identity);
    line.startColor = line.endColor = color;
    line.startWidth = line.endWidth = 1f;
    line.gameObject.SetActive(true);
    line.positionCount = 0;
    return line;
  }

  void FixedUpdate() {
    var a = AbilityManager.Running.FirstOrDefault(a => a.HitConfigData != null);
    if (a != null && a != LastAttack) {
      LastHitParams = new(a.HitConfigData, Attributes);
      UpdateIndicators(LastHitParams);
    }
    LastAttack = a;
  }
}