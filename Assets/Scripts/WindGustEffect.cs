using UnityEngine;

namespace Rise
{
    public sealed class WindGustEffect : MonoBehaviour
    {
        [SerializeField] private Camera gameCamera;
        [SerializeField] private Sprite windPixel;
        [SerializeField] private int poolSize = 32;
        [SerializeField] private int lightGustCount = 4;
        [SerializeField] private int strongGustCount = 8;
        [SerializeField] private float lightStreakPixels = 8f;
        [SerializeField] private float strongStreakPixels = 16f;
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private float lightGustSpeed = 2.2f;
        [SerializeField] private float strongGustSpeed = 6.8f;
        [SerializeField] private float fullStrengthImpulse = 4.2f;

        private struct WindPixel
        {
            public SpriteRenderer Renderer;
            public Vector2 Velocity;
            public float Age;
            public float Lifetime;
            public bool Active;
        }

        private WindPixel[] pixels;
        private int nextPixel;
        // Visual randomness must not change the sequence of generated obstacles.
        private readonly System.Random random = new System.Random();

        private void Awake()
        {
            pixels = new WindPixel[Mathf.Max(1, poolSize)];
            for (int i = 0; i < pixels.Length; i++)
            {
                GameObject pixel = new GameObject("Wind streak");
                pixel.transform.SetParent(transform, false);
                SpriteRenderer renderer = pixel.AddComponent<SpriteRenderer>();
                renderer.sprite = windPixel;
                // Repeat the original pixel along the streak; keep its thickness and transform scale.
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.tileMode = SpriteTileMode.Continuous;
                renderer.sortingOrder = -2;
                renderer.enabled = false;
                pixels[i].Renderer = renderer;
            }
        }

        public void Emit(Vector2 impulse)
        {
            if (!isActiveAndEnabled || pixels == null || windPixel == null || gameCamera == null
                || impulse.sqrMagnitude < 0.001f) return;

            float strength = Mathf.Clamp01(impulse.magnitude / Mathf.Max(0.01f, fullStrengthImpulse));
            int count = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(lightGustCount, strongGustCount, strength)), 1, pixels.Length);
            float speed = Mathf.Lerp(lightGustSpeed, strongGustSpeed, strength);
            float halfHeight = gameCamera.orthographicSize;
            float halfWidth = halfHeight * gameCamera.aspect;
            Vector2 direction = impulse.normalized;
            Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            float pixelSize = 1f / windPixel.pixelsPerUnit;
            int columns = Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(count * gameCamera.aspect)));
            int rows = Mathf.CeilToInt((float)count / columns);

            for (int i = 0; i < count; i++)
            {
                ref WindPixel pixel = ref pixels[nextPixel];
                nextPixel = (nextPixel + 1) % pixels.Length;
                pixel.Age = 0f;
                pixel.Lifetime = Mathf.Max(0.1f, duration * Range(0.9f, 1.1f));
                pixel.Velocity = direction * speed * Range(0.85f, 1.15f);
                pixel.Active = true;
                // One streak per region prevents clumps; small offsets keep each gust natural.
                int row = i / columns;
                int columnsInRow = Mathf.Min(columns, count - row * columns);
                float x = (i % columns + 0.5f + Range(-0.2f, 0.2f)) / columnsInRow;
                float y = (row + 0.5f + Range(-0.2f, 0.2f)) / rows;
                pixel.Renderer.transform.localPosition = new Vector3((x * 2f - 1f) * halfWidth, (y * 2f - 1f) * halfHeight, 0f);
                pixel.Renderer.transform.localRotation = rotation;
                int length = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(lightStreakPixels, strongStreakPixels, strength) * Range(0.8f, 1.2f)));
                pixel.Renderer.size = new Vector2(length * pixelSize, pixelSize);
                pixel.Renderer.color = new Color(1f, 1f, 1f, 0f);
                pixel.Renderer.enabled = true;
            }
        }

        private void LateUpdate()
        {
            if (pixels == null || gameCamera == null) return;
            float halfHeight = gameCamera.orthographicSize;
            float halfWidth = halfHeight * gameCamera.aspect;
            for (int i = 0; i < pixels.Length; i++)
            {
                ref WindPixel pixel = ref pixels[i];
                if (!pixel.Active) continue;
                pixel.Age += Time.deltaTime;
                Transform visual = pixel.Renderer.transform;
                visual.localPosition += (Vector3)(pixel.Velocity * Time.deltaTime);
                Vector3 position = visual.localPosition;
                Vector3 extent = pixel.Renderer.bounds.extents;
                if (pixel.Age >= pixel.Lifetime || Mathf.Abs(position.x) > halfWidth + extent.x
                    || Mathf.Abs(position.y) > halfHeight + extent.y)
                {
                    pixel.Active = false;
                    pixel.Renderer.enabled = false;
                    continue;
                }

                float fadeIn = Mathf.Clamp01(pixel.Age / 0.04f);
                float fadeOut = Mathf.Clamp01((pixel.Lifetime - pixel.Age) / 0.2f);
                pixel.Renderer.color = new Color(1f, 1f, 1f, fadeIn * fadeOut);
            }
        }

        public void Clear()
        {
            if (pixels == null) return;
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i].Active = false;
                if (pixels[i].Renderer != null) pixels[i].Renderer.enabled = false;
            }
            nextPixel = 0;
        }

        private float Range(float minimum, float maximum) => Mathf.Lerp(minimum, maximum, (float)random.NextDouble());
        private void OnDisable() => Clear();
    }
}
