# ProcAnim (`com.99bugs.procanim`)

Our own procedural animation module, used **instead of Unity's Animator** for EcoBalance's animals.
The animal models are static (no bones, no animation clips), so ProcAnim moves the whole model in
code: every frame it works out a pose from what the animal is doing and how it is moving.

It has **no dependency on the app or on EcoSim**. The app tells it a `MotionState`; ProcAnim never
knows about species, AI or the simulation.

| File | What it does |
|---|---|
| `MotionPose.cs` | `MotionState` (Idle, Walk, Run, Flee, Stalk, Eat, Drink, Alert, Die) and `MotionPose` (lift, pitch, roll, scale) |
| `MotionProfile.cs` | Motion settings as an asset (Create > ProcAnim > Motion Profile): one looping `StateMotion` per state, plus blending, spring, death and spawn settings |
| `MotionStateMachine.cs` | Our replacement for an Animator Controller: current/previous state, cycle phase, cross-fade; turn-lean, accel-pitch and spawn-pop overlays. Plain C#, so it is unit-tested |
| `SpringFloat.cs` | Damped spring (mass-spring-damper, sub-stepped so it looks the same at 30 or 120 fps) for lean and settle |
| `LegLayout.cs`, `Appendage.cs`, `BodyDeformer.cs` | Finds the body parts in a static mesh with no bones: four legs (lowest part, split into corners, a hip on top of each), the head (front end above the legs, bending at the neck) and the tail (back end). `BodyDeformer` moves their vertices on a per-object mesh copy: legs trot, head grazes/nods/turns to look, tail wags. Needs the model's Read/Write import setting |
| `ProcAnimator.cs` | The component: measures speed/turning from how the object moved, evaluates the state machine, layers the springs and applies the pose to a `ProcAnimPivot` child |

## The motions
Looping states: idle breathing, walk, run, flee, stalk (crouched), eat (head dips), drink, alert (head up).
One-off: spawn pop (grows in with an overshoot); death, made child-friendly (lies down gently, stays a moment so it's noticed, then shrinks away; optional sparkle/leaf effect prefab; no blood).
Overlays on any state: lean into turns, nose dip/lift when braking/speeding up (both springs).
Body parts: legs trot (walk < run < flee), front legs brace to eat/drink; head bends down to graze/drink, nods while walking, looks around when idle and turns towards a predator (alert) or prey (stalk/hunt); tail wags.

Gaits (walk, run, flee, stalk) advance by **distance travelled**, so an animal moving faster steps
faster and an animal that stops stops stepping. The other states loop in time.

## How the app uses it
- `Packages/manifest.json`: `"com.99bugs.procanim": "file:../../../../packages/procanim"` and in `testables`.
- `AnimalBrain` adds `AnimalMotionBridge` (app, `Scripts/EcoSim/`), which adds a `ProcAnimator`, loads
  `Resources/Motion/<species asset name>` (e.g. `BlueSheep`), and maps `AnimalBrain.CurrentAction` to a
  `MotionState` every frame. It is the only code that knows about both sides.
- `ProcAnimator` moves the model's children under `ProcAnimPivot` on Awake and animates only the pivot,
  so the NavMeshAgent moving the root is never fought.

## Tuning
Select `Assets/_Project/Resources/Motion/BlueSheep` (or `SnowLeopard`) and change values in Play mode;
heights are in metres, angles in degrees. Add a new species by creating `Motion/<SpeciesAssetName>`.

## Rules
- Nothing in `Runtime/` may reference app code or EcoSim.
- Every change here needs a test in `Tests/Editor/`.
