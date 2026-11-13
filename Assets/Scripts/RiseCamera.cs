using UnityEngine;

namespace Rise
{
    [RequireComponent(typeof(Camera))]
    public sealed class RiseCamera : MonoBehaviour
    {
        [SerializeField] private float automaticSpeed = 0.42f;
        [SerializeField] private float followPosition = 0.58f;
        private const float PortraitAspect = 9f / 16f;
        private Vector3 startPosition;
        public Camera Camera { get; private set; }
        public float Bottom => transform.position.y - Camera.orthographicSize;
        public float Top => transform.position.y + Camera.orthographicSize;
        public float HalfWidth => Camera.orthographicSize * PortraitAspect;

        private void Awake()
        {
            Camera = GetComponent<Camera>();
            startPosition = transform.position;
            UpdateViewport();
        }

        private void Update() => UpdateViewport();

        private void UpdateViewport()
        {
            float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            if (screenAspect >= PortraitAspect)
            {
                float width = PortraitAspect / screenAspect;
                Camera.rect = new Rect((1f - width) * 0.5f, 0f, width, 1f);
            }
            else
            {
                float height = screenAspect / PortraitAspect;
                Camera.rect = new Rect(0f, (1f - height) * 0.5f, 1f, height);
            }
            Camera.aspect = PortraitAspect;
        }

        public void Advance(float kiteHeight, float deltaTime)
        {
            float followHeight = kiteHeight - (followPosition - 0.5f) * Camera.orthographicSize * 2f;
            Vector3 position = transform.position;
            position.y = Mathf.Max(position.y + automaticSpeed * deltaTime, followHeight);
            transform.position = position;
        }

        public void ResetView() => transform.position = startPosition;
    }
}
