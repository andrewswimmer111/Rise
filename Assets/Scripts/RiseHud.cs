using UnityEngine;
using UnityEngine.UI;

namespace Rise
{
    public sealed class RiseHud : MonoBehaviour
    {
        [SerializeField] private Camera gameCamera;
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private PixelScore height;
        [SerializeField] private GameObject endPanel;
        [SerializeField] private PixelScore finalHeight;
        [SerializeField] private Button retry;
        private RiseGame game;

        private void Awake() => retry.onClick.AddListener(Retry);
        private void Retry() { if (game != null) game.ResetRun(); }

        public void Refresh(RiseGame currentGame)
        {
            game = currentGame;
            canvas.scaleFactor = Mathf.Max(0.1f, gameCamera.pixelHeight / 640f);
            Rect view = gameCamera.pixelRect;
            Rect safe = Screen.safeArea;
            viewport.anchorMin = new Vector2(Mathf.Max(view.xMin, safe.xMin) / Screen.width,
                Mathf.Max(view.yMin, safe.yMin) / Screen.height);
            viewport.anchorMax = new Vector2(Mathf.Min(view.xMax, safe.xMax) / Screen.width,
                Mathf.Min(view.yMax, safe.yMax) / Screen.height);
            viewport.offsetMin = viewport.offsetMax = Vector2.zero;

            int score = Mathf.FloorToInt(game.MaximumHeight);
            height.SetNumber(score);
            height.gameObject.SetActive(game.State != RunState.GameOver);
            endPanel.SetActive(game.State == RunState.GameOver);
            finalHeight.SetNumber(score);
        }
    }
}
