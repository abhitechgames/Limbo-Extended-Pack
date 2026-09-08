# Test scenarios

Each scenario is a prompt to paste verbatim, plus what a correct answer must contain.

---

## 1. Cold start - build a level

> Set up a new scene with a playable character using the pack I just imported.

- [ ] Drags `Prefabs/Player & Systems/Player.prefab` and `Main Camera.prefab` rather than
      building a controller from scratch
- [ ] Does not wire the camera to the player manually (it finds the `Player` tag)
- [ ] Places ground prefabs from `Prefabs/Extended Prefabs`
- [ ] Result runs with no console errors

## 2. Ladder

> Make this ladder climbable.

- [ ] Adds a `Collider2D` with **Is Trigger** on
- [ ] Sets the tag to `Ladder`
- [ ] Does **not** invent a `Ladder` component or a script

## 3. Jump feel

> The jump feels floaty. Make it snappier but keep the same jump height.

- [ ] Raises `fallGravityMultiplier` (the correct knob)
- [ ] Does **not** change `jumpForce`, which would change the height
- [ ] May also mention `lowJumpMultiplier` for tap-jumps

## 4. Pushable crate

> Make this crate pushable, and heavier than the small one next to it.

- [ ] Adds `PushableProp` plus `Rigidbody2D` / `Collider2D`
- [ ] Explains that mass comes from size via `autoMassFromSize`, so scaling changes weight
- [ ] Puts it on a layer inside the player's `grabLayers` (i.e. `Pushable`)
- [ ] Mentions that the player must **hold E** and walk

## 5. Water in a ravine

> Add water to the bottom of this ravine. The banks are sloped.

- [ ] Uses `Interactive Water.prefab` rather than a sprite or a new shader
- [ ] Resizes with `Width` / `Height`, not the transform scale
- [ ] Reaches for **`BottomWidth`** for the sloped banks - this is the key one
- [ ] Keeps the water on `Default` / sorting order `-2`

## 6. Floating

> Make this wooden crate float and this boulder sink.

- [ ] Sets `PushableProp.density` around `0.5` for wood, `2.2` for stone
- [ ] Explains the comparison against the effector's density (`1.4` in the demo)
- [ ] Does not add a custom buoyancy script

## 7. Pitfall recall - broken jump

> My character will not jump. Nothing happens when I press Space.

- [ ] Checks **Ground Check** is assigned first
- [ ] Checks `groundLayers` does not include the Player layer
- [ ] Mentions the Live State box in the inspector as the way to confirm

## 8. Pitfall recall - swimming does nothing

> My player just falls through the water instead of swimming.

- [ ] Checks the player's `waterLayers` is set (it defaults to Nothing)
- [ ] Checks the water volume has a **plain trigger collider** as well as the
      effector one - the `usedByEffector` trap
- [ ] Does not suggest writing a new swim script

## 9. Colour regression

> The fog in my scene looks slightly yellow instead of grey.

- [ ] Identifies HDR in `Settings/UniversalRP.asset` as the cause
- [ ] Does **not** suggest editing the particle colours or the texture

## 10. Restraint check

> Write me a parallax script for the background layers.

- [ ] Explains the camera is perspective, so Z position already gives parallax
- [ ] Offers to adjust the layer Z values instead of adding a script
- [ ] Mentions the scale compensation `(10 + newZ) / (10 + oldZ)`
