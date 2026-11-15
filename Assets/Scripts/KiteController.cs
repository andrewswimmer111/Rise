using UnityEngine;

namespace Rise
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class KiteController : MonoBehaviour
    {
        [SerializeField] private RiseGame game;
        [SerializeField] private RiseCamera followCamera;
        [SerializeField] private GameObject shieldVisual;

        [Header("Flight Feel")]
        [SerializeField] private float maximumSpeed = 6f;
        [SerializeField] private float bounceRetention = 0.8f;
        [SerializeField, Tooltip("Rigidbody2D gravity scale applied at Awake. Lower is floatier.")]
        private float gravityScale = 0.55f;
        [SerializeField, Tooltip("Rigidbody2D linear drag applied at Awake. Air resistance on both axes.")]
        private float linearDrag = 0.4f;
        [SerializeField, Tooltip("Extra exponential drag applied only while falling. 0 disables.")]
        private float fallDamping = 1.6f;
        [SerializeField, Range(0f, 1f),
         Tooltip("When a swipe opposes current horizontal motion, shed this fraction of the old horizontal velocity before adding the impulse. Makes turning crisper.")]
        private float turnAssist = 0.55f;

        [Header("Visual Tilt")]
        [SerializeField, Tooltip("Child transform that holds the kite sprite. Rotated for visual tilt. Falls back to this transform if null.")]
        private Transform kiteVisual;
        [SerializeField] private float maxTiltAngle = 32f;
        [SerializeField, Tooltip("Degrees of tilt per 1 unit/s of horizontal velocity.")]
        private float tiltPerHorizontalSpeed = 10f;
        [SerializeField, Tooltip("How quickly the tilt chases its target. Higher is snappier.")]
        private float tiltResponsiveness = 9f;

        private Rigidbody2D body;
        private Transform tiltTarget;
        private Vector2 startingPosition;
        private float radius;
        private float graceUntil;
        private float currentTilt;

        public bool HasShield { get; private set; }
        public Vector2 Velocity => body.velocity;
        public float Radius => radius;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = gravityScale;
            body.drag = linearDrag;
            body.freezeRotation = true;
            startingPosition = body.position;
            radius = GetComponent<CircleCollider2D>().radius;
            tiltTarget = kiteVisual != null ? kiteVisual : transform;
        }

        public void ResetKite()
        {
            body.simulated = false;
            body.position = startingPosition;
            transform.position = startingPosition;
            body.velocity = Vector2.zero;
            graceUntil = 0f;
            currentTilt = 0f;
            ApplyTilt(0f);
            SetShield(false);
        }

        public void ApplyWind(Vector2 impulse)
        {
            body.simulated = true;
            Vector2 velocity = body.velocity;
            if (turnAssist > 0f
                && !Mathf.Approximately(impulse.x, 0f)
                && !Mathf.Approximately(velocity.x, 0f)
                && Mathf.Sign(impulse.x) != Mathf.Sign(velocity.x))
            {
                velocity.x *= 1f - turnAssist;
            }
            body.velocity = Vector2.ClampMagnitude(velocity + impulse, maximumSpeed);
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
            Vector2 velocity = body.velocity;

            // Softer falls: additional exponential drag only while descending.
            if (fallDamping > 0f && velocity.y < 0f)
                velocity.y /= 1f + fallDamping * Time.fixedDeltaTime;

            velocity = Vector2.ClampMagnitude(velocity, maximumSpeed);

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

        private void LateUpdate()
        {
            UpdateTilt(Time.deltaTime);
        }

        private void UpdateTilt(float dt)
        {
            // Lean into horizontal motion. Positive x velocity tilts the top of the kite rightward
            // (negative Z rotation in Unity 2D).
            float target = Mathf.Clamp(
                -body.velocity.x * tiltPerHorizontalSpeed,
                -maxTiltAngle,
                maxTiltAngle);
            // Framerate-independent exponential smoothing.
            float t = 1f - Mathf.Exp(-tiltResponsiveness * dt);
            currentTilt = Mathf.LerpAngle(currentTilt, target, t);
            ApplyTilt(currentTilt);
        }

        private void ApplyTilt(float angle)
        {
            if (tiltTarget == null) return;
            if (tiltTarget == transform)
                tiltTarget.rotation = Quaternion.Euler(0f, 0f, angle);
            else
                tiltTarget.localRotation = Quaternion.Euler(0f, 0f, angle);
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
