# Rise

A portrait Unity 2D game about keeping a kite aloft with swipe-controlled wind.

## Run the prototype

1. Open this project with **Unity 2022.3.62f3**.
2. Open `Assets/Scenes/RisePrototype.unity`.
3. Press **Play**. Click and drag inside the portrait Game view to swipe, or swipe on a touchscreen.
4. The first swipe launches the kite. Release each swipe to apply wind in that direction. Faster swipes are stronger; distance has a smaller effect. Each swipe sends a brief gust of 6–10 thin wind streaks across the screen, with base lengths of 6–12 pixels, fading after about half a second.
5. Keep climbing ahead of the camera. Touch shield bubbles to absorb one saw hit. Falling below the screen or hitting a saw without a shield ends the run. The screen turns black and shows your height. Tap or click that screen to restart.

The score displays the maximum height reached as a pixel-font number in the top left. For this first prototype, one world unit adds one point of height. This is a provisional display scale. On loss, only the centered score remains on a black screen.

Difficulty reaches the checkpoint's height-400 curve value at 100 and continues through the existing cap at 500, primarily by raising the density target from roughly 5 saws per 8 units at height 100 to 8 at 500. All saws remain stationary until maximum height 50. From then on, newly generated saws can move; most still remain stationary. Moving saws request 1–1.55 units of amplitude (2–3.1 end to end), retaining the slow average-speed ranges. Placement uses two-dimensional spacing and a capped randomized spacing score. There is no reserved central passage. A connected-component check rejects any chain of full swept hazard capsules that touches both side boundaries, including diagonal chains. Capsules include kite radius and a 0.16-unit comfort margin per side, rejecting slightly tighter openings. This is a conservative geometric safeguard, not a proof of comfortable physics-based playability. Shields retain small reserved pockets at randomized horizontal positions every 50 height.

## Artwork

The original exported PNGs are in `Assets/Art/Rise`. They were copied unchanged from Lucas's PixelArt exports. Every sprite uses **16 pixels per unit**, point filtering, uncompressed textures, and transform scale **(1, 1, 1)**. A 32-pixel asset remains twice the canvas size of a 16-pixel asset. Saw pivots center their visible artwork without resizing it.

The active shield uses `kite-shield.png`, aligned with the kite using a custom pivot for its extra bottom row. After the shield breaks, the kite blinks every 0.1 seconds for a 0.9-second invincibility window. Steady visibility signals that this protection has ended.

The starting composition follows the reference in [the game spec](docs/game-spec.md). Occasional clouds drift slowly across the blue sky, behind all gameplay. They use the three original cloud sprites at the same pixel scale as the other artwork, with softened opacity and only 12–22% of the foreground's vertical scrolling. Stationary hazards use `saw-grey`; moving hazards use the small red saw (`saw2`). Both spin at 360 degrees per second and retain the same pixel scale. Birds are not implemented yet.

## Code and tuning

- `WindSwipeInput`: touch and mouse gestures; swipe strength and cap.
- `WindGustEffect`: pooled streaks tiled from the original wind pixel at one-pixel thickness, aligned with each swipe and fading behind the kite and obstacles. Peak streak opacity is 50%, below the original roughly 78%, and can be tuned with Opacity in the Wind gusts Inspector. The wind pixel imports as opaque so this setting directly controls opacity while retaining the animated fade. Stronger swipes produce faster, longer, slightly denser gusts.
- `BackgroundClouds`: up to five decorative clouds, slow horizontal drift, and distant parallax. One starts in the sky; another enters from the side every 7–12 seconds when space is available.
- `KiteController`: momentum, gravity through Rigidbody2D, side bouncing, shield, and collisions. Interpolated physics stays on the parent; `Kite art` and its shield child tilt independently. Position is only corrected at a wall or reset, preserving interpolation during coasting.
- `RiseCamera`: portrait viewport, upward follow that keeps the kite around 54% of the screen height from the bottom, and automatic climb at 0.5 world units per second.
- `RiseGame`: waiting, playing, game over, maximum height, and restart.
- `CourseSpawner` / `CourseItem`: irregular placement, full-sweep spacing checks, connected navigation clearance, shield pockets, and sine-wave movement with a minimum cycle-average speed.
- `RiseHud` / `PixelScore`: numeric pixel-font score, black loss screen, and tap-to-restart.

Tune serialized values in the scene Inspector. Physics values, saw spacing/movement, camera speed, and the 0.9-second grace period after a shield breaks are initial playtest defaults, not settled design decisions. The kite string is visual, uses 25% opacity, and does not limit flight. The original `SampleScene` title screen is retained.

## Verify

With Play mode stopped, use **Rise → Run Prototype Tests**, or select `Rise.PlayModeTests` in Unity's Test Runner. Checks cover launch gating, gravity, side bouncing, camera follow point and scrolling speed, hazards, shield pickup and non-stacking, shield-break blinking/protection expiry/restart cleanup, shields every 50 height, falling off-screen, score/restart, common sprite scale, visual tilt isolation, coasting interpolation, difficulty progression through 500/reset, and border-saw visibility/collision. Tests disable live input so editor or phone taps cannot interfere. Menu-run results are written to `Temp/Rise-test-results.xml`.

Verified on November 18, 2026: **18 Unity play mode tests passed, 0 failed**. Coverage includes center placement, side-by-side spawning, swept spacing, diagonal wall rejection, cross-screen openings, wider sweep bounds, grey/red sprite selection, shield pockets, border collisions, accelerated difficulty and restart. The updated iOS export and Xcode build succeeded and the app was installed on the connected iPhone 16 Pro. Geometry checks do not substitute for momentum and difficulty playtesting.
