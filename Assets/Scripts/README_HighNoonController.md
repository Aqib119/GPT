# High Noon 1v1 Fusion Controller

Added scripts:

- `HighNoonNetworkInput.cs`: Fusion input payload.
- `HighNoonDuelController.cs`: networked duel movement + restricted vision + gyro-driven look support.
- `HighNoonInputProvider.cs`: pushes local input into Fusion runner.

## Behavior implemented

1. **1v1 facing setup**
   - Host yaw starts at `90`.
   - Client yaw starts at `-90`.

2. **Local stationary feeling**
   - Enable `keepLocalPlayerVisuallyStationary` to keep the local visual root anchored at spawn,
     while networked movement still replicates so the enemy sees movement.

3. **Restricted front vision**
   - Yaw is clamped around duel-facing direction via `maxYawOffsetFromFacing`.
   - Camera FOV is narrowed (`42`) to create front-focused vision.

4. **Enhanced gyro**
   - Gyroscope enabled when supported.
   - Smoothed attitude + multiplier contributes to look delta.

## Wiring in Unity

1. Add `HighNoonDuelController` to your player prefab.
2. Set references:
   - `networkBodyRoot` (optional transform for model root)
   - `localVisualRoot` (visual root that can be anchor-locked for local player)
   - `playerCamera` (local camera)
3. Add `HighNoonInputProvider` in scene and assign the local duel controller.
4. Register input provider as `INetworkRunnerCallbacks` listener on your `NetworkRunner`.

