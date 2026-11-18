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
        private readonly Dictionary<int, float> shieldPositions = new Dictionary<int, float>();
        private float startingHeight;
        private float nextShieldHeight;
        private int nextSection;
        private float ShieldInterval => Mathf.Max(5f, shieldHeightInterval);
        // The previous curve mapped actual height 400 to curve height 450.
        private float DifficultyHeight => game.MaximumHeight <= 100f ? game.MaximumHeight * 4.5f
            : Mathf.Lerp(450f, 500f, Mathf.InverseLerp(100f, 500f, game.MaximumHeight));
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
            shieldPositions.Clear();
            startingHeight = game.Kite.transform.position.y;
            nextShieldHeight = startingHeight + ShieldInterval;
            nextSection = 0;
        }

        private float ShieldX(int milestone)
        {
            if (!shieldPositions.TryGetValue(milestone, out float x))
            {
                x = Random.Range(-followCamera.HalfWidth + 0.8f, followCamera.HalfWidth - 0.8f);
                shieldPositions.Add(milestone, x);
            }
            return x;
        }

        public void Advance()
        {
            if (!enabled) return;
            float ahead = followCamera.Top + SectionHeight;
            // Reserve future shield pockets before placing any nearby hazards.
            for (int milestone = Mathf.Max(1, Mathf.FloorToInt((followCamera.Bottom - startingHeight) / ShieldInterval));
                startingHeight + milestone * ShieldInterval <= ahead + SectionHeight + 1.4f; milestone++)
                ShieldX(milestone);
            while (firstHazardHeight + nextSection * SectionHeight < ahead)
            {
                float bottom = firstHazardHeight + nextSection * SectionHeight;
                PopulateSection(bottom, Difficulty);
                nextSection++;
            }
            while (nextShieldHeight < ahead)
            {
                CreateShield(new Vector2(ShieldX(Mathf.RoundToInt((nextShieldHeight - startingHeight) / ShieldInterval)), nextShieldHeight));
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
            int expired = Mathf.FloorToInt((followCamera.Bottom - startingHeight - 2f) / ShieldInterval);
            shieldPositions.Remove(expired);
        }

        private void PopulateSection(float bottom, float difficulty)
        {
            int target = Mathf.RoundToInt(Scale(3f, 4f, 5f, difficulty)
                + 3f * Mathf.InverseLerp(100f, fullDifficultyHeight, game.MaximumHeight));
            float previousY = bottom;
            for (int index = 0; index < target; index++)
            {
                bool pair = index > 0 && Random.value < 0.12f;
                // Already visible stationary saws never begin moving retroactively.
                bool moving = game.MaximumHeight >= 50f && Random.value < Scale(0.2f, 0.3f, 0.4f, difficulty);
                float bestScore = -1f;
                Vector2 bestPosition = Vector2.zero;
                float bestTravel = 0f;
                for (int attempt = 0; attempt < 160; attempt++)
                {
                    if (attempt == 80 && bestScore >= 0f) break;
                    float y = pair ? previousY : Random.Range(bottom, bottom + SectionHeight);
                    float travel = moving && attempt < 80 ? Random.Range(1f, Scale(1.3f, 1.4f, 1.55f, difficulty)) : 0f;
                    float x = Random.Range(-followCamera.HalfWidth + travel, followCamera.HalfWidth - travel);
                    if (!PlacementIsClear(x, y, travel)) continue;
                    float score = SectionHeight;
                    foreach (GameObject item in items)
                    {
                        if (item == null || !item.activeSelf) continue;
                        CourseItem other = item.GetComponent<CourseItem>();
                        if (!other.IsShield) score = Mathf.Min(score, Vector2.Distance(new Vector2(x, y), other.Origin));
                    }
                    // Cap the spacing reward and retain randomness among adequately spread candidates.
                    score = Mathf.Min(score, 2.4f) + Random.Range(0f, 0.8f);
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
            int milestone = Mathf.RoundToInt((y - startingHeight) / ShieldInterval);
            float shieldY = startingHeight + milestone * ShieldInterval;
            if (milestone >= 1 && Mathf.Abs(y - shieldY) < 1.4f)
            {
                float dx = Mathf.Max(0f, Mathf.Abs(x - ShieldX(milestone)) - travel);
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
            }
            return !WouldFormWall(new Vector2(x, y), travel);
        }

        public bool CanPlaceSaw(Vector2 position, float travel = 0f)
            => PlacementIsClear(position.x, position.y, travel);

        private bool WouldFormWall(Vector2 position, float travel)
        {
            // Inflate the entire sine sweep by the kite radius and a comfort margin.
            // A connected chain touching both side boundaries blocks ascent, including
            // diagonal chains that a single horizontal-row test would miss.
            float radius = SawRadius + game.Kite.Radius + 0.16f;
            float edge = followCamera.HalfWidth - game.Kite.Radius;
            var centers = new List<Vector2> { position };
            var travels = new List<float> { travel };
            foreach (GameObject item in items)
            {
                if (item == null || !item.activeSelf) continue;
                CourseItem saw = item.GetComponent<CourseItem>();
                if (saw.IsShield) continue;
                centers.Add(saw.Origin);
                travels.Add(saw.Travel);
            }
            var visited = new bool[centers.Count];
            var pending = new Queue<int>();
            pending.Enqueue(0);
            visited[0] = true;
            bool left = false, right = false;
            while (pending.Count > 0)
            {
                int a = pending.Dequeue();
                left |= centers[a].x - travels[a] - radius <= -edge;
                right |= centers[a].x + travels[a] + radius >= edge;
                if (left && right) return true;
                for (int b = 1; b < centers.Count; b++)
                {
                    if (visited[b]) continue;
                    float dx = Mathf.Max(0f, Mathf.Abs(centers[a].x - centers[b].x) - travels[a] - travels[b]);
                    float dy = centers[a].y - centers[b].y;
                    if (dx * dx + dy * dy > 4f * radius * radius) continue;
                    visited[b] = true;
                    pending.Enqueue(b);
                }
            }
            return false;
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
