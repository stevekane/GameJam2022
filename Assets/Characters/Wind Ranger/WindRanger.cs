using UnityEngine;

/*
Wind Ranger:

ShackleShot:
  Blocked when
    owner has disabled
    owner has active ability
  While active
    owner has active ability
  Activate
    windup animation
    spawn/launch bolo in forward direction
    recovery animation

Powershot:
  Blocked when
    owner has disabled
    owner has active ability
  While active
    owner has active ability
  Activate
    windup animation or key up
    fire animation

Windrun:
  Blocked when
    owner has disabled
    owner has active ability
    already active
  While active
    owner has WindRunEffect
  Activate
    wait duration

  WindRunEffect
    Increases movespeed by multiplier

Focus Fire:
  Blocked when
    owner has disabled
    owner has active ability
    already active
  While active
    owner has FocusFireEffect
  Activate
    duration or fire(target) then wait cooldown
*/

public class WindRanger : MonoBehaviour {
  public AbilityManager AbilityManager;
  public CharacterController CharacterController;
}