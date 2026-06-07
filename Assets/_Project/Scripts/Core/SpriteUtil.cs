using UnityEngine;

namespace Ulak.Core
{
    /// <summary>
    /// Runtime'da üretilen yardımcı sprite'lar (greybox v1):
    ///  - YuvarlakKutu: 9-slice'lı yuvarlak köşeli kutu — SpriteRenderer
    ///    Sliced modunda her boyuta köşeleri BOZULMADAN esner (replik kutusu).
    ///  - YuvarlakBar: yarım daire uçlu hap şekli (can barları).
    /// </summary>
    public static class SpriteUtil
    {
        private static Sprite _kutu;
        private static Sprite _barOrta;
        private static Sprite _barSol;

        /// <summary>48x48, 10px köşe yarıçaplı, 9-slice kenarlıklı beyaz kutu.</summary>
        public static Sprite YuvarlakKutu()
        {
            if (_kutu != null) return _kutu;
            var tex = YuvarlakDoku(48, 48, 10);
            _kutu = Sprite.Create(tex, new Rect(0, 0, 48, 48),
                new Vector2(0.5f, 0.5f), 48f, 0,
                SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12)); // 9-slice kenarlık
            return _kutu;
        }

        /// <summary>32x8 hap (yarım daire uçlu), merkez pivotlu.</summary>
        public static Sprite YuvarlakBar()
        {
            if (_barOrta != null) return _barOrta;
            _barOrta = Sprite.Create(YuvarlakDoku(32, 8, 4),
                new Rect(0, 0, 32, 8), new Vector2(0.5f, 0.5f), 32f);
            return _barOrta;
        }

        /// <summary>32x8 hap, SOL pivotlu (soldan dolan bar için).</summary>
        public static Sprite YuvarlakBarSol()
        {
            if (_barSol != null) return _barSol;
            _barSol = Sprite.Create(YuvarlakDoku(32, 8, 4),
                new Rect(0, 0, 32, 8), new Vector2(0f, 0.5f), 32f);
            return _barSol;
        }

        /// <summary>Yuvarlak köşeli beyaz doku üretir (köşeler dışı saydam).</summary>
        private static Texture2D YuvarlakDoku(int w, int h, int r)
        {
            var tex = new Texture2D(w, h) { filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool icinde = true;
                // Dört köşe: yarıçap dairesinin dışında kalan piksel saydam.
                if (x < r && y < r) icinde = Mesafe(x, y, r, r) <= r;
                else if (x >= w - r && y < r) icinde = Mesafe(x, y, w - 1 - r, r) <= r;
                else if (x < r && y >= h - r) icinde = Mesafe(x, y, r, h - 1 - r) <= r;
                else if (x >= w - r && y >= h - r) icinde = Mesafe(x, y, w - 1 - r, h - 1 - r) <= r;

                px[y * w + x] = icinde
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        private static float Mesafe(int x, int y, int cx, int cy)
        {
            float dx = x - cx, dy = y - cy;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }
    }
}
