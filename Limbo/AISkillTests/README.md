# AI Skill tests

Scenarios for checking that the skills in
`Assets/Dark 2D World - Extended/AI/Skills` actually work for a customer.

**This folder stays in the repo and is never exported with the package.** Leave it outside
`Assets/` so it cannot end up in the .unitypackage.

## How to run one

1. Use a **fresh project** with only this pack imported, and an agent session with **no
   prior context** about the pack. Testing in this repo proves nothing - the agent can just
   read the source.
2. Point the agent at the Skills folder (see `Assets/Dark 2D World - Extended/AI/README.md`).
3. Paste the scenario prompt exactly as written. Do not add hints.
4. Score it against the checklist. Anything unchecked is a gap in the skill, not a gap in
   the customer.

Unity suggests authoring with a flagship model and testing with a cheaper one, so you catch
what a weaker agent will get wrong.

## Scoring

| Result | Meaning |
| --- | --- |
| Pass | Every checklist item, no invented API or menu path |
| Partial | Right approach, but missed a pitfall or needed a nudge |
| Fail | Wrong approach, hallucinated names, or a broken scene |

Log failures below with the date and what you changed in the skill.

## Results log

| Date | Scenario | Model | Result | Skill change |
| --- | --- | --- | --- | --- |
| | | | | |
