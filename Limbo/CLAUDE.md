# Dark 2D World - Extended Pack

Unity 6 (`6000.0.60f1`), URP, 2D. The shipping product is `Assets/Dark 2D World - Extended`.
Everything outside that folder is development-side and must not end up in the .unitypackage.

## Maintaining the AI Skills

The pack ships AI Skills in `Assets/Dark 2D World - Extended/AI/Skills` so customers' agents
know how to drive it. They are customer-facing product, so treat them like code:

- **When you change a public field, default, component name, menu path or prefab path,
  update the matching skill in the same commit.** A stale skill is worse than no skill -
  it makes an agent confidently wrong.
- **When you fix a bug that was hard to diagnose, add it to that skill's Pitfalls section.**
  That section is the real value: the traps a customer cannot infer from the inspector.
  Current examples worth keeping: `usedByEffector` suppressing trigger callbacks, water
  having to stay behind the terrain, HDR yellowing the greys, `Input.GetKeyDown` in
  `FixedUpdate`.
- **Keep the section headings identical across all three skills** (When to use this skill /
  Setup / Reference / Recipes / Pitfalls / How to verify). Unity asks publishers not to
  restructure the format.
- **Verify against source, never from memory.** Unity's guidance to publishers is explicit
  that the publisher owns accuracy on menu paths, API names and defaults.
- After a meaningful change, re-run a scenario from `AISkillTests/scenarios.md` in a clean
  project with a fresh agent session.

## Repo layout

| Path | Ships? | Notes |
| --- | --- | --- |
| `Assets/Dark 2D World - Extended` | Yes | The product |
| `Assets/Dark 2D World - Extended/AI` | Yes | Customer-facing AI Skills |
| `AISkillTests/` | No | Skill test scenarios, deliberately outside `Assets/` |
| `CLAUDE.md` | No | This file |

## Non-obvious project facts

- Layers: `4 Water`, `6 Player`, `7 BoxChain`, `8 Pushable`. Tags: `Player`, `Ladder`.
- Sorting layers: `Default`, then `SortingTexture` (used only by the underwater overlay).
- The Main Camera is **perspective**, FOV 100, at `z = -10`. Parallax comes from layer Z,
  not from a script - a previous ParallaxLayer script was tried and rejected.
- **HDR is off in `Settings/UniversalRP.asset` on purpose.** See the level-building skill.
- The demo scene is `Assets/Dark 2D World - Extended/Demo Scenes/DEMO.unity`, roughly
  1350 units wide, player starts near `x = 455`.
- Public documentation lives at the Notion page "Dark 2D World - Extended Pack". Keep it in
  step with the skills; they cover the same ground for different audiences.
