# Rise

A portrait Unity 2D game about keeping a kite aloft with swipe-controlled wind.

## Run the prototype

1. Open this project with **Unity 2022.3.62f3**.
2. Open `Assets/Scenes/RisePrototype.unity`.
3. Press **Play**. Click and drag inside the portrait Game view to swipe, or swipe on a touchscreen.
4. The first swipe launches the kite. Release each swipe to apply wind in that direction. Faster swipes are stronger; distance has a smaller effect. Each swipe sends a brief gust of 4–8 long, thin wind streaks across the screen, fading after about half a second.
5. Keep climbing ahead of the camera. Touch shield bubbles to absorb one saw hit. Falling below the screen or hitting a saw without a shield ends the run. The screen turns black and shows your height. Tap or click that screen to restart.

The score displays the maximum height reached as a pixel-font number in the top left. For this first prototype, one world unit adds one point of height. This is a provisional display scale. On loss, only the centered score remains on a black screen.

## Artwork

The original exported PNGs are in `Assets/Art/Rise`. They were copied unchanged from Lucas's PixelArt exports. Every sprite uses **16 pixels per unit**, point filtering, uncompressed textures, and transform scale **(1, 1, 1)**. A 32-pixel asset remains twice the canvas size of a 16-pixel asset. Saw pivots center their visible artwork without resizing it.

The active shield uses `kite-shield.png`, aligned with the kite using a custom pivot for its extra bottom row.

The starting composition follows the reference in [the game spec](docs/game-spec.md). Occasional clouds drift slowly across the blue sky, behind all gameplay. They use the three original cloud sprites at the same pixel scale as the other artwork, with softened opacity and only 12–22% of the foreground's vertical scrolling. The current hazards use only the small saw (`saw2`), spinning at 220 degrees per second. Birds are not implemented yet.

## Code and tuning

- `WindSwipeInput`: touch and mouse gestures; swipe strength and cap.
- `WindGustEffect`: pooled streaks tiled from the original wind pixel at one-pixel thickness, aligned with each swipe and fading behind the kite and obstacles. Stronger swipes produce faster, longer, slightly denser gusts.
- `BackgroundClouds`: up to five decorative clouds, slow horizontal drift, and distant parallax. One starts in the sky; another enters from the side every 7–12 seconds when space is available.
- `KiteController`: momentum, gravity through Rigidbody2D, side bouncing, shield, and collisions.
- `RiseCamera`: portrait viewport, upward follow, and automatic climb.
- `RiseGame`: waiting, playing, game over, maximum height, and restart.
- `CourseSpawner` / `CourseItem`: generate saws and shield bubbles ahead of the camera and remove items left behind.
- `RiseHud` / `PixelScore`: numeric pixel-font score, black loss screen, and tap-to-restart.

Tune serialized values in the scene Inspector. Physics values, saw spacing/movement, camera speed, and the 0.45-second grace period after a shield breaks are initial playtest defaults, not settled design decisions. The kite string is visual and does not limit flight. The original `SampleScene` title screen is retained.

## Verify

With Play mode stopped, use **Rise → Run Prototype Tests**, or select `Rise.PlayModeTests` in Unity's Test Runner. The nine checks cover launch gating, gravity, side bouncing, camera direction, hazards, shield pickup and non-stacking, falling off-screen, score/restart, and common sprite scale. Menu-run results are written to `Temp/Rise-test-results.xml`.

Verified in Unity on November 13, 2026: **9 passed, 0 failed**. A phone/device build has not been tested yet.
