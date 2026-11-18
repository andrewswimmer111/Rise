# Random-looking saw placement without heavy clustering

Research date: November 18, 2026. Game checkpoint: `b969545`.

Status: research and proposed design, not a replacement spawner. The checkpoint built successfully, was installed, and launched on the iPhone. No gameplay code changed during this research.

## Recommendation

Use **two-dimensional placement with minimum separation, a soft preference for filling neglected areas, and an independent navigation check**. Keep side-by-side saws possible. Keep occasional small groups. Suppress large clumps, long empty stretches, repeated rows, and persistent easy lanes.

Blue-noise sampling is the most relevant mathematical starting point. It is a family of spatial distributions, not a complete level-design solution. For Rise, the important missing layer is how empty space connects over time as the kite and saws move.

Prototype a modest candidate sampler before implementing a sophisticated sampler. Compare it against the checkpoint on identical seeds and difficulty settings. Replace the current permanent central passage only once a replacement navigation check works. Simply deleting that safeguard would undermine the requested navigability.

## What “random” means here

There are three separate goals:

1. **Statistical randomness:** positions are independently sampled. Clumps and gaps are expected.
2. **Visual randomness:** the player cannot easily describe the pattern as rows, columns, evenly alternating sides, or repeating groups.
3. **Playable variety:** situations demand different achievable decisions without unreasonable concentration of hazards.

A distribution can satisfy one and fail the others. Independent randomness has clumps. A grid eliminates clumps but exposes a pattern. A beautiful irregular point set can still form an unavoidable wall or leave a completely safe lane.

For a simple illustration, suppose 20 independently sampled points fall into four equal regions. The count in any one region is binomial, with mean 5 and standard deviation about 1.94. Uneven local counts are an ordinary consequence of this model, not evidence that the random-number generator is broken. This is a mathematical example, not a model fitted to Rise.

### What perception research contributes

Hurst's 2015 thesis experimentally studied selections in structured grids. Participants tended to spread selections out and avoid some alignments and edges. A model based on perceived coverage explained judgments of randomness. This supports investigating coverage as well as distances. It does **not** establish that the same preferences hold during fast moving gameplay, or that Rise should remove edge saws. [Hurst, University of Waterloo](https://uwspace.uwaterloo.ca/items/4ca67b76-8402-4e37-a72d-161c0c497a1c)

Marupudi and Varma studied human grouping of arrays containing 10–40 points. Their results identify several attributes involved in clustering, including area, density and linearity. This reinforces that a single nearest-neighbor threshold cannot describe everything a player sees as a cluster. The inspected evidence here is the published abstract, not the full paper. [Human visual clustering of point arrays](https://pubmed.ncbi.nlm.nih.gov/39666551/)

**Design inference:** repeated alignments, matching motion phases, and groups of red saws may make a layout feel organized even if positions are irregular. Evaluate screenshots and moving scenes. Do not treat a static psychology experiment as proof of game difficulty or enjoyment.

## Mathematical options and their tradeoffs

| Approach | Useful property | Main limitation for Rise |
|---|---|---|
| Independent uniform samples | Simple, unpredictable individual positions | Clumps and large gaps occur naturally |
| Jittered cells | Distributes samples across the region | Hidden grid structure; neighbors across cell boundaries can still be close |
| Minimum-distance / Poisson-disk-style placement | Prevents very close neighbors without assigning rows | Does not by itself bound the largest hole or guarantee a route |
| Candidate selection by distance | Easy to add; can favor underfilled areas | Always choosing the furthest point makes arrangements increasingly regular |
| Weighted sample elimination | Useful when choosing a target number from a larger candidate pool | More machinery, plus separate safety and navigation constraints |
| Multiple-class spacing | Can regulate red saws separately from the combined field | Global red percentage alone does not distribute red saws locally |
| Direction-dependent spacing | Can express different horizontal and vertical needs | Extreme vertical exclusion recreates one-saw-per-row behavior |

Bridson's algorithm grows a sample set using candidates in an annulus around existing samples, a background grid for local searches, and a minimum radius. For fixed dimension and candidate count, its generation cost is linear in the number of generated points. This is a useful reference implementation when minimum spacing is the main control. A game with additional rejection rules and disconnected allowed regions needs extra care; the same practical guarantees do not automatically carry over. [Bridson, 2007](https://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf)

PBRT's stratified sampler places one randomized sample in each subdivision. This improves coverage, but jittered samples can still approach one another or align in projection. For Rise, a grid can be a lookup structure without becoming a placement rule. “One item per cell” and “use cells to find neighbors efficiently” are different decisions. [PBRT, Stratified Sampler](https://www.pbr-book.org/4ed/Sampling_and_Reconstruction/Stratified_Sampler)

Yuksel's weighted sample elimination starts with excess candidates and removes crowded samples to obtain a desired count. The author explicitly notes that maximal coverage is not guaranteed. This is worth considering if count control becomes more important than a fixed exclusion radius, but it is not necessary for the first experiment. [Yuksel, 2015](https://www.cemyuksel.com/research/sampleelimination/)

Wei's multi-class work targets distributions whose individual classes and unions retain blue-noise characteristics. The relevant idea for Rise is to control the distribution of moving saws as well as all saws together. A 30% moving probability can still put several moving saws together. We do not need to reproduce the full published algorithm to test this principle. [Wei, 2010](https://www.liyiwei.org/papers/noise-sig10/)

“Blue noise” describes low power at low spatial frequencies, rather than a particular implementation. The graphics motivation is avoiding low-frequency artifacts; translating that to enjoyable game layouts remains an engineering hypothesis. [PBRT, Sampling Theory](https://www.pbr-book.org/4ed/Sampling_and_Reconstruction/Sampling_Theory)

## What the current Rise code actually does

Inspection of `Assets/Scripts/CourseSpawner.cs` at the checkpoint establishes:

- Generation uses 8-unit sections and targets 3–5 saws per section, subject to rejection.
- Ordinary candidates are ranked by their nearest **vertical** distance to existing saws. Horizontal separation does not contribute to this score.
- A 12% pair attempt explicitly reuses the preceding saw's height.
- A route point is added every 8 units. Its x coordinate changes by at most 0.6 and stays within ±1.1.
- A saw's **entire horizontal sweep** must stay away from the protected route segments.
- Swept saw intervals must remain at least 1.5 units apart in the current distance calculation.
- A candidate cannot have two existing saws within 1.5 vertical units, even when they are far apart horizontally.
- After 80 unsuccessful moving attempts, the next attempts use stationary candidates. Placement can still fail and omit an item.
- Shields reserve a small pocket at each 50-unit interval.

These observations explain several risks, rather than proving which one dominates the user's experience:

**Vertical ranking can restore a row-like rhythm.** It rewards a new y level even when two widely separated x positions at the same height would be useful. Special pair attempts compensate locally but do not change the underlying score.

**The route can concentrate saws beside an easy center.** It changes slowly and permanently excludes moving sweeps. The observed clustering may partly be a consequence of the allowed region, not inadequate randomness.

**Wider moving sweeps are strongly constrained.** At the reference half-width of 3, with a locally centered route, the protected center separation is:

`0.60 saw radius + 0.34 kite radius + 0.20 margin = 1.14`.

For a right-side saw with origin `x` and amplitude `A`, the constraints include `x - A > 1.14` and `x + A <= 3`. Combining them gives `A < 0.93`. Therefore some requested amplitudes up to 1.35 cannot fit beside a centered route. Route bends and neighboring saws can constrain this further. A route displaced toward one side can leave more room on the other; 0.93 is not a global bound on every spawn.

**Requested percentages are not realized percentages.** Moving candidates reject more often, then become stationary or disappear. Measuring only the probability parameter hides this bias. Record actual moving counts, accepted amplitudes, and rejection reasons.

## Why a cluster can make the game easier

This is a geometric explanation and a playtest hypothesis, not an experimentally established result for Rise.

For navigation, each saw excludes a region for the kite's center. Two nearby exclusion regions overlap. Their combined blocked area is the sum of their areas minus that overlap. Consequently, a group of hazards can occupy less unique space than the same hazards spread out.

More importantly, the group may ask for **one avoidance maneuver**. If every saw is on the right, a player can stay left through the entire encounter. The challenge is related to the number and timing of route decisions, not merely saw count. Conversely, a concentrated cluster near a bottleneck can be much harder or impossible. “Clusters are easy” is not a universal rule.

For moving hazards, spatial snapshots are insufficient. LaValle formulates moving-obstacle planning in configuration and time, with trajectories constrained to advance forward in time. Rise also needs velocity and control constraints: the kite cannot teleport sideways or reverse momentum instantly. [Planning Algorithms, §7.1.1](https://msl.cs.illinois.edu/~lavalle/planning/node312.html)

## Small reproducible experiment

Files: [comparison image](spacing-comparison.svg), [script](spacing-experiment.py), [raw results](spacing-experiment-results.json).

Run `python3 docs/spacing-experiment.py` from the repository root. It compares 24 points in a 6×24 rectangle, using seeds 0–99. All methods receive the same count and domain. The illustrated seed is 0, fixed before inspecting outputs.

| Method | Mean pairs closer than 0.8 | Mean nearest distance | Nearest-distance variation (CV) | Sampled maximum empty distance |
|---|---:|---:|---:|---:|
| Independent | 3.76 | 1.36 | 0.563 | 3.91 |
| Jittered cells | 0.75 | 1.75 | 0.356 | 2.72 |
| Minimum distance 1.25 | 0.00 | 1.84 | 0.246 | 3.30 |
| Best of 16 candidates | 0.00 | 2.28 | 0.174 | 2.46 |

The last column measures distance to the closest point at 576 probe locations, then takes the maximum. It is a sampled coverage metric, not an exact largest-empty-circle radius. CV is standard deviation divided by mean. Neither has a universal ideal value for a game.

The minimum-distance method eliminated close pairs by construction but still left substantial gaps. Best-of-16 filled space more evenly and also produced more uniform nearest-neighbor distances. Its zero close pairs in this experiment is an observation, not a mathematical guarantee. The experiment uses simple rejection sampling for minimum distance, not Bridson's algorithm.

Limitations: these are static points with no kite, saw size, route, camera, shields, or motion. They demonstrate the distinction between separation and coverage. They do not establish that any method is safer, harder, more random-looking to players, or ready to ship. No statistical significance or human preference is claimed.

## Proposed Rise design

### 1. Separate hard rules from soft preferences

Hard rules: preserve screen bounds, avoid unwanted saw overlap, preserve shield pockets, and retain a validated navigable continuation. Never reduce safety margins merely to hit a count target.

Soft preferences: discourage local overpopulation, long hazard-free stretches, repeated alignments, persistent one-sided placement, and adjacent moving saws. Allow exceptions that produce varied encounters. A side-by-side pair should be a possible outcome of two-dimensional spacing, not require exactly equal y coordinates.

### 2. Use a real two-dimensional score

For origin candidates, a simple optional distance measure is:

`d² = (Δx / rx)² + (Δy / ry)²`.

This defines elliptical separation. At equal heights, sufficient horizontal separation still permits a pair. Start near isotropic spacing; only increase vertical preference if playtesting shows it helps reaction time. Keep actual collision safety in world-space geometry rather than confusing this aesthetic metric with collision clearance.

Generate a small pool of legal candidates, score their **two-dimensional** density and coverage, then randomly select among good ones. A possible soft selection is `P(candidate i) ∝ exp(score_i / T)` after scores have been normalized. Higher T allows more variation; very low T approaches always taking the winner. This is our proposed heuristic, not a claim of implementing an exact blue-noise process.

Start by comparing 8 and 16 candidates and a few selection strengths. Avoid picking one arbitrary “optimal” constant without testing. Cap the benefit for an already sufficient empty gap so the algorithm does not obsessively create equal spacing.

### 3. Control local density at multiple scales

Inspect overlapping windows approximately half a screen, one screen, and two screens high. Penalize extremes relative to available area and the difficulty target, rather than require exactly the same count in every window. Shield pockets intentionally reduce available hazard area and should not provoke a compensating clump immediately above them.

Retain neighbors across generation boundaries. The 8-unit chunks may remain an implementation detail, but placement and density evaluation must span the seams. Never reposition already revealed hazards to make a later chunk fit.

Keep borders in the sampling domain. Do not add an alternating border quota. Measure realized occupancy near each edge, because rejection rules can create bias even when candidate x values are uniform.

### 4. Treat moving saws as trajectories

Keep the requested sine wave:

`x(t) = x0 + A sin(ωt + φ)`.

Its mean absolute speed over a cycle is `2Aω/π`, so choose average speed first and calculate `ω = πv_avg/(2A)`. The period is `4A/v_avg`. This preserves slow average movement with a wider excursion; zero speed at the endpoints is expected. It also means a broad, slow saw may barely traverse its range during a short encounter. Measure the portion of the sweep the player actually sees.

Randomize phases and evaluate simultaneous positions; several same-color saws moving together may form a visually coherent group. Keep moving saws a minority and add a soft moving-to-moving crowding penalty. Maintain separate random streams for course layout and cosmetic effects so changing clouds does not change the course used in a comparison.

### 5. Preserve navigation without preserving a permanent highway

First prototype: keep the current conservative safety check while testing the spacing score. This isolates aesthetic spacing improvements, but will not fully solve the center-lane and wide-sweep conflicts.

Second prototype: plan multiple possible connected passages that can shift across the width over a longer horizon. Validate reachable exits with the actual kite dynamics, rather than reserve one narrow central family forever. Do not demand arbitrary rapid left-right alternation.

For moving saws, validate over time with representative position/velocity states, swipe choices, gravity, drag, maximum speed, camera pressure and saw phase. A clear path in a screenshot is insufficient. A coarse sampled search is also not a formal guarantee: use conservative swept collision checks between steps, margins, and executable witness trajectories. Test several arrival times and plausible incoming velocities. Preserve the existing safeguard until this replacement is validated.

On failure, retry or remove an offending hazard in the unseen section. Never solve it by silently changing kite handling. Keep shield pockets small and check both approach and departure, not just the bubble's center.

## How to decide whether it works

Run the checkpoint and candidates at matched difficulty and comparable **realized** saw counts. Use at least several hundred seeds per difficulty band before selecting representative scenes; record worst cases, not only attractive examples.

Record these separately:

- Spatial: nearest-neighbor distances, close triples, empty-distance probes, density distributions at several scales, alignment counts, seam behavior, left/right and edge occupancy.
- Generation: requested versus actual counts, moving fraction, accepted amplitudes, rejection causes, fallback frequency, runtime on phone.
- Navigation: valid continuations, smallest clearance, required lateral travel and reversals, time available for action, longest easy lane, success of simple center-hugging and edge-hugging policies.
- Motion: simultaneous cluster size across time, phase similarity, and sweep distance visible during an encounter.
- Experience: separately rate perceived randomness, repetitive layout, fairness, and challenge. Do not collapse all four into “looks better.”

Use short, counterbalanced A/B phone sessions with the same starting conditions. Ask the player to identify where the layout felt repetitive, trivial or unfair, then inspect those seeds. Keep the count and speed knobs stable while comparing placement algorithms.

Suggested acceptance criteria: no safety-rule violations in the tested corpus; no impossible section in the validated state set; small shield pockets remain usable; side-by-side situations remain possible; no special border alternation; materially fewer large clumps and long empty runs than the checkpoint at comparable counts; no dominant center-hugging strategy; moving saws actually retain the intended sweep range. These are targets, not results already achieved.

## Decision to carry forward

The next useful implementation is **moderately spaced 2D candidate placement with soft density control**, followed by a separately validated change to route protection. Minimum distance alone will not solve the problem, and maximizing spacing everywhere risks another visibly formatted layout. The strongest Rise-specific finding is the interaction between the permanent passage and full-sweep exclusion. Addressing that deliberately matters more than replacing `Random.Range` with a different random generator.
