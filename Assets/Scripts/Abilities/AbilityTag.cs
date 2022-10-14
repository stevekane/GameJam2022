using System;

[Serializable, Flags]
public enum AbilityTag {
  OnlyOne = 1 << 0,               // Character is only allowed to have one of these running at once
  BlockIfRunning = 1 << 1,        // This trigger can not run if the ability is already running
  BlockIfNotRunning = 1 << 2,     // This trigger can not run if the ability is not already running
  Uninterruptible = 1 << 3,       // This ability keeps running if hit
  Cancellable = 1 << 4,           // This ability can be cancelled if a CancelOthers ability runs
  CancelOthers = 1 << 5,          // This ability will cancel any of the character's abilities with the Cancellable tag

  AbilityHeavyEnabled = 1 << 28,
  AbilitySlamEnabled = 1 << 29,
  AbilityMorphEnabled = 1 << 30,
  AbilitySuplexEnabled = 1 << 31,
}