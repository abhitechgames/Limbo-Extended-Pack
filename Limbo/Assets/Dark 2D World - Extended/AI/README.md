# AI Skills for Dark 2D World - Extended Pack

This folder contains **AI Skills**: small instruction files that teach an AI coding
assistant how to actually use this pack, so you can ask for what you want in plain English
instead of explaining the pack first.

They are plain markdown. Nothing runs, nothing phones home, and deleting this folder has no
effect on the pack.

## What is in here

| Skill | Ask it about |
| --- | --- |
| `Skills/dark-2d-world-level-building` | Placing scenery, scene structure, layers, sorting, background depth |
| `Skills/dark-2d-world-character` | Movement feel, jumping, ladders, pushing crates, the follow camera |
| `Skills/dark-2d-world-water` | Pools, ripples, buoyancy, splashes, the underwater effect |

## Turning them on

**Unity AI** - this is the folder layout Unity AI looks in, so importing the pack is enough.

**Claude Code** - copy the three skill folders into `.claude/skills/` at your project root:

```
.claude/skills/dark-2d-world-level-building/SKILL.md
.claude/skills/dark-2d-world-character/SKILL.md
.claude/skills/dark-2d-world-water/SKILL.md
```

Or skip the copying and just tell it once per session: *"Read the skills in
Assets/Dark 2D World - Extended/AI/Skills and use them for this project."*

**Cursor, Copilot, Codex and other agents** - point the assistant at this folder once at the
start of a session:

> Read every SKILL.md under `Assets/Dark 2D World - Extended/AI/Skills` before working on
> this project.

Some assistants also pick them up if you reference the folder in your own `AGENTS.md` or
`.cursorrules`.

## Things worth asking

Once the skills are loaded, prompts like these should just work:

- *"Add a pool of water to my scene at the bottom of this ravine and make the crate float."*
- *"The character feels floaty. Make the jump snappier without changing how high it goes."*
- *"Make this ladder climbable."*
- *"My background layers do not move. Set up parallax."*
- *"Why can I see a straight grey edge at the side of my water?"*
- *"Set up a new level with the player, camera and post-processing already working."*

## A note on accuracy

The skills describe how this pack works in the version you downloaded, including the
non-obvious traps that are easy to get wrong. They do not replace your own judgement -
always review what an AI changes in your project before you keep it.

Questions, or something in here out of date? [Join the Discord](https://discord.gg/sbjZXg2YJ9)
or email gameseedassets@gmail.com
