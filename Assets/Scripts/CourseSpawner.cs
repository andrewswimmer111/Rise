using System.Collections.Generic;
using UnityEngine;

namespace Rise
{
    public sealed class CourseSpawner : MonoBehaviour
    {
        [SerializeField] private RiseGame game;
        [SerializeField] private RiseCamera followCamera;
        [SerializeField] private Sprite[] sawSprites;
        [SerializeField] private Sprite bubbleSprite;
        [SerializeField] private Sprite shieldSprite;
        [SerializeField] private float firstHazardHeight = 12f;
        [SerializeField] private float rowSpacing = 4f;
        private readonly List<GameObject> items = new List<GameObject>();
        private float nextRow;
        private int row;

        public void ResetCourse()
        {
            foreach (GameObject item in items)
                if (item != null) { item.SetActive(false); Destroy(item); }
            items.Clear();
            row = 0;
            nextRow = firstHazardHeight;
        }

        public void Advance()
        {
            if (!enabled) return;
            // Generate ahead of view so obstacles never pop into the visible sky.
            while (nextRow < followCamera.Top + rowSpacing)
            {
                float x = Random.Range(-1.65f, 1.65f);
                CreateSaw(new Vector2(x, nextRow), 1, row % 3 == 2 ? 0.4f : 0f);
                if (row % 3 == 0)
                    CreateShield(new Vector2(-Mathf.Sign(x) * 1.15f, nextRow - 1.8f));
                row++;
                nextRow += rowSpacing;
            }
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (items[i] == null) { items.RemoveAt(i); continue; }
                if (items[i].transform.position.y < followCamera.Bottom - 2f)
                {
                    Destroy(items[i]);
                    items.RemoveAt(i);
                }
            }
        }

        public GameObject CreateSaw(Vector2 position, int spriteIndex = 1, float horizontalTravel = 0f)
        {
            GameObject root = new GameObject("Saw");
            root.transform.SetParent(transform);
            root.transform.position = position;
            SpriteRenderer visual = AddSprite("Saw art", root.transform, sawSprites[spriteIndex], 3);
            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = spriteIndex == 0 ? 0.72f : 0.6f;
            root.AddComponent<CourseItem>().Initialize(game, false, visual.transform, horizontalTravel);
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
