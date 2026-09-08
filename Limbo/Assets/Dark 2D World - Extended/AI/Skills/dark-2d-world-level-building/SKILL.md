---
name: dark-2d-world-level-building
description: Build or edit levels with the Dark 2D World - Extended Pack (Unity 2D, URP). Covers the prefab library, scene structure, layers and tags, sorting order, and the perspective-camera depth setup. Use whenever the user is placing scenery, building a level, organising the hierarchy, or asking why background layers move or sort oddly in a project that contains "Assets/Dark 2D World - Extended".
---

# Dark 2D World - Extended: building levels

A hand-drawn black-and-white silhouette environment pack for Unity 2D. Everything is
ordinary prefabs, sprites and colliders - there is no level editor, tilemap or custom
importer. Place prefabs, and it works.

## When to use this skill

Use it when the project contains `Assets/Dark 2D World - Extended` and the user wants to
build or change a level, add scenery, fix hierarchy or sorting, or adjust background depth.

For the character, ladders and crates, use `dark-2d-world-character`.
For pools, buoyancy and the underwater effect, use `dark-2d-world-water`.

## Setup

Requirements: Unity 6 (authored on `6000.0.60f1`), **URP**, Linear colour space, a 2D project.

To start a level from scratch:

1. Create a scene.
2. Drag in `Prefabs/Player & Systems/Player.prefab` and
   `Prefabs/Player & Systems/Main Camera.prefab`. The camera finds the player by the
   `Player` tag, so no wiring is needed.
3. Drag ground and prop prefabs from `Prefabs/Extended Prefabs` into the scene.
4. Enter Play mode.

If the whole scene renders magenta, URP is not active. Assign
`Assets/Dark 2D World - Extended/Settings/UniversalRP.asset` in
**Edit > Project Settings > Graphics**.

## Reference

Pack root: `Assets/Dark 2D World - Extended`

| Folder | Contents |
| --- | --- |
| `Demo Scenes/DEMO.unity` | The only demo scene, about 1350 units wide |
| `Prefabs/Extended Prefabs` | 171 props added by this pack, plus `Interactive Water.prefab` |
| `Prefabs/Base Prefabs` | 32 props carried over from the free pack |
| `Prefabs/Particles` | `DustMotesCalm`, `FallingLeaves`, `SmokeDarkSoft`, `Water Splash` |
| `Prefabs/Player & Systems` | `Player.prefab`, `Main Camera.prefab` |
| `Scripts` | `Camera/`, `Player/`, `Water/` - six short MonoBehaviours |
| `Shaders` | `Water.shader` + `Water.mat`, `Underwater.shader` + `Underwater.mat` |
| `Settings` | `UniversalRP.asset`, `Renderer2D.asset`, `Post Processing.asset` |
| `Sprites` | 236 source PNGs |

**Layers** (set in Project Settings, shipped with the pack):

| Index | Name | Used for |
| --- | --- | --- |
| 0 | `Default` | Ground, walls, scenery |
| 4 | `Water` | Water volumes |
| 6 | `Player` | The character only |
| 7 | `BoxChain` | Hanging chain segments |
| 8 | `Pushable` | Crates, logs, the boat |

**Tags:** `Player` on the character, `Ladder` on climbable ladder triggers.

**Sorting layers:** `Default`, then `SortingTexture`. `SortingTexture` draws after
`Default` and exists so the underwater overlay can sample the frame. Do not put ordinary
scenery on it.

**Demo scene hierarchy** - mirror the grouping in new scenes, it keeps large levels navigable:

```
Main Camera        (child: Underwater Overlay)
Post Processing    global Volume
Player             (child: IsGroundedPoint)
ENVIRONMENT        Parallax, Terrain, Structures, Props, Vegetation, Interactive
WATER              Water Surface, Water Volume, Floating Props
FX                 Placed FX (Falling Leaves, Dust Motes, Smoke Dark Soft)
```

## Recipes

### Add scenery

Drag any prefab from `Prefabs/Extended Prefabs`. Leave it on `Default`. Sprites already
carry colliders where they need them; scenery that is only decoration has none.

### Depth and parallax

There is **no parallax script**. The Main Camera is **perspective**, and in the demo it sits
`10` units back from the gameplay plane (`z = -10`, FOV 100). Anything at a positive Z then
drifts on its own:

```
drift = z / (cameraDistance + z)          // cameraDistance = 10 in the demo scene
```

The demo splits the background into three groups under `ENVIRONMENT/Parallax` at mean Z of
`0.45` (near, ~4%), `1.00` (mid, ~9%) and `1.69` (far, ~14%).

To change depth, move a whole layer group along Z. **A perspective camera also shrinks
whatever you push back**, so multiply the objects' scale by
`(cameraDistance + newZ) / (cameraDistance + oldZ)` to keep their apparent size. Keep the
near/black layer shallow - deep layers also shift slightly toward screen centre, which is
invisible on sky silhouettes but obvious on anything touching the ground.

Check the camera's actual Z and field of view before quoting numbers: the drift ratio
depends on the distance, not the FOV, and the `Main Camera.prefab` does not necessarily
match the demo scene's camera.

### Ambient effects

The emitters under `FX/Placed FX` are static, not camera-followed. Drag more from
`Prefabs/Particles` and position them. Dust motes are white and only read against the dark
silhouettes, so keep them near the terrain line rather than up in open sky.

### Post-processing

`Settings/Post Processing.asset` (Vignette, Bloom, Color Adjustments, Film Grain) is applied
by the global Volume at `SYSTEMS/Post Processing`. The Main Camera prefab already has
**Post Processing** and **FXAA** enabled. Bloom works here because the sky is pure white and
sits above the threshold.

## Pitfalls

- **Do not enable HDR** in `Settings/UniversalRP.asset`. It is off deliberately. URP's 32-bit
  HDR buffer gives blue a smaller mantissa than red and green; the pack's stacked low-alpha
  particles then drift blue downward and the greyscale fog turns visibly yellow. If HDR is
  genuinely needed later, set HDR Precision to 64-bit at the same time.
- **Do not move the water onto `SortingTexture`.** It must stay on `Default` at sorting
  order `-2`, behind the terrain - that is what hides the straight edges of the water mesh.
- **Do not turn off Camera Sorting Layer Texture** in `Settings/Renderer2D.asset`. It is on,
  bound to `Default`, and the underwater effect stops working without it.
- Keep the character off the `Default` layer, or its own ground check detects it as ground.
- Particle textures are BC7 (`CompressedHQ`), not DXT5, because DXT blotches the big soft
  cloud gradients. Keep it that way if you reimport them.

## How to verify

1. Open `Demo Scenes/DEMO.unity` and enter Play mode - the character should walk, jump and
   climb with no console errors.
2. Walk left and right and watch the three background layers separate.
3. Check the Console is clean. A per-frame "Renderer at index 1 is missing" warning means
   the camera is pointing at a renderer index the URP asset does not have.
