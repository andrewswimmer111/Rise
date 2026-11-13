using System.Collections;
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
            game.FollowCamera.Advance(2f, 0.1f);
            Assert.That(game.FollowCamera.transform.position.y, Is.GreaterThan(highest));
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
            yield return new WaitForSeconds(0.5f);
            course.CreateSaw(body.position);
            yield return new WaitForSeconds(0.1f);
            Assert.That(game.State, Is.EqualTo(RunState.GameOver));
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
    }
}
