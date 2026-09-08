---
name: dark-2d-world-character
description: Set up, tune or debug the player character from the Dark 2D World - Extended Pack (Unity 2D) - walking and jump feel, ladders, pushing crates, swimming, and the follow camera. Use whenever the user is working on the character, says jumping or climbing or crate-pushing is not working, or wants the movement to feel heavier or snappier, in a project that contains "Assets/Dark 2D World - Extended".
---

# Dark 2D World - Extended: the character

`PlayerController` is a weighty 2D platformer controller: speed ramps up instead of
snapping, the jump arcs and can be cut short, and there is forgiveness on both sides of a
ledge. It also handles ladders, dragging crates and swimming.

## When to use this skill

Use it when the project contains `Assets/Dark 2D World - Extended` and the work touches the
character, its feel, ladders, pushable props, swimming, or the follow camera.

For scenery, layers and depth, use `dark-2d-world-level-building`.
For pools and buoyancy, use `dark-2d-world-water`.

## Setup

Drag `Prefabs/Player & Systems/Player.prefab` and `Prefabs/Player & Systems/Main Camera.prefab`
into the scene. Both are pre-configured. The camera locates the player by the `Player` tag.

Building one by hand instead - all four steps are required:

1. Add `PlayerController` (**Add Component > Dark 2D World > Player Controller**). It pulls in
   a `Rigidbody2D`. Add a `Collider2D` yourself.
2. Add an empty child at the character's feet and drag it into **Ground Check**.
3. Put the object on the **Player** layer, tag it **Player**.
4. Set **Ground Layers** and **Grab Layers** so neither includes the Player layer.

Input is read through the legacy Input Manager (`Input.GetAxisRaw`, `Input.GetKey`). In
**Project Settings > Player > Active Input Handling**, the project must be on *Input Manager
(Old)* or *Both*.

## Reference

Component: `GameSeed.DarkPlatformer.PlayerController`
Menu path: **Dark 2D World > Player Controller**

Controls: `Horizontal` / `Vertical` axes, `jumpKey` (Space), `grabKey` (E, hold).
All four are inspector fields - there is no hardcoded key.

Class defaults below. **The `Player.prefab` and the demo scene's Player are tuned
differently** - read the actual component before quoting a number at the user, and say which
one you read.

| Field | Class default | Notes |
| --- | --- | --- |
| `movingSpeed` | 6.5 | Top walking speed |
| `groundAcceleration` / `groundDeceleration` | 55 / 40 | Lower = heavier, more slide |
| `airAcceleration` / `airDeceleration` | 28 / 12 | Keep below the ground values |
| `jumpForce` | 40 | Impulse, divided by Rigidbody mass |
| `fallGravityMultiplier` | 1.25 | The main "not floaty" knob |
| `lowJumpMultiplier` | 2.2 | Gravity when jump is released early |
| `maxFallSpeed` | 32 | Terminal velocity |
| `coyoteTime` / `jumpBufferTime` | 0.12 / 0.12 | Forgiveness either side of a ledge |
| `climbSpeed` | 4 | Ladder speed |
| `ladderJumpScale` | 0.85 | Jump power when leaving a ladder |
| `ladderJumpPush` | 4.5 | Sideways shove; 0 = straight up |
| `ladderRegrabDelay` | 0.25 | Stops Up re-grabbing instantly |
| `stepOffLadderSideways` | true | Walking sideways lets go |
| `groundCheckRadius` | 0.3 | Shipped prefab uses 0.28 |
| `groundLayers` | Everything | Shipped prefab: everything except Player and Water |
| `grabLayers` | Everything | Shipped prefab: **Pushable only** |
| `waterLayers` | Nothing | Shipped prefab: **Water only**. Empty disables swimming |
| `swimSpeedFactor` | 0.55 | Shipped prefab: 0.6 |
| `buoyancy` | 14 | Shipped prefab: 16 |
| `swimStrokeForce` | 14 | Upward kick per stroke |
| `surfaceJumpDepth` | 0.9 | Within this depth of the surface, jump vaults you out |

Read-only state, safe to poll from other scripts:
`IsGrounded`, `IsInWater`, `IsClimbing`, `IsPushing`, `Facing`, `Velocity`.

Animator: drives a single int parameter `playerState` - `0` idle, `1` run, `2` jump,
`3` climb. Facing is a `SpriteRenderer.flipX` flip, because the shipped animation clips
already animate `localScale`.

Camera: `CameraController`, menu **Dark 2D World > Camera Controller**. `damping` 1.5,
`offset` (2, 1, 0), `verticalDampingScale` 0.6, `verticalDeadZone` 1.5. It leads in the
direction of travel and ignores small vertical hops, which keeps the parallax steady.

## Recipes

### Make a ladder climbable

Give the ladder a `Collider2D` with **Is Trigger** ticked and the **Ladder** tag. Nothing
else. Press Up or Down while touching it to grab on.

`Space` while climbing jumps off at `ladderJumpScale` of a normal jump, pushed sideways by
`ladderJumpPush` if a direction is held.

### Make a crate pushable

1. Add `PushableProp` (**Dark 2D World > Pushable Prop**) - it requires a `Rigidbody2D` and a
   `Collider2D`.
2. Set `density`: about `0.5` for wood, `2.2` for stone or metal.
3. Put it on the **Pushable** layer, or whichever layer is ticked in the player's
   `grabLayers`.

`autoMassFromSize` (on by default) sets the collider's density and turns on Unity's
`useAutoMass`, so mass is area times density. Scaling a crate in the scene really does make
it heavier, and `SpeedFactor` slows the player proportionally -
`referenceMass / (referenceMass + mass)`, floored at `minSpeedFactor`.

The player must hold `grabKey` and walk. Bumping into a crate only nudges it.

### Tune the jump feel

Raise `fallGravityMultiplier` for a snappier arc, lower it for a floatier one. It changes how
the jump *feels* without changing how high it goes, so it is almost always the right knob.

Peak height is `(jumpForce / rigidbodyMass)^2 / (2 * 9.81)`. Read both values off the object
rather than assuming - `jumpForce` is an impulse, so halving the Rigidbody mass doubles the
launch speed and quadruples the height. If you are authoring gaps, compute the apex from the
character you actually shipped and multiply air time by `movingSpeed` for the reach.

## Pitfalls

- **Ground Check must be assigned.** With it empty the character is never grounded and can
  never jump. The custom inspector flags this in red.
- **Never include the Player layer in `groundLayers` or `grabLayers`.** In ground layers the
  character stands on itself; in grab layers the reach raycast hits its own collider.
- **`waterLayers` empty means no swimming.** It defaults to Nothing, so a hand-built
  character will sink and behave like it is in air.
- **Do not read `Input.GetKeyDown` from `FixedUpdate`** if you extend this script. It is only
  true for one frame and FixedUpdate can miss it entirely or run twice inside it. All input
  decisions live in `Update`; `FixedUpdate` only applies movement.
- Triggers are excluded from the ground check on purpose. Do not switch it to a plain
  `OverlapCircle` without a filter, or standing inside a ladder or water trigger counts as
  ground and gives infinite jumps.

## How to verify

Select the Player and enter Play mode. The inspector shows a **Live State** box - Grounded,
On Ladder, In Water, Pushing, Facing, Speed - which is the fastest way to see what the
controller thinks is happening. It also prints plain-English setup warnings above it.

Then check by hand: walk and stop (should slide slightly), jump and tap-jump (different
heights), climb a ladder and press Space (should launch, not drop), hold E against a crate
and walk, and drop into water (should float and be able to jump out near the surface).
