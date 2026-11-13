using UnityEngine;

namespace Rise
{
    public sealed class CourseItem : MonoBehaviour
    {
        public bool IsShield { get; private set; }
        private RiseGame game;
        private Vector3 origin;
        private float phase;
        private float amplitude;
        private float elapsed;
        private Transform artwork;

        public void Initialize(RiseGame owner, bool shield, Transform visual, float horizontalTravel)
        {
            game = owner;
            IsShield = shield;
            artwork = visual;
            origin = transform.position;
            amplitude = horizontalTravel;
            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (game == null || game.State != RunState.Playing) return;
            elapsed += Time.deltaTime;
            if (!IsShield)
            {
                transform.position = origin + Vector3.right * (Mathf.Sin(elapsed * 0.65f + phase) * amplitude);
                artwork.Rotate(0f, 0f, 220f * Time.deltaTime);
            }
        }

        public void Consume()
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
