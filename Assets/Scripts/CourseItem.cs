using UnityEngine;

namespace Rise
{
    public sealed class CourseItem : MonoBehaviour
    {
        public bool IsShield { get; private set; }
        public Vector3 Origin => origin;
        public float Travel => amplitude;
        public float AverageSpeed => 2f * amplitude * speed / Mathf.PI;
        private RiseGame game;
        private Vector3 origin;
        private float phase;
        private float amplitude;
        private float speed;
        private float elapsed;
        private Transform artwork;

        public void Initialize(RiseGame owner, bool shield, Transform visual, float horizontalTravel,
            float horizontalSpeed = 0.65f, float? startingPhase = null)
        {
            game = owner;
            IsShield = shield;
            artwork = visual;
            origin = transform.position;
            amplitude = horizontalTravel;
            speed = horizontalSpeed;
            phase = startingPhase ?? Random.Range(0f, Mathf.PI * 2f);
            if (!IsShield) UpdatePosition();
        }

        private void Update()
        {
            if (game == null || game.State != RunState.Playing) return;
            elapsed += Time.deltaTime;
            if (!IsShield)
            {
                UpdatePosition();
                artwork.Rotate(0f, 0f, 360f * Time.deltaTime);
            }
        }

        private void UpdatePosition()
        {
            transform.position = origin + Vector3.right * (Mathf.Sin(elapsed * speed + phase) * amplitude);
        }

        public void Consume()
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
