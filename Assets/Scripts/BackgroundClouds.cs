using UnityEngine;

namespace Rise
{
    public sealed class BackgroundClouds : MonoBehaviour
    {
        [SerializeField] private RiseCamera followCamera;
        [SerializeField] private Sprite[] cloudSprites;
        [SerializeField] private int maximumClouds = 5;
        [SerializeField] private Vector2 spawnInterval = new Vector2(7f, 12f);
        [SerializeField] private Vector2 driftSpeed = new Vector2(0.08f, 0.16f);
        [SerializeField] private Vector2 parallax = new Vector2(0.12f, 0.22f);

        private struct Cloud
        {
            public SpriteRenderer Renderer;
            public float Speed;
            public float Parallax;
            public float Opacity;
            public float Age;
        }

        private Cloud[] clouds;
        private float spawnCountdown;
        private float previousCameraHeight;
        // Decorative clouds must not change obstacle generation's random sequence.
        private readonly System.Random random = new System.Random();

        private void Awake()
        {
            if (cloudSprites == null || cloudSprites.Length == 0) return;
            clouds = new Cloud[Mathf.Max(1, maximumClouds)];
            for (int i = 0; i < clouds.Length; i++)
            {
                GameObject cloud = new GameObject("Background cloud");
                cloud.transform.SetParent(transform, false);
                SpriteRenderer renderer = cloud.AddComponent<SpriteRenderer>();
                renderer.sprite = cloudSprites[i % cloudSprites.Length];
                renderer.sortingOrder = -10;
                renderer.enabled = false;
                clouds[i].Renderer = renderer;
            }
        }

        public void ResetSky()
        {
            if (clouds == null || followCamera == null) return;
            foreach (Cloud cloud in clouds) cloud.Renderer.enabled = false;
            previousCameraHeight = followCamera.transform.position.y;
            spawnCountdown = Range(spawnInterval);
            Spawn(true);
        }

        // Called after camera movement so parallax has no frame of lag.
        public void Advance(float deltaTime)
        {
            if (!isActiveAndEnabled || clouds == null || followCamera == null) return;
            float cameraHeight = followCamera.transform.position.y;
            float cameraRise = cameraHeight - previousCameraHeight;
            previousCameraHeight = cameraHeight;
            float halfHeight = followCamera.Camera.orthographicSize;

            for (int i = 0; i < clouds.Length; i++)
            {
                ref Cloud cloud = ref clouds[i];
                if (!cloud.Renderer.enabled) continue;
                cloud.Age += deltaTime;
                Transform visual = cloud.Renderer.transform;
                // The parent follows the camera; only this fraction scrolls past the viewer.
                visual.localPosition += new Vector3(cloud.Speed * deltaTime, -cameraRise * cloud.Parallax, 0f);
                Vector3 bounds = cloud.Renderer.sprite.bounds.extents;
                if (visual.localPosition.x - bounds.x > followCamera.HalfWidth
                    || visual.localPosition.y + bounds.y < -halfHeight)
                {
                    cloud.Renderer.enabled = false;
                    continue;
                }
                cloud.Renderer.color = new Color(1f, 1f, 1f, cloud.Opacity * Mathf.Clamp01(cloud.Age / 1.5f));
            }

            spawnCountdown -= deltaTime;
            if (spawnCountdown <= 0f)
            {
                Spawn(false);
                spawnCountdown = Range(spawnInterval);
            }
        }

        private void Spawn(bool startingCloud)
        {
            for (int i = 0; i < clouds.Length; i++)
            {
                ref Cloud cloud = ref clouds[i];
                if (cloud.Renderer.enabled) continue;
                cloud.Renderer.sprite = cloudSprites[random.Next(cloudSprites.Length)];
                cloud.Renderer.flipX = random.Next(2) == 0;
                float depth = (float)random.NextDouble();
                cloud.Speed = Mathf.Lerp(driftSpeed.x, driftSpeed.y, depth);
                cloud.Parallax = Mathf.Lerp(parallax.x, parallax.y, depth);
                cloud.Opacity = Mathf.Lerp(0.5f, 0.68f, depth);
                cloud.Age = 0f;
                float x = startingCloud ? -followCamera.HalfWidth * 0.35f
                    : -followCamera.HalfWidth - cloud.Renderer.sprite.bounds.extents.x;
                float y = followCamera.Camera.orthographicSize * (startingCloud ? 0.55f : Range(new Vector2(0.05f, 0.85f)));
                cloud.Renderer.transform.localPosition = new Vector3(x, y, 0f);
                cloud.Renderer.color = new Color(1f, 1f, 1f, 0f);
                cloud.Renderer.enabled = true;
                return;
            }
        }

        private float Range(Vector2 range) => Mathf.Lerp(range.x, range.y, (float)random.NextDouble());
    }
}
