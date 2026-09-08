---
name: dark-2d-world-water
description: Add, resize, tune or debug the interactive water from the Dark 2D World - Extended Pack (Unity 2D, URP) - the rippling surface mesh, buoyancy and floating props, splash effects, and the underwater distortion overlay. Use whenever the user mentions water, a pool or river, floating, sinking, splashes, or the underwater warp in a project that contains "Assets/Dark 2D World - Extended".
---

# Dark 2D World - Extended: interactive water

The water is a generated strip mesh, not a sprite. Each column along the surface is a
spring, so anything falling in dents the surface and the dip travels outwards. On top of
that: buoyancy, splash particles, and a full-screen warp while the player is submerged.

## When to use this skill

Use it when the project contains `Assets/Dark 2D World - Extended` and the work involves
water, buoyancy, splashes or the underwater effect.

For the swimming controls themselves, use `dark-2d-world-character`.
For layers, sorting and the render pipeline, use `dark-2d-world-level-building`.

## Setup

Drag `Prefabs/Extended Prefabs/Interactive Water.prefab` into the scene. It is a working
20 x 10 pool: it ripples, floats wood, sinks stone and warps the view underwater.

The prefab contains two children, and both are needed:

```
Interactive Water
  Water Surface   MeshFilter, MeshRenderer, EdgeCollider2D (trigger),
                  InteractableWater, WaterTriggerHandler          - layer Water
  Water Volume    BoxCollider2D (trigger, Used By Effector) + BuoyancyEffector2D
                  BoxCollider2D (trigger, plain) for detection    - layer Water
```

Resize with the **Width** and **Height** fields on `InteractableWater`, not by scaling the
transform. Then match the `Water Volume` box colliders to the same size.

## Reference

| Component | Menu path | Purpose |
| --- | --- | --- |
| `InteractableWater` | Dark 2D World > Interactable Water | Builds and simulates the surface mesh |
| `WaterTriggerHandler` | Dark 2D World > Water Trigger Handler | Splash particles + ripple on entry/exit |
| `UnderwaterEffect` | Dark 2D World > Underwater Effect | Screen warp while submerged |

`InteractableWater` defaults:

| Field | Default | Notes |
| --- | --- | --- |
| `NumOfVertices` | 96 | Columns along the surface; 96-160 is plenty |
| `Width` / `Height` | 10 / 4 | World units. Demo pool is 94.11 x 35.64 |
| `BottomWidth` | 1 | Fraction of Width at the bottom edge. Demo uses 0.52 |
| `WaterMaterial` | none | Assign `Shaders/Water.mat` |
| `springConstant` | 0.025 | How springy a column is |
| `damping` | 0.035 | How fast ripples die. Demo uses 0.02 |
| `spread` | 0.06 | How far ripples travel. Demo uses 0.05 |
| `maxDisplacement` | 1.2 | Clamp, so a heavy impact cannot tear the surface |
| `sortingLayer` / `sortingOrder` | `Default` / -2 | **Leave these alone** - see Pitfalls |

`WaterTriggerHandler`: `splashPrefab` (`Prefabs/Particles/Water Splash.prefab`),
`minImpactSpeed` 1.5, `fullImpactSpeed` 18, `minSplashScale` 0.5, `maxSplashScale` 2.2,
`splashLifetime` 3, `rippleStrength` 0.006, `exitRippleScale` 0.5, `reactingLayers`.

`UnderwaterEffect`: lives on a quad child of the Main Camera. `overlayMaterial`
(`Shaders/Underwater.mat`), `gameplayPlaneZ` 0, `fadeSpeed` 7, `fullEffectDepth` 0.5,
`sortingLayer` `SortingTexture`, `sortingOrder` 100. It finds the player and the water on
its own; the renderer is disabled entirely unless the player is under the surface.

Useful API: `water.SurfaceLevel`, `water.LeftEdge`, `water.RightEdge`,
`water.Splash(worldX, force, radius)` and `water.Splash(Collider2D, force)`.
`InteractableWater` is `[ExecuteAlways]`, so the mesh rebuilds in the editor; there is also a
**Rebuild Mesh** context-menu item.

## Recipes

### Fit water to a sloped riverbed

A rectangle pokes out through sloped banks. **`BottomWidth`** narrows the bottom edge into a
trapezoid: `0.52` in the demo, which follows a V-shaped bed. Too low and the background
shows in wedges beside the banks; too high and the bottom corners poke out. The gizmo draws
the shape when the object is selected.

### Make something float or sink

Buoyancy comes from **density**, as in real life. Add `PushableProp`, set `density` to about
`0.5` for wood or `2.2` for stone, and put the object on the **Pushable** layer.
`autoMassFromSize` turns on Unity's auto mass, so mass is collider area times density and a
bigger crate really is heavier. The demo's `BuoyancyEffector2D` uses density `1.4`, so
anything under that floats.

If floating objects feel glued in place, the culprit is usually the effector's `linearDrag`
and `flowMagnitude`, not their mass. The `Interactive Water` prefab ships `linearDrag` 2 and
`flowMagnitude` 0.6, which settles small props quickly but makes anything heavy feel stuck;
the demo scene's pool is loosened off to `0.35` and `0.1`. Lower both if a boat or a large
crate will not shove.

### Let the player swim

Set the player's `waterLayers` to the **Water** layer. The `BuoyancyEffector2D` in the demo
deliberately excludes the Water and Player layers via its collider mask, because
`PlayerController` runs its own swim physics.

### Add water to a new scene

Ensure **Camera Sorting Layer Texture** is enabled in `Settings/Renderer2D.asset`, bound to
`Default`, and that the camera has an `UnderwaterEffect` quad (the `Main Camera.prefab`
already does). Without it the surface still ripples but nothing warps underwater.

## Pitfalls

- **A collider with `usedByEffector = true` does not fire its own trigger callbacks.** That is
  why `Water Volume` carries two `BoxCollider2D`: one for the `BuoyancyEffector2D`, one plain
  trigger so the player can detect the water. Delete the second one and swimming silently
  stops working.
- **Keep the water on `Default` at sorting order `-2`.** It renders behind the terrain, and
  that is the only thing hiding the straight edges of the rectangular mesh. Moving it onto
  `SortingTexture` so it can refract makes every bank and the riverbed show a hard grey edge.
- **The underwater warp is a separate overlay, not the water shader.** `UnderwaterEffect` plus
  `Custom/Underwater2D` on a camera quad, sampling `_CameraSortingLayerTexture`.
- **`Custom/Water2D` needs `_Aspect`** (width divided by height) or the bubbles render as
  ovals on a wide pool. `InteractableWater` writes it through a `MaterialPropertyBlock`;
  `0` means "guess from the object scale", which is only right for a plain sprite quad.
- Mesh vertices are in **world units** with transform scale 1, so the material's wave height
  and frequency are world-unit values. Wave settings tuned for a 10-unit pool look wrong on a
  90-unit one.
- Do not enable HDR to improve the look - see `dark-2d-world-level-building`.

## How to verify

Enter Play mode and drop a Rigidbody2D object into the pool. Expect all four: a visible dent
that spreads outwards, a `Water Splash(Clone)` spawned at the surface scaled by impact speed,
the object settling at a depth that matches its density, and the view below the waterline
rippling once the player goes under while everything above stays crisp.

If the splash never appears, check `splashPrefab` is assigned and that the falling object has
a `Rigidbody2D` - the handler ignores anything without one.
