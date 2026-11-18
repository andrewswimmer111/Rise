using System.Collections.Generic;
using UnityEngine;

namespace Rise
{
    public sealed class CourseSpawner : MonoBehaviour
    {
        [SerializeField] private RiseGame game;
        [SerializeField] private RiseCamera followCamera;
        [SerializeField] private Sprite[] sawSprites;
        [SerializeField] private Sprite stationarySawSprite;
        [SerializeField] private Sprite bubbleSprite;
        [SerializeField] private Sprite shieldSprite;
        [SerializeField] private float firstHazardHeight = 12f;
        [SerializeField, Min(5f)] private float shieldHeightInterval = 50f;
        [Header("Height Difficulty")]
        [SerializeField] private float difficultyStartHeight = 20f;
        [SerializeField] private float originalDifficultyHeight = 150f;
        [SerializeField] private float fullDifficultyHeight = 500f;
        private const float SectionHeight = 8f;
        private const float SawRadius = 0.6f;
        private readonly List<GameObject> items = new List<GameObject>();
        private readonly List<Vector2> route = new List<Vector2>();
        private float startingHeight;
        private float nextShieldHeight;
        private int nextSection;
        private float ShieldInterval => Mathf.Max(5f, shieldHeightInterval);
        // Reach the former height-300 pressure at 100, then retain the cap at 500.
        private float DifficultyHeight => game.MaximumHeight <= 100f ? game.MaximumHeight * 3f
            : Mathf.Lerp(300f, 500f, Mathf.InverseLerp(100f, 500f, game.MaximumHeight));
        public float Difficulty => DifficultyHeight <= originalDifficultyHeight
            ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(difficultyStartHeight, originalDifficultyHeight, DifficultyHeight))
            : 1f + Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(originalDifficultyHeight, fullDifficultyHeight, DifficultyHeight));
        private static float Scale(float start, float middle, float end, float difficulty)
            => difficulty <= 1f ? Mathf.Lerp(start, middle, difficulty) : Mathf.Lerp(middle, end, difficulty - 1f);

        public void ResetCourse()
        {
            foreach (GameObject item in items)
                if (item != null) { item.SetActive(false); Destroy(item); }
            items.Clear();
            route.Clear();
            startingHeight = game.Kite.transform.position.y;
            nextShieldHeight = startingHeight + ShieldInterval;
            nextSection = 0;
            route.Add(new Vector2(0f, firstHazardHeight - SectionHeight));
        }

        private void ExtendRoute(float height)
        {
            while (route[route.Count - 1].y < height)
            {
                Vector2 previous = route[route.Count - 1];
                route.Add(new Vector2(Mathf.Clamp(previous.x + Random.Range(-0.6f, 0.6f), -1.1f, 1.1f), previous.y + SectionHeight));
            }
        }

        public float PassageX(float height)
        {
            for (int i = 1; i < route.Count; i++)
                if (height <= route[i].y)
                    return Mathf.Lerp(route[i - 1].x, route[i].x, Mathf.InverseLerp(route[i - 1].y, route[i].y, height));
            return route[route.Count - 1].x;
        }

        public void Advance()
        {
            if (!enabled) return;
            float ahead = followCamera.Top + SectionHeight;
            ExtendRoute(ahead + SectionHeight * 2f);
            while (firstHazardHeight + nextSection * SectionHeight < ahead)
            {
                float bottom = firstHazardHeight + nextSection * SectionHeight;
                PopulateSection(bottom, Difficulty);
                nextSection++;
            }
            while (nextShieldHeight < ahead)
            {
                CreateShield(new Vector2(PassageX(nextShieldHeight), nextShieldHeight));
                nextShieldHeight += ShieldInterval;
            }
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (items[i] == null) { items.RemoveAt(i); continue; }
                if (items[i].transform.position.y < followCamera.Bottom - 2f)
                {
                    items[i].SetActive(false);
                    Destroy(items[i]);
                    items.RemoveAt(i);
                }
            }
            while (route.Count > 2 && route[1].y < followCamera.Bottom - SectionHeight)
                route.RemoveAt(0);
        }

        private void PopulateSection(float bottom, float difficulty)
        {
            int target = Mathf.RoundToInt(Scale(3f, 4f, 5f, difficulty));
            float previousY = bottom;
            for (int index = 0; index < target; index++)
            {
                bool pair = index > 0 && Random.value < 0.12f;
                bool moving = Random.value < Scale(0.2f, 0.3f, 0.4f, difficulty);
                float bestScore = -1f;
                Vector2 bestPosition = Vector2.zero;
                float bestTravel = 0f;
                for (int attempt = 0; attempt < 160; attempt++)
                {
                    if (attempt == 80 && bestScore >= 0f) break;
                    float y = pair ? previousY : Random.Range(bottom, bottom + SectionHeight);
                    float travel = moving && attempt < 80 ? Random.Range(0.65f, Scale(0.9f, 1.15f, 1.35f, difficulty)) : 0f;
                    float x = Random.Range(-followCamera.HalfWidth + travel, followCamera.HalfWidth - travel);
                    if (!PlacementIsClear(x, y, travel)) continue;
                    float score = SectionHeight;
                    foreach (GameObject item in items)
                    {
                        if (item == null || !item.activeSelf) continue;
                        CourseItem other = item.GetComponent<CourseItem>();
                        if (!other.IsShield) score = Mathf.Min(score, Mathf.Abs(y - other.Origin.y));
                    }
                    if (score <= bestScore) continue;
                    bestScore = score;
                    bestPosition = new Vector2(x, y);
                    bestTravel = travel;
                    if (pair) break;
                }
                if (bestScore < 0f) continue;
                float averageSpeed = Random.Range(Scale(0.55f, 0.75f, 0.95f, difficulty), Scale(0.85f, 1.1f, 1.4f, difficulty));
                // A sine cycle travels 4A in 2pi/omega seconds: average speed = 2A*omega/pi.
                float angularSpeed = bestTravel > 0f ? averageSpeed * Mathf.PI / (2f * bestTravel) : 0f;
                CreateSaw(bestPosition, 1, bestTravel, angularSpeed);
                previousY = bestPosition.y;
            }
        }

        private bool PlacementIsClear(float x, float y, float travel)
        {
            float clearance = SawRadius + game.Kite.Radius + 0.2f;
            // Protect a connected, gently shifting passage against every phase of each sine wave.
            for (int i = 1; i < route.Count; i++)
            {
                if (route[i].y < y - clearance || route[i - 1].y > y + clearance) continue;
                float low = Mathf.Min(route[i - 1].x, route[i].x) - clearance;
                float high = Mathf.Max(route[i - 1].x, route[i].x) + clearance;
                if (x + travel >= low && x - travel <= high) return false;
            }
            float shieldY = startingHeight + Mathf.Round((y - startingHeight) / ShieldInterval) * ShieldInterval;
            if (shieldY >= startingHeight + ShieldInterval && Mathf.Abs(y - shieldY) < 1.25f)
            {
                float dx = Mathf.Max(0f, Mathf.Abs(x - PassageX(shieldY)) - travel);
                if (new Vector2(dx, y - shieldY).magnitude < 1.4f) return false;
            }
            int nearby = 0;
            foreach (GameObject item in items)
            {
                if (item == null || !item.activeSelf) continue;
                CourseItem saw = item.GetComponent<CourseItem>();
                if (saw.IsShield) continue;
                float dy = Mathf.Abs(y - saw.Origin.y);
                float dx = Mathf.Max(0f, Mathf.Abs(x - saw.Origin.x) - travel - saw.Travel);
                if (new Vector2(dx, dy).magnitude < 1.5f) return false;
                if (dy < 1.5f && ++nearby >= 2) return false;
            }
            return true;
        }

        public GameObject CreateSaw(Vector2 position, int spriteIndex = 1, float horizontalTravel = 0f,
            float horizontalSpeed = 0.65f, float? startingPhase = null)
        {
            GameObject root = new GameObject("Saw");
            root.transform.SetParent(transform);
            root.transform.position = position;
            SpriteRenderer visual = AddSprite("Saw art", root.transform, horizontalTravel > 0f ? sawSprites[spriteIndex] : stationarySawSprite, 3);
            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = spriteIndex == 0 ? 0.72f : 0.6f;
            root.AddComponent<CourseItem>().Initialize(game, false, visual.transform, horizontalTravel, horizontalSpeed, startingPhase);
            items.Add(root);
            return root;
        }

        public GameObject CreateShield(Vector2 position)
        {
            GameObject root = new GameObject("Shield bubble");
            root.transform.SetParent(transform);
            root.transform.position = position;
            AddSprite("Bubble", root.transform, bubbleSprite, 3);
            AddSprite("Shield", root.transform, shieldSprite, 4);
            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.46f;
            root.AddComponent<CourseItem>().Initialize(game, true, root.transform, 0f);
            items.Add(root);
            return root;
        }

        private static SpriteRenderer AddSprite(string name, Transform parent, Sprite sprite, int order)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
