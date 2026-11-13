using UnityEngine;
using UnityEngine.UI;

namespace Rise
{
    // A small 5-by-7 numeric pixel font. Each lit cell is a sharp UI quad.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PixelScore : MaskableGraphic
    {
        [SerializeField] private float pixelSize = 3f;
        [SerializeField] private bool centered;
        private int number = -1;
        private string digits = "0";

        private static readonly string[] Glyphs =
        {
            "01110" + "10001" + "10011" + "10101" + "11001" + "10001" + "01110",
            "00100" + "01100" + "00100" + "00100" + "00100" + "00100" + "01110",
            "01110" + "10001" + "00001" + "00010" + "00100" + "01000" + "11111",
            "11110" + "00001" + "00001" + "01110" + "00001" + "00001" + "11110",
            "00010" + "00110" + "01010" + "10010" + "11111" + "00010" + "00010",
            "11111" + "10000" + "10000" + "11110" + "00001" + "00001" + "11110",
            "01110" + "10000" + "10000" + "11110" + "10001" + "10001" + "01110",
            "11111" + "00001" + "00010" + "00100" + "01000" + "01000" + "01000",
            "01110" + "10001" + "10001" + "01110" + "10001" + "10001" + "01110",
            "01110" + "10001" + "10001" + "01111" + "00001" + "00001" + "01110"
        };

        public void SetNumber(int value)
        {
            value = Mathf.Max(0, value);
            if (number == value) return;
            number = value;
            digits = value.ToString();
            SetVerticesDirty();
        }

        public void Configure(float size, bool alignCenter)
        {
            pixelSize = size;
            centered = alignCenter;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Rect rect = GetPixelAdjustedRect();
            int columns = digits.Length * 6 - 1;
            float cell = Mathf.Min(pixelSize, rect.width / columns, rect.height / 7f);
            float startX = centered ? rect.center.x - columns * cell * 0.5f : rect.xMin;
            float top = rect.center.y + 7f * cell * 0.5f;
            for (int digit = 0; digit < digits.Length; digit++)
            {
                string glyph = Glyphs[digits[digit] - '0'];
                for (int y = 0; y < 7; y++)
                    for (int x = 0; x < 5; x++)
                    {
                        if (glyph[y * 5 + x] != '1') continue;
                        float left = startX + (digit * 6 + x) * cell;
                        float bottom = top - (y + 1) * cell;
                        int index = helper.currentVertCount;
                        helper.AddVert(new Vector3(left, bottom), color, Vector2.zero);
                        helper.AddVert(new Vector3(left, bottom + cell), color, Vector2.zero);
                        helper.AddVert(new Vector3(left + cell, bottom + cell), color, Vector2.zero);
                        helper.AddVert(new Vector3(left + cell, bottom), color, Vector2.zero);
                        helper.AddTriangle(index, index + 1, index + 2);
                        helper.AddTriangle(index + 2, index + 3, index);
                    }
            }
        }
    }
}
