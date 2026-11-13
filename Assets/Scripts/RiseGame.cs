using UnityEngine;

namespace Rise
{
    public enum RunState { Waiting, Playing, GameOver }

    public sealed class RiseGame : MonoBehaviour
    {
        [SerializeField] private WindSwipeInput swipeInput;
        [SerializeField] private KiteController kite;
        [SerializeField] private RiseCamera followCamera;
        [SerializeField] private CourseSpawner course;
        [SerializeField] private RiseHud hud;
        [SerializeField] private LineRenderer kiteString;
        [SerializeField] private Transform stringAnchor;
        [SerializeField] private WindGustEffect windGusts;
        [SerializeField] private BackgroundClouds backgroundClouds;

        public RunState State { get; private set; }
        public float MaximumHeight { get; private set; }
        public string EndReason { get; private set; }
        public KiteController Kite => kite;
        public RiseCamera FollowCamera => followCamera;
        private float launchHeight;

        private void Start()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Application.targetFrameRate = 60;
            Input.simulateMouseWithTouches = false;
            swipeInput.Swiped += ApplySwipe;
            ResetRun();
        }

        public void ApplySwipe(Vector2 impulse)
        {
            if (State == RunState.GameOver || impulse.sqrMagnitude < 0.001f) return;
            State = RunState.Playing;
            kite.ApplyWind(impulse);
            if (windGusts != null) windGusts.Emit(impulse);
        }

        private void LateUpdate()
        {
            if (State == RunState.Playing)
            {
                MaximumHeight = Mathf.Max(MaximumHeight, kite.transform.position.y - launchHeight);
                followCamera.Advance(kite.transform.position.y, Time.deltaTime);
                course.Advance();
                if (kite.transform.position.y + kite.Radius < followCamera.Bottom)
                    EndRun("The kite fell behind.");
            }

            if (State != RunState.GameOver && backgroundClouds != null)
                backgroundClouds.Advance(Time.deltaTime);

            // This is a visual string only. It does not constrain endless flight.
            if (kiteString != null)
            {
                Vector3 end = kite.transform.position;
                Vector3 start = stringAnchor.position;
                float visibleBottom = followCamera.Bottom - 0.5f;
                if (start.y < visibleBottom && end.y > start.y)
                    start = Vector3.Lerp(start, end, Mathf.Clamp01((visibleBottom - start.y) / (end.y - start.y)));
                kiteString.SetPosition(0, start);
                kiteString.SetPosition(1, end);
            }
            hud.Refresh(this);
        }

        public void EndRun(string reason)
        {
            if (State != RunState.Playing) return;
            State = RunState.GameOver;
            EndReason = reason;
            kite.Freeze();
            if (windGusts != null) windGusts.Clear();
            swipeInput.CancelGesture();
        }

        public void ResetRun()
        {
            State = RunState.Waiting;
            EndReason = string.Empty;
            MaximumHeight = 0f;
            if (windGusts != null) windGusts.Clear();
            swipeInput.CancelGesture();
            kite.ResetKite();
            followCamera.ResetView();
            if (backgroundClouds != null) backgroundClouds.ResetSky();
            launchHeight = kite.transform.position.y;
            course.ResetCourse();
            hud.Refresh(this);
        }

        private void OnApplicationPause(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;
            if (paused && swipeInput != null) swipeInput.CancelGesture();
        }

        private void OnDestroy()
        {
            if (swipeInput != null) swipeInput.Swiped -= ApplySwipe;
            Time.timeScale = 1f;
        }
    }
}
