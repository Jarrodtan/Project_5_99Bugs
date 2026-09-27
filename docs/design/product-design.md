# EcoBalance: Product Design Document

> Covers: team and roles · title · problem · target users · core user journey · MVP · product features · technical approach · high-level architecture.
> **Content owners:** Teo Shu Min (PM), Nomitha (Design Lead). Sections marked *TODO* need the design team's input.

## 1. Team and roles

| Member | Degree | Role / champion |
|---|---|---|
| Teo Shu Min | UXGD | Project Manager |
| Nomitha D/O Velmurugan | UXGD | Design Lead |
| Tan Wei Leong | RTIS | Tech Lead, Graphics/Rendering Champion |
| Sng Swe Yong Dillon | IMGD | UI Champion, Programmer |
| Reginald Lew Yee Ren | IMGD | Physics/Math Champion, Programmer |
| Chia Li Juan | IMGD | AI Champion, Programmer |
| Lim Zhi Jie | IMGD | Programmer |
| Lim Yan De | IMGD | Programmer |
| Yusuf Bin Abdul Rahman Bahajjaj | IMGD | Programmer |

## 2. Title

**EcoBalance** (working title during concept: *EcoLab 3D*)

## 3. Problem

Food chains and population balance are usually taught as static diagrams. Learners rarely get to change an ecosystem and watch the consequences play out over time, so the cause-and-effect links (predator ↔ prey ↔ resources) stay abstract.

## 4. Target users

Learners being introduced to ecosystems and food chains, who take the role of a **young ecosystem researcher**.
*TODO (Design): age range, learning context, and a summary of user research (interviews, comparable products such as other ecosystem sims), with links to the research notes.*

## 5. Core user journey

```
Launch → Ecosystem introduction → Configure starting ecosystem → Predict → Run simulation
       → Observe animals, resources and numbers → Pause / speed up / reset → Compare with prediction → Adjust and run again
```

1. **Configure** the starting amounts of vegetation, herbivores and carnivores (F01).
2. **Run and control** the simulation: start, pause, reset (F02), and speed up (F06).
3. **Observe** herbivores eating vegetation and predators eating herbivores (F03).
4. **Observe resources** being depleted by grazing and slowly regrowing (F04).
5. **Review** population and resource changes over time (F05), then adjust and repeat.

## 6. MVP for Milestone 1

The smallest useful experience the team promises to demonstrate: **a user configures a small grass → blue sheep → snow leopard ecosystem, runs it, controls it, and sees populations and grass change in response to each other.**

## 7. Product features (M1 Feature Plan)

| ID | Feature | What the user can do | Priority | Owner |
|---|---|---|---|---|
| F01 | Configure Starting Ecosystem | Choose starting amounts of vegetation, herbivores and carnivores | MUST | Shu Min |
| F02 | Run and Control Simulation | Start, pause and reset the simulation | MUST | Wei Leong |
| F03 | Observe Basic Ecosystem Behaviour | Watch herbivores eat vegetation and predators eat herbivores | MUST | Li Juan |
| F04 | Observe Resource Changes | Watch vegetation decrease when eaten and recover over time | MUST | Yan De |
| F05 | View Ecosystem Changes Over Time | See current herbivore, carnivore and vegetation levels as they change | SHOULD | Nomitha |
| F06 | Adjust Simulation Speed | Speed up the simulation to see long-term change | SHOULD | Reginald |
| F07 | Interact with Representative Animals | Learn about animals' needs through contextual dialogue | SHOULD | Nomitha |
| F08 | Discover Organism Information | Read "Did You Know?" facts about organisms | COULD | Shu Min |
| F09 | Inspect Individual Organisms | Select an animal to see hunger, thirst, health and behaviour | COULD | Nomitha |
| F10 | Ecosystem Introduction | Follow a short narrative introduction before experimenting | COULD | Shu Min |

Each MUST feature has a Jira epic/story. See [deliverables.md](../deliverables.md) for the technical deliverables (T01–T07) that support them.

## 8. Technical approach

- **Engine:** Unity 6 + URP. **Language:** C#. **Target:** Android ([toolchain.md](../toolchain.md)).
- **Data-driven EcoSim:** species, resources and relationships are ScriptableObject assets, so new species need no new code.
- **Population simulation** is separate from the visual agents. The numbers drive the populations, and 3D agents represent them.
- **Animal AI:** goal/utility-based decisions (needs → goals → actions) executed by agents on the NavMesh.
- **Quality:** unit tests (Unity Test Framework) and CI on every PR, with an Android build per merge.

## 9. High-level architecture

Data assets → **Simulation layer** (SimulationClock, EcosystemSimulator, AnimalBrain) → **Interface layer** (data/query providers) → **Presentation layer** (population display, spawner, selection, inspector).
Full diagram and component owners: [architecture.md](../architecture.md).

## 10. UI/UX

Wireframes, user flows and mockups: [ui-ux/](ui-ux/). *TODO (Design): add exported wireframes and the M1 mockup here.*

---
*Compiled from the team's concept draft, M1 Feature Plan and milestone workbook. Formatted with AI assistance (Claude Code, Claude Opus 5.5) by Tan Wei Leong.*
