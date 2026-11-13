using UnityEngine;

namespace Rise
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class KiteController : MonoBehaviour
    {
        [SerializeField] private RiseGame game;
        [SerializeField] private RiseCamera followCamera;
        [SerializeField] private GameObject shieldVisual;
        [SerializeField] private float maximumSpeed = 6f;
        [SerializeField] private float bounceRetention = 0.8f;
        private Rigidbody2D body;
        private Vector2 startingPosition;
        private float radius;
        private float graceUntil;
        public bool HasShield { get; private set; }
        public Vector2 Velocity => body.velocity;
        public float Radius => radius;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            startingPosition = body.position;
            radius = GetComponent<CircleCollider2D>().radius;
        }

        public void ResetKite()
        {
            body.simulated = false;
            body.position = startingPosition;
            transform.position = startingPosition;
            body.velocity = Vector2.zero;
            graceUntil = 0f;
            SetShield(false);
        }

        public void ApplyWind(Vector2 impulse)
        {
            body.simulated = true;
            body.velocity = Vector2.ClampMagnitude(body.velocity + impulse, maximumSpeed);
        }

        public void Freeze()
        {
            body.velocity = Vector2.zero;
            body.simulated = false;
        }

        private void FixedUpdate()
        {
            if (game.State != RunState.Playing) return;
            Vector2 position = body.position;
            Vector2 velocity = Vector2.ClampMagnitude(body.velocity, maximumSpeed);
            float edge = followCamera.HalfWidth - radius;
            if (position.x < -edge)
            {
                position.x = -edge;
                velocity.x = Mathf.Abs(velocity.x) * bounceRetention;
            }
            else if (position.x > edge)
            {
                position.x = edge;
                velocity.x = -Mathf.Abs(velocity.x) * bounceRetention;
            }
            body.position = position;
            body.velocity = velocity;
        }

        public void CollectShield()
        {
            if (game.State == RunState.Playing) SetShield(true);
        }

        public void HitHazard()
        {
            if (game.State != RunState.Playing || Time.time < graceUntil) return;
            if (HasShield)
            {
                SetShield(false);
                // Allow time to clear the same obstacle after the shield breaks.
                graceUntil = Time.time + 0.45f;
            }
            else game.EndRun("The kite hit a saw.");
        }

        private void SetShield(bool active)
        {
            HasShield = active;
            if (shieldVisual != null) shieldVisual.SetActive(active);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CourseItem item = other.GetComponent<CourseItem>();
            if (item == null || game.State != RunState.Playing) return;
            if (item.IsShield)
            {
                CollectShield();
                item.Consume();
            }
            else HitHazard();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            CourseItem item = other.GetComponent<CourseItem>();
            if (item != null && !item.IsShield) HitHazard();
        }
    }
}
