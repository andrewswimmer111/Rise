using System;
using UnityEngine;

namespace Rise
{
    public sealed class WindSwipeInput : MonoBehaviour
    {
        [SerializeField] private Camera gameCamera;
        [SerializeField] private float minimumDistance = 0.025f;
        [SerializeField] private float speedWeight = 2.2f;
        [SerializeField] private float distanceWeight = 0.9f;
        [SerializeField] private float maximumImpulse = 4.2f;

        public event Action<Vector2> Swiped;
        private Vector2 start;
        private float startTime;
        private bool dragging;
        private int fingerId = -1;

        private void Update()
        {
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Began && !dragging)
                    {
                        Begin(touch.position);
                        if (dragging) fingerId = touch.fingerId;
                    }
                    else if (touch.fingerId == fingerId)
                    {
                        if (touch.phase == TouchPhase.Ended) End(touch.position);
                        else if (touch.phase == TouchPhase.Canceled) CancelGesture();
                    }
                }
                return;
            }

            // A lost touch must never become a second, simulated mouse swipe.
            if (fingerId >= 0) { CancelGesture(); return; }
            if (Input.GetMouseButtonDown(0)) Begin(Input.mousePosition);
            if (Input.GetMouseButtonUp(0)) End(Input.mousePosition);
        }

        private void Begin(Vector2 position)
        {
            if (gameCamera == null || !gameCamera.pixelRect.Contains(position)) return;
            dragging = true;
            start = position;
            startTime = Time.unscaledTime;
        }

        private void End(Vector2 position)
        {
            if (!dragging) return;
            Vector2 delta = (position - start) / Mathf.Max(1f, gameCamera.pixelHeight);
            float duration = Mathf.Max(0.045f, Time.unscaledTime - startTime);
            CancelGesture();
            if (delta.magnitude < minimumDistance) return;
            float strength = Mathf.Min(maximumImpulse,
                delta.magnitude / duration * speedWeight + delta.magnitude * distanceWeight);
            Swiped?.Invoke(delta.normalized * strength);
        }

        public void CancelGesture() { dragging = false; fingerId = -1; }
        private void OnDisable() => CancelGesture();
        private void OnApplicationFocus(bool focused) { if (!focused) CancelGesture(); }
    }
}
