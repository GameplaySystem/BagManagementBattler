# Bag War — Architecture

Status: design baseline; implementation has not been verified or started here.
Working title: Bag War. Component names below describe responsibilities, not a
requirement to create every class before its feature is implemented.

## Scope

A small 2D Unity portfolio game: prepare a bag, produce soldiers through item
adjacency, fight an enemy wave, receive a reward, and prepare again.
Target approximately two weeks of development, subject to actual progress.
One main scene contains the bag, battlefield, and persistent HUD.

MVP exclusions: physical projectiles, throwable weapons, item rotation,
multiple soldier types, complex boss abilities, advanced pathfinding, and
multiple worlds of content. Persistence requirements remain undecided.
Seven weapon families are planned: knife, crowbar, pistol, SMG, rifle,
shotgun, sniper. Integrate a small subset first. Each item has three levels.

## Responsibilities

| Owner | Responsibility | Boundary |
|---|---|---|
| BagGrid | Cell availability, occupancy, footprint queries and placement validation | No input, shop, or combat behavior |
| BagManager | Coordinate pickup, placement, merge, replacement and rollback | No soldier spawning |
| Drag input/view | Detect pointer actions, show drag feedback, submit requests | Does not independently edit grid occupancy |
| Item container | Hold ordered loose items in scrollable generated slots | No production; no practical capacity limit |
| SoldierItem | Bag identity, config reference, current level and placement | Not a battlefield soldier |
| WeaponItem | Bag state, resolved adjacent soldier items and production timer | Requests spawning; does not acquire pooled instances |
| SoldierSpawner | Acquire, initialize, track and recycle battlefield units | No bag rules or production scheduling |
| CombatSoldier | Coordinate a unit's health, movement, targeting and states | No economy or wave progression |
| Health | Current/max health and depletion notification | No animation or pool access |
| Targeting | Acquire nearest valid opponent and retain a valid target | No damage application |
| Movement | Advance/chase and stop through Rigidbody2D | No attack timing |
| StateMachine/states | Unit behavior and explicit transitions | No shop or global phase decisions |
| Wave runner | Execute configured enemy spawn schedules; report schedule completion | Does not grant rewards or advance progression |
| Player base | Health and depletion notification | No reward decisions |
| EconomyManager | Own balance, affordability checks, grants and spending | No random item selection |
| ShopManager | Weighted selection, reroll cost and purchase count | Uses economy and container operations |
| GameFlowController | Own current wave, phase transitions and ordered coordination | Delegates bag, combat and shop work |
| PreparationPhase | Enable preparation and report Begin Battle request | Does not independently change the global phase |
| BattlePhase | Run production/wave activity and resolve battle outcome | Delegates spawning and cleanup |
| Presentation | Camera, HUD, feedback and visual effects | Observes gameplay; does not decide damage or rewards |

Use plain C# objects for unit subsystems where a MonoBehaviour is unnecessary.
CombatSoldier is a prefab MonoBehaviour. Use a generic small state machine.
Do not introduce a general service locator, global event bus, transaction
framework, or speculative extension architecture for these responsibilities.

## Configuration and runtime data

ScriptableObjects are read-only configuration at runtime. Explicit level
entries define progression; do not derive level stats using scaling formulas.
Runtime item/unit instances own their mutable values.

| Configuration | Contents |
|---|---|
| Bag config | Maximum dimensions, initially unlocked cells |
| SoldierConfig | Identity, footprint, per-level max health, base speed, acquisition range and visuals |
| WeaponConfig | Identity, footprint, per-level damage, attack interval/range, production duration, speed multiplier and visuals; melee/ranged designation |
| ShopConfig | Weighted item options, reroll batch size, cost list and reduced resale values |
| WaveConfig | Spawn entries, victory reward, defeat reward, ShopConfig |
| WorldConfig | Ordered WaveConfig references |

Footprints are relative integer cell offsets. A soldier bag item is 1 cell
wide and 2 cells tall. No rotation. Item identity is independent of instance
identity; two separately purchased copies can merge.

Runtime state includes item level/location, weapon elapsed production time,
unit health/target/state, active units, currency, current wave and reroll count.
Soldier item level supplies soldier stats; weapon level supplies weapon stats.
Combat speed uses soldier base speed and the weapon speed multiplier.

## Bag rules

- Each occupied cell references the same item instance for a multi-cell item.
- Placement must stay inside the grid and use unlocked cells.
- The bag is locked during battle and during phase transitions.
- Picking up an item releases its occupied cells or container slot while
  retaining enough information to restore its original location.
- Validate the complete result before committing occupancy changes.
- Empty valid destination: place the dragged item.
- One matching identity and level: merge if a next level exists. Validate the
  resulting footprint before consuming either item.
- One incompatible obstructing item: move it to the bottom container, then
  place the dragged item. This is replacement, not a position swap.
- More than one distinct obstructing item: reject.
- Any invalid drop: restore the item to its original source.
- A same-level maximum-level match cannot merge. Its exact drop behavior is
  unresolved; confirm it when implementing placement.
- Container items can be dragged into the bag, and bag items into the container.
- Expansion pieces unlock 1x1, 1x2 or 2x1 cells. Every cell must be locked and
  in bounds, and the new area must share an edge with an unlocked cell.

## Production

1. Resolve connections when battle begins, because the bag then stays locked.
2. A weapon activates only with at least one orthogonally adjacent SoldierItem.
3. Deduplicate neighboring SoldierItem instances: multiple touching edges count
   as one connection. Diagonal contact never counts.
4. Each weapon owns one timer, regardless of adjacent soldier count.
5. At completion, request one combat soldier per unique adjacent SoldierItem.
6. Use that soldier item's level and the producing weapon's level/config.
7. Restart the weapon timer while battle remains active.

Production progress drives visual filling; the fill effect never owns time.
Timer reset behavior between battles must be settled before this feature.

## Combat

Friendly units spawn at tent transforms on the left and advance toward +X.
Enemies spawn off-screen to the right and advance toward -X.
Acquisition range may exceed attack range. Acquire the nearest valid opponent,
retain it while valid, chase outside attack range, stop and attack in range.
After target loss, acquire again or resume advancing.

States: Idle, Walk, MeleeAttack, RangedAttack, Death.
Walk handles advancing and chasing. States request transitions through the
state machine. Share attack behavior only where actual duplication warrants it.
Health depletion interrupts any active living state.

Both melee and ranged attacks apply direct damage. Damage timing is gameplay
logic, independent of animation/tween completion. Death or interruption cancels
or invalidates pending attacks and delayed callbacks.

Use Rigidbody2D and body Collider2D initially. Use reusable buffers and throttled
queries for acquisition where appropriate. Add simple separation only if
playtesting shows a need. Y-based render sorting does not solve collisions.

## Spawn and recycle lifecycle

1. SoldierSpawner acquires an inactive pooled unit.
2. Initialize team, stats, weapon, health, movement, targeting and visual state.
3. Activate behavior only after initialization; enter Idle, then Walk.
4. Track the unit as active until it is recycled.

Normal death: health depleted -> DeathState -> visual death completes ->
CombatSoldier reports completion -> SoldierSpawner releases the pooled unit.
Death presentation never calls the pool directly.
Wave cleanup: stop producers and scheduled spawns, then ask SoldierSpawner to
recycle all remaining units directly, without requiring death animations.
Recycle clears target, timers, velocity, subscriptions, tweens and coroutines.
Delayed callbacks must not affect a later reuse of the same pooled instance.
A unit is released at most once, including overlapping death and cleanup.

Enemies reaching the base deal damage and recycle. Friendlies reaching the
far-right boundary recycle. Base-contact damage amount/cadence remains open.

## Shop and economy

Reroll is the purchase action. When affordable and preparation is active:
validate generation/configuration, replace all container contents with a
weighted random batch (approximately 2–3 items), deduct the configured cost,
and increment purchase count. Existing container items receive no compensation;
this includes items deliberately moved out of the bag. Bag items stay intact.
Failure must not consume currency or discard items.

Reroll costs follow an explicit list. Reset purchase count on advancement to
a new wave, not on defeat/retry. Cost-list exhaustion behavior remains open.
Begin Battle sells all remaining container items for configured reduced values
and clears the container once. Economy changes notify UI independently.

## Phase and wave flow

Begin Battle, in order:
1. Lock preparation input and prevent duplicate start requests.
2. Sell and clear remaining container items once.
3. Resolve production connections and prepare wave data.
4. Animate camera position and orthographic size simultaneously with DOTween.
5. On transition completion, start weapon production and enemy scheduling.

Keep the bag at its authored scale. Camera framing makes it appear smaller
during battle, where its production progress remains visible.

Wave entries define start time, soldier/weapon configs and levels, count and
spawn interval. Entries may overlap.
Victory requires schedules finished, no active enemies, and a living base.
Defeat occurs when base health reaches zero. Resolve an outcome only once;
a dead base cannot produce victory. Count dying tracked enemies consistently
until recycling completes, unless a later documented decision changes this.

On outcome: stop production and enemy spawning, recycle units, grant the
appropriate reward once, return camera/input to preparation through the flow
controller. Victory advances the wave and resets reroll progression. Defeat
retains the bag and purchase count, restores base health, grants consolation
currency and allows retrying the same wave.
Base restoration on victory and final-wave completion behavior remain open.
An optional reward for surviving friendlies is not included in the baseline.
Bosses may start as larger units with stronger stats and a large health bar.

## Presentation and implementation boundaries

Use direct calls for ordered operations; events for independent UI/audio feedback.
Initial effects: weapon fill, white unit hit flash, DOTween recoil/bounce/death.
A small custom sprite shader can supply fill/flash; no paid shader package is
required. Choose sprite versus Canvas rendering before implementing the bag.
World-space camera framing does not itself require a specific bag renderer.

## Decisions still to settle when relevant

- Unity version/render pipeline, target platform and bag rendering approach.
- Input cancellation on disable/phase change; max-level matching drop behavior.
- Production timer reset between battles and catch-up after long frames.
- Damage point within attack cadence and base-contact damage.
- Reroll cost-list exhaustion, starting currency and initial item configurations.
- Base health on victory, final-wave screen, and whether persistence is in scope.

These are design gaps, not authorization to add systems. Resolve only the gaps
that affect the next implementation step.
