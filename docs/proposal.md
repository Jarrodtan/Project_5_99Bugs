# EcoBalance: Project Proposal

**Team:** 99 Bugs (9 members: RTIS, IMGD, UXGD) · **Module:** CSD3401 / UXG3400, Fall 2026 · **Engine:** Unity 6 · **Target platform:** Android tablet

## 1. Concept

EcoBalance is a 3D interactive, educational ecosystem simulation. The player sets up a small ecosystem, predicts what will happen, runs the simulation, watches the results, and adjusts the ecosystem based on what they learned.

```
BUILD → PREDICT → SIMULATE → OBSERVE → LEARN → ADJUST
```

The focus is **experimentation**. Rather than presenting facts about ecology, EcoBalance lets the player change conditions directly and discover the consequences, for example:

> Less rainfall → less water → less grass → fewer blue sheep → less food for snow leopards → fewer snow leopards.

## 2. Problem

Food chains and population balance are usually taught as static diagrams. Learners see *that* predators and prey are linked, but rarely get to **change** an ecosystem and watch the chain reaction unfold over time. EcoBalance makes cause and effect visible and testable.

## 3. Target users

**Children aged 6–9**, playing as a **young ecosystem researcher** on an **Android tablet**. They are being introduced to ecosystems and food chains.

- **Intended experience:** curiosity → experimentation → discovery → understanding.
- **Design priorities:** simple touch interactions, visible cause-and-effect, and safe experimentation with easy retry.

## 4. Initial ecosystem (M1 scope)

A small Himalayan ecosystem, kept small so the interactions stay understandable while still producing chain reactions:

```
Environment / rainfall → Water → Grass → Blue Sheep → Snow Leopard
```

- Blue sheep eat grass. Snow leopards hunt blue sheep.
- Grass regrows over time.
- Water exists as a resource asset. Rainfall and water-driven effects are planned after M1.

## 5. Player experience

| Step | What the player does |
|---|---|
| **Build** | Sets starting populations and resources, and configures an experiment |
| **Predict** | Predicts how the ecosystem will change before running it |
| **Simulate** | Runs the ecosystem forward in simulated time (play, pause, reset, speed) |
| **Observe** | Watches animals and resources in 3D, alongside live population and resource numbers |
| **Learn & adjust** | Compares the result with the prediction, changes a condition and runs again |

## 6. Technical approach (summary)

- **Unity 6 (URP)** for rendering, navigation, input and Android deployment. The team builds the custom simulation, not an engine.
- **EcoSim**, a data-driven simulation core. Species, resources and relationships (*Snow Leopard → Eats → Blue Sheep*) are ScriptableObject data, so new species need no new code.
- **Population-level simulation** is separate from the visual agents. EcoSim tracks the population numbers, and a smaller number of 3D animals represent them.
- **Animal AI**: a goal/utility-based decision system (needs → goals → actions) driving individual agents.
- **Engineering**: GitHub (PR-only `main`), GitHub Actions CI (unit tests + Android build), Jira.

Details: [Architecture](architecture.md) · [Toolchain](toolchain.md) · [Deliverables & ownership](deliverables.md) · [Product design](design/product-design.md)

## 7. Core technical goal

Changing **one** variable should produce meaningful consequences across the whole ecosystem through reusable systems, not through scripted events:

```
Change one variable → resources change → species behaviour changes → populations change → the player sees the chain reaction
```

---
*Adapted from the team's "EcoLab 3D: Concept & Technical Architecture Draft" and M1 Feature Plan. Formatted with AI assistance (Claude Code, Claude Opus 5.5) by Tan Wei Leong. Content owners: Teo Shu Min (PM), Nomitha (Design Lead).*
