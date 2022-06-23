using UnityEngine;

[CreateAssetMenu(fileName = "HeroConfig", menuName = "Hero/Config")]
public class HeroConfig : ScriptableObject {
  [Header("Bullet Time")]  
  public bool USE_BULLET_TIME = true;

  [Header("Moving")]
  [Tooltip("Max movement speed")]
  public float MOVE_SPEED = 45f;
  [Tooltip("Maximum amount of instant acceleration possible while on ground")]
  public float MAX_XZ_ACCELERATION = .2f;
  [Tooltip("Frames between footstep sounds")]
  public int FramesPerFootstep = 250;

  [Header("Dashing")]
  [Tooltip("Max dash speed")]
  public float DASH_SPEED = 100f;
  [Tooltip("Duration of dash")]
  public int MAX_DASH_FRAMES = 200;

  [Header("Jumping")]
  [Tooltip("Initial upward velocity when jumping")]
  public float JUMP_Y_VELOCITY = 15f;
  [Tooltip("Instant multiplier of movespeed for xz plane when jumping")]
  public float JUMP_XZ_MULTIPLIER = 2f;
  [Tooltip("Multiplier applied to normal jump when pouncing")]
  public float POUNCE_XZ_MULTIPLER = 2f;
  [Tooltip("Max frames for a pounce")]
  public int MAX_POUNCE_FRAMES = 300;

  [Header("Falling")]
  [Tooltip("Amount of air steering as a function of air-time")]
  public AnimationCurve STEERING_STRENGTH;
  [Tooltip("Amount of air-time before full air steering is possible")]
  public float MAX_STEERING_TIME = .5f;
  [Tooltip("Amount of steering power in the air")]
  public float MAX_STEERING_MULTIPLIER = 15;
  [Tooltip("Strength of downward force of gravity")]
  public float GRAVITY = -10f;
  [Tooltip("Multiplier applied to gravity when player is falling ('fast fall')")]
  public float FALL_GRAVITY_MULTIPLIER = 3;

  [Header("Perching")]
  [Tooltip("Rate to pull player to perch (exponential lerp)")]
  [Range(-10,-.1f)]
  public float PERCH_ATTRACTION_EPSILON = -.1f;

  [Header("Targeting")]
  [Tooltip("Weight of distance to target in scoring algorithm")]
  public AnimationCurve DISTANCE_SCORE;
  [Tooltip("Weight of angle between forward and totarget in scoring algorithm")]
  public AnimationCurve ANGLE_SCORE;
  [Range(0,3000)]
  [Tooltip("Maximum frames of targeting")]
  public int MAX_TARGETING_FRAMES = 300;
  [Range(0,100)] 
  [Tooltip("Maximum distance for targeting")]
  public float MAX_TARGETING_DISTANCE = 10f;
  [Range(0,180)] 
  [Tooltip("Maximum angle in front of player for targeting")]
  public float MAX_TARGETING_ANGLE = 90;

  [Header("Holding")]
  [Tooltip("Rate to pull held target to player (exponential lerp)")]
  [Range(-10,-.1f)]
  public float HOLD_ATTRACTION_EPSILON = -.1f;
  [Tooltip("Number of frames to reach for a throwable")]
  public int MAX_REACHING_FRAMES = 40;
  [Tooltip("Number of frames to pull a throwable to player")]
  public int MAX_PULLING_FRAMES = 30;

  [Header("Throwing")]
  [Tooltip("Speed of thrown objects")]
  public float THROW_SPEED = 50f;

  [Header("Status Effects")]
  [Tooltip("Time in seconds of a bumper's bump effect")]
  public float BUMP_DURATION = .25f;

  [Header("Audio")]
  public AudioClip RunningAudioClip;
  public AudioClip JumpAudioClip;
  public AudioClip PounceAudioClip;
  public AudioClip DashAudioClip;
  public AudioClip LandAudioClip;
  public AudioClip PerchAudioClip;
  public AudioClip LeapAudioClip;
  public AudioClip HoldAudioClip;
  public AudioClip ThrowAudioClip;
}