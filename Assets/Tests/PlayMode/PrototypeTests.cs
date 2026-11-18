using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Rise.Tests
{
    public sealed class PrototypeTests
    {
        private RiseGame game;
        private CourseSpawner course;

        [UnitySetUp]
        public IEnumerator LoadPrototype()
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("RisePrototype");
            yield return null;
            game = Object.FindObjectOfType<RiseGame>();
            course = Object.FindObjectOfType<CourseSpawner>();
            Assert.That(game, Is.Not.Null);
            // These tests drive the game directly; editor/phone input must not restart a test run.
            Object.FindObjectOfType<WindSwipeInput>().enabled = false;
            GameObject.Find("EventSystem").SetActive(false);
            course.enabled = false;
        }

        [UnityTest]
        public IEnumerator WaitsForFirstSwipeThenLaunches()
        {
            Vector3 start = game.Kite.transform.position;
            float cameraStart = game.FollowCamera.transform.position.y;
            yield return new WaitForSeconds(0.15f);
            Assert.That(game.State, Is.EqualTo(RunState.Waiting));
            Assert.That(game.Kite.transform.position, Is.EqualTo(start));
            Assert.That(game.FollowCamera.transform.position.y, Is.EqualTo(cameraStart));
            game.ApplySwipe(Vector2.up * 4f);
            yield return new WaitForSeconds(0.1f);
            Assert.That(game.State, Is.EqualTo(RunState.Playing));
            Assert.That(game.Kite.transform.position.y, Is.GreaterThan(start.y));
            Assert.That(game.FollowCamera.transform.position.y, Is.GreaterThan(cameraStart));
        }

        [UnityTest]
        public IEnumerator GravityReducesUpwardVelocity()
        {
            game.ApplySwipe(Vector2.up * 4f);
            yield return new WaitForFixedUpdate();
            float initialVelocity = game.Kite.Velocity.y;
            yield return new WaitForSeconds(0.2f);
            Assert.That(game.Kite.Velocity.y, Is.LessThan(initialVelocity));
            Assert.That(game.Kite.Velocity.y, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator SideEdgeReflectsVelocity()
        {
            game.ApplySwipe(Vector2.up);
            Rigidbody2D body = game.Kite.GetComponent<Rigidbody2D>();
            body.position = new Vector2(game.FollowCamera.HalfWidth + 0.2f, 3f);
            body.velocity = new Vector2(3f, 1f);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(body.velocity.x, Is.LessThan(0f));
            Assert.That(body.position.x, Is.LessThan(game.FollowCamera.HalfWidth));
        }

        [Test]
        public void CameraNeverMovesBackDown()
        {
            game.FollowCamera.Advance(20f, 0.1f);
            float highest = game.FollowCamera.transform.position.y;
            Assert.That(game.FollowCamera.Camera.WorldToViewportPoint(new Vector3(0f, 20f, 0f)).y,
                Is.EqualTo(0.54f).Within(0.001f));
            game.FollowCamera.Advance(2f, 0.1f);
            Assert.That(game.FollowCamera.transform.position.y - highest, Is.EqualTo(0.05f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator HazardWithoutShieldEndsRun()
        {
            game.ApplySwipe(Vector2.up);
            course.CreateSaw(game.Kite.transform.position);
            yield return new WaitForSeconds(0.1f);
            Assert.That(game.State, Is.EqualTo(RunState.GameOver));
        }

        [UnityTest]
        public IEnumerator BubbleGrantsOneShieldAndShieldsDoNotStack()
        {
            game.ApplySwipe(Vector2.up * 0.1f);
            Rigidbody2D body = game.Kite.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.velocity = Vector2.zero;
            course.CreateShield(body.position);
            yield return new WaitForSeconds(0.1f);
            Assert.That(game.Kite.HasShield, Is.True);
            course.CreateShield(body.position);
            yield return new WaitForSeconds(0.1f);
            Assert.That(game.Kite.HasShield, Is.True);
            GameObject firstSaw = course.CreateSaw(body.position);
            yield return new WaitForSeconds(0.1f);
            Assert.That(game.State, Is.EqualTo(RunState.Playing));
            Assert.That(game.Kite.HasShield, Is.False);
            Object.Destroy(firstSaw);
            yield return new WaitForSeconds(1f);
            course.CreateSaw(body.position);
            yield return new WaitForSeconds(0.1f);
            Assert.That(game.State, Is.EqualTo(RunState.GameOver));
        }

        [UnityTest]
        public IEnumerator ShieldBreakBlinksUntilProtectionExpires()
        {
            game.ApplySwipe(Vector2.up * 0.1f);
            Rigidbody2D body = game.Kite.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.velocity = Vector2.zero;
            SpriteRenderer art = game.Kite.transform.Find("Kite art").GetComponent<SpriteRenderer>();
            game.Kite.CollectShield();
            float hitTime = Time.time;
            game.Kite.HitHazard();
            Assert.That(game.Kite.HasShield, Is.False);
            Assert.That(art.enabled, Is.False, "The shield break should start the blink immediately.");
            Assert.That(body.simulated, Is.True);
            Assert.That(game.Kite.GetComponent<Collider2D>().enabled, Is.True);

            bool sawVisibleFrame = false;
            bool sawHiddenFrame = false;
            while (Time.time < hitTime + 0.8f)
            {
                yield return null;
                sawVisibleFrame |= art.enabled;
                sawHiddenFrame |= !art.enabled;
            }
            Assert.That(sawVisibleFrame && sawHiddenFrame, Is.True, "The kite should alternate visibility.");
            game.Kite.HitHazard();
            Assert.That(game.State, Is.EqualTo(RunState.Playing), "Protection should outlast the old 0.6-second window.");

            while (Time.time < hitTime + 0.95f) yield return null;
            yield return null;
            Assert.That(art.enabled, Is.True, "The kite should stay visible once protection ends.");
            game.Kite.HitHazard();
            Assert.That(game.State, Is.EqualTo(RunState.GameOver), "Hazards must become lethal again after the blink.");
        }

        [Test]
        public void EndingAndRestartingDuringShieldBlinkRestoresKite()
        {
            game.ApplySwipe(Vector2.up);
            game.Kite.CollectShield();
            game.Kite.HitHazard();
            SpriteRenderer art = game.Kite.transform.Find("Kite art").GetComponent<SpriteRenderer>();
            Assert.That(art.enabled, Is.False);
            game.EndRun("Test");
            Assert.That(art.enabled, Is.True);
            game.ResetRun();
            Assert.That(art.enabled, Is.True);
            game.ApplySwipe(Vector2.up);
            game.Kite.HitHazard();
            Assert.That(game.State, Is.EqualTo(RunState.GameOver), "Restart must clear the previous run's protection.");
        }

        [UnityTest]
        public IEnumerator FallingOffScreenEndsRunEvenWithShield()
        {
            game.ApplySwipe(Vector2.up);
            game.Kite.CollectShield();
            game.Kite.GetComponent<Rigidbody2D>().position = new Vector2(0f, game.FollowCamera.Bottom - 2f);
            yield return new WaitForFixedUpdate();
            yield return null;
            yield return null;
            Assert.That(game.State, Is.EqualTo(RunState.GameOver));
        }

        [UnityTest]
        public IEnumerator HeightNeverDecreasesAndRestartClearsRun()
        {
            game.ApplySwipe(Vector2.up * 4f);
            yield return new WaitForSeconds(0.4f);
            float peak = game.MaximumHeight;
            game.ApplySwipe(Vector2.down * 5f);
            yield return new WaitForSeconds(0.15f);
            Assert.That(game.MaximumHeight, Is.GreaterThanOrEqualTo(peak));
            game.Kite.CollectShield();
            game.EndRun("Test");
            game.ResetRun();
            Assert.That(game.State, Is.EqualTo(RunState.Waiting));
            Assert.That(game.MaximumHeight, Is.Zero);
            Assert.That(game.Kite.HasShield, Is.False);
            Assert.That(game.Kite.Velocity, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void AllSpritesKeepTheSamePixelScale()
        {
            foreach (SpriteRenderer renderer in Object.FindObjectsOfType<SpriteRenderer>(true))
            {
                Assert.That(renderer.sprite.pixelsPerUnit, Is.EqualTo(16f), renderer.name);
                Assert.That(renderer.transform.lossyScale, Is.EqualTo(Vector3.one), renderer.name);
            }
        }

        [UnityTest]
        public IEnumerator TiltKeepsPhysicsBodyUprightAndShieldAligned()
        {
            game.ApplySwipe(new Vector2(1f, 3f));
            game.Kite.CollectShield();
            yield return new WaitForSeconds(0.2f);

            Rigidbody2D body = game.Kite.GetComponent<Rigidbody2D>();
            Transform art = game.Kite.transform.Find("Kite art");
            Assert.That(art, Is.Not.Null);
            Assert.That(game.Kite.GetComponent<SpriteRenderer>(), Is.Null);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, body.rotation)), Is.LessThan(0.001f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, game.Kite.transform.eulerAngles.z)), Is.LessThan(0.001f));
            Assert.That(Mathf.DeltaAngle(0f, art.localEulerAngles.z), Is.LessThan(-1f));
            Transform shield = art.Find("Active shield");
            Assert.That(shield.gameObject.activeSelf, Is.True);
            Assert.That(shield.position, Is.EqualTo(art.position));
            Assert.That(shield.rotation, Is.EqualTo(art.rotation));

            game.ResetRun();
            Assert.That(art.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(shield.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator CoastingRendersMotionBetweenPhysicsSteps()
        {
            float originalStep = Time.fixedDeltaTime;
            try
            {
                // Deliberately separate render and physics rates to expose pose resets.
                Time.fixedDeltaTime = 0.1f;
                game.ApplySwipe(new Vector2(0.2f, 1f));
                Rigidbody2D body = game.Kite.GetComponent<Rigidbody2D>();
                body.gravityScale = 0f;
                body.drag = 0f;
                yield return new WaitForSeconds(0.25f);

                float previousPhysicsY = body.position.y;
                float previousRenderedY = game.Kite.transform.position.y;
                int interpolatedFrames = 0;
                for (int i = 0; i < 24; i++)
                {
                    yield return null;
                    float physicsY = body.position.y;
                    float renderedY = game.Kite.transform.position.y;
                    if (Mathf.Approximately(physicsY, previousPhysicsY)
                        && renderedY > previousRenderedY + 0.0001f)
                        interpolatedFrames++;
                    previousPhysicsY = physicsY;
                    previousRenderedY = renderedY;
                }
                Assert.That(interpolatedFrames, Is.GreaterThan(0),
                    "The kite should render intermediate positions between fixed physics ticks.");
            }
            finally
            {
                Time.fixedDeltaTime = originalStep;
            }
        }

        [UnityTest]
        public IEnumerator HeightDifficultyTightensRowsCapsAndResets()
        {
            Assert.That(course.Difficulty, Is.Zero);
            game.FollowCamera.Advance(20f, 0.1f);
            AssertPassageClear();
            game.ResetRun();
            game.ApplySwipe(Vector2.up);
            Rigidbody2D body = game.Kite.GetComponent<Rigidbody2D>();
            float launchY = body.position.y;

            // Teleport without simulating so this checks course progression, not survival.
            body.simulated = false;
            body.position = new Vector2(0f, launchY + 85f / 3f);
            game.Kite.transform.position = body.position;
            yield return null;
            yield return null;
            Assert.That(course.Difficulty, Is.EqualTo(0.5f).Within(0.001f));
            body.position = new Vector2(0f, launchY + 80f / 3f);
            game.Kite.transform.position = body.position;
            yield return null;
            yield return null;
            Assert.That(course.Difficulty, Is.EqualTo(0.5f).Within(0.001f),
                "Descending must not reduce difficulty.");

            body.position = new Vector2(0f, launchY + 50f);
            game.Kite.transform.position = body.position;
            yield return null;
            yield return null;
            Assert.That(course.Difficulty, Is.EqualTo(1f).Within(0.001f));
            body.position = new Vector2(0f, launchY + 100f);
            game.Kite.transform.position = body.position;
            yield return null;
            yield return null;
            Assert.That(course.Difficulty, Is.EqualTo(1f + Mathf.SmoothStep(0f, 1f, 150f / 350f)).Within(0.001f));
            body.position = new Vector2(0f, launchY + 150f);
            game.Kite.transform.position = body.position;
            yield return null;
            yield return null;
            Assert.That(course.Difficulty, Is.EqualTo(1.5f).Within(0.001f));

            body.position = new Vector2(0f, launchY + 500f);
            game.Kite.transform.position = body.position;
            yield return null;
            yield return null;
            Assert.That(course.Difficulty, Is.EqualTo(2f));
            body.position = new Vector2(0f, launchY + 630f);
            game.Kite.transform.position = body.position;
            yield return null;
            yield return null;
            Assert.That(course.Difficulty, Is.EqualTo(2f));
            AssertPassageClear();

            // Check actual moving hazards remain on-screen at the highest difficulty.
            for (int i = 0; i < 12; i++)
            {
                yield return new WaitForSeconds(0.1f);
                foreach (CourseItem saw in course.GetComponentsInChildren<CourseItem>().Where(item => !item.IsShield))
                    Assert.That(Mathf.Abs(saw.transform.position.x),
                        Is.LessThanOrEqualTo(game.FollowCamera.HalfWidth + 0.001f),
                        "Saw centers must stay inside the border so at least half remains visible.");
            }
            Assert.That(body.gravityScale, Is.EqualTo(0.38f));
            Assert.That(body.drag, Is.EqualTo(0.4f));
            game.ResetRun();
            Assert.That(course.Difficulty, Is.Zero);
            game.FollowCamera.Advance(20f, 0.1f);
            AssertPassageClear();
        }

        [UnityTest]
        public IEnumerator ShieldsSpawnEveryFiftyHeightThroughDifficultyRampAndRestart()
        {
            game.ApplySwipe(Vector2.up);
            Rigidbody2D body = game.Kite.GetComponent<Rigidbody2D>();
            float launchY = body.position.y;
            body.simulated = false;
            course.enabled = true;
            course.Advance();
            Assert.That(course.GetComponentsInChildren<CourseItem>().Count(item => item.IsShield), Is.Zero);

            for (int height = 50; height <= 550; height += 50)
            {
                body.position = new Vector2(0f, launchY + height);
                game.Kite.transform.position = body.position;
                yield return null;
                yield return null;
                CourseItem[] shields = course.GetComponentsInChildren<CourseItem>().Where(item => item.IsShield).ToArray();
                Assert.That(shields.Length, Is.EqualTo(1), "One bubble should appear at each 50-height milestone.");
                Assert.That(shields[0].transform.position.y - launchY, Is.EqualTo(height).Within(0.001f));
                foreach (CourseItem saw in course.GetComponentsInChildren<CourseItem>().Where(item => !item.IsShield))
                    Assert.That(new Vector2(Mathf.Max(0f, Mathf.Abs(saw.Origin.x - shields[0].transform.position.x) - saw.Travel), saw.Origin.y - shields[0].transform.position.y).magnitude,
                        Is.GreaterThanOrEqualTo(1.399f), "Shield levels must have room clear of moving saws.");
                course.Advance();
                course.Advance();
                Assert.That(course.GetComponentsInChildren<CourseItem>().Count(item => item.IsShield), Is.EqualTo(1),
                    "Repeated spawning must not duplicate a milestone bubble.");
            }

            game.ResetRun();
            yield return null;
            Assert.That(course.GetComponentsInChildren<CourseItem>().Count(item => item.IsShield), Is.Zero);
            game.ApplySwipe(Vector2.up);
            body.simulated = false;
            body.position = new Vector2(0f, launchY + 50f);
            game.Kite.transform.position = body.position;
            yield return null;
            yield return null;
            CourseItem first = course.GetComponentsInChildren<CourseItem>().Single(item => item.IsShield);
            Assert.That(first.transform.position.y - launchY, Is.EqualTo(50f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator BorderSawsCoverBothSidesAndStayHalfVisible()
        {
            Random.State previousRandomState = Random.state;
            try
            {
                Random.InitState(730);
                game.ApplySwipe(Vector2.up);
                Rigidbody2D body = game.Kite.GetComponent<Rigidbody2D>();
                body.simulated = false;
                course.enabled = true;
                float edge = game.FollowCamera.HalfWidth;
                bool sawLeft = false;
                bool sawRight = false;
                bool sawPair = false;
                for (int height = 20; height <= 220; height += 10)
                {
                    body.position = new Vector2(0f, height);
                    game.Kite.transform.position = body.position;
                    yield return null;
                    yield return null;
                    CourseItem[] generated = course.GetComponentsInChildren<CourseItem>().Where(item => !item.IsShield).ToArray();
                    for (int a = 0; a < generated.Length; a++)
                        for (int b = a + 1; b < generated.Length; b++)
                        {
                            float dy = Mathf.Abs(generated[a].Origin.y - generated[b].Origin.y);
                            float dx = Mathf.Max(0f, Mathf.Abs(generated[a].Origin.x - generated[b].Origin.x) - generated[a].Travel - generated[b].Travel);
                            Assert.That(new Vector2(dx, dy).magnitude, Is.GreaterThanOrEqualTo(1.499f));
                            sawPair |= dy < 0.001f;
                        }
                    foreach (CourseItem saw in generated)
                    {
                        Assert.That(Mathf.Abs(saw.Origin.x - course.PassageX(saw.Origin.y)) - saw.Travel,
                            Is.GreaterThanOrEqualTo(0.6f + game.Kite.Radius + 0.19f));
                        if (saw.Travel > 0f) Assert.That(saw.AverageSpeed, Is.InRange(0.55f, 1.401f));
                        float x = saw.transform.position.x;
                        Assert.That(Mathf.Abs(x), Is.LessThanOrEqualTo(edge + 0.001f));
                        sawLeft |= x < -edge + 0.8f;
                        sawRight |= x > edge - 0.8f;
                    }
                }
                Assert.That(sawPair, Is.True, "The course should include side-by-side saws.");
                Assert.That(sawLeft && sawRight, Is.True, "The generated course must threaten both borders.");

                course.enabled = false;
                game.ResetRun();
                yield return null;
                game.ApplySwipe(Vector2.up);
                body.simulated = false;
                body.position = new Vector2(0f, 5f);
                game.Kite.transform.position = body.position;
                GameObject left = course.CreateSaw(new Vector2(-edge + 1f, 5f), 1, 1f, 1.15f, -Mathf.PI * 0.5f);
                GameObject right = course.CreateSaw(new Vector2(edge - 1f, 5f), 1, 1f, 1.15f, Mathf.PI * 0.5f);
                Assert.That(left.transform.position.x, Is.EqualTo(-edge).Within(0.001f));
                Assert.That(right.transform.position.x, Is.EqualTo(edge).Within(0.001f));
                bool sweptInward = false;
                for (int i = 0; i < 30; i++)
                {
                    yield return new WaitForSeconds(0.2f);
                    Assert.That(Mathf.Abs(left.transform.position.x), Is.LessThanOrEqualTo(edge + 0.001f));
                    Assert.That(Mathf.Abs(right.transform.position.x), Is.LessThanOrEqualTo(edge + 0.001f));
                    sweptInward |= right.transform.position.x < edge - 1.5f;
                }
                Assert.That(sweptInward, Is.True);
            }
            finally
            {
                Random.state = previousRandomState;
            }
        }

        [UnityTest]
        public IEnumerator BorderSawHitsKiteHuggingTheSide()
        {
            game.ApplySwipe(Vector2.up * 0.1f);
            Rigidbody2D body = game.Kite.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.velocity = Vector2.zero;
            float edge = game.FollowCamera.HalfWidth;
            body.position = new Vector2(edge - game.Kite.Radius, 3f);
            course.CreateSaw(new Vector2(edge, 3f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(game.State, Is.EqualTo(RunState.GameOver), "Hugging a border must not evade its saw.");
        }

        private void AssertPassageClear()
        {
            course.enabled = true;
            course.Advance();
            course.enabled = false;
            foreach (CourseItem saw in course.GetComponentsInChildren<CourseItem>().Where(item => !item.IsShield))
            {
                float clearance = Mathf.Abs(saw.Origin.x - course.PassageX(saw.Origin.y)) - saw.Travel;
                Assert.That(clearance, Is.GreaterThanOrEqualTo(0.6f + game.Kite.Radius + 0.19f));
                if (saw.Travel > 0f) Assert.That(saw.AverageSpeed, Is.InRange(0.55f, 1.401f));
            }
        }
    }
}
