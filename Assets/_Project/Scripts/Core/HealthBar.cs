using UnityEngine;

namespace Ulak.Core
{
    /// <summary>
    /// Karakterin üstünde süzülen world-space can barı (greybox v1).
    /// Asset gerekmez: beyaz sprite'ı runtime'da üretir, çocuk objeleri kendisi kurar.
    /// <see cref="Health"/> değerini her kare okur — event bağlantısı gerekmez.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class HealthBar : MonoBehaviour
    {
        [Header("Yerleşim")]
        [Tooltip("Barın karaktere göre ofseti.")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 0.85f);
        [SerializeField] private Vector2 size = new Vector2(1f, 0.14f);

        [Header("Renkler")]
        [SerializeField] private Color fillColor = new Color(0.25f, 0.9f, 0.35f);
        [Tooltip("Can azaldıkça dolgu bu renge kayar.")]
        [SerializeField] private Color lowColor = new Color(0.95f, 0.25f, 0.2f);
        [SerializeField] private Color backColor = new Color(0f, 0f, 0f, 0.75f);

        private Health _health;
        private Transform _fill;
        private SpriteRenderer _fillSr;

        // Runtime'da üretilen paylaşımlı 1x1 beyaz sprite'lar (merkez ve sol pivotlu).
        private static Sprite _whiteCenter;
        private static Sprite _whiteLeft;

        private void Awake()
        {
            _health = GetComponent<Health>();
            BuildBar();
        }

        private void LateUpdate()
        {
            float t = _health.Max > 0 ? Mathf.Clamp01((float)_health.Current / _health.Max) : 0f;
            _fill.localScale = new Vector3(size.x * t, size.y, 1f);
            _fillSr.color = Color.Lerp(lowColor, fillColor, t);
        }

        private void BuildBar()
        {
            var root = new GameObject("HealthBar");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = offset;

            // Arka plan (merkez pivot)
            var back = new GameObject("Back");
            back.transform.SetParent(root.transform, false);
            back.transform.localScale = new Vector3(size.x, size.y, 1f);
            var backSr = back.AddComponent<SpriteRenderer>();
            backSr.sprite = GetWhiteSprite(centerPivot: true);
            backSr.color = backColor;
            backSr.sortingOrder = 20;

            // Dolgu (sol pivot — soldan sağa dolar/boşalır)
            var fill = new GameObject("Fill");
            fill.transform.SetParent(root.transform, false);
            fill.transform.localPosition = new Vector3(-size.x * 0.5f, 0f, 0f);
            fill.transform.localScale = new Vector3(size.x, size.y, 1f);
            _fillSr = fill.AddComponent<SpriteRenderer>();
            _fillSr.sprite = GetWhiteSprite(centerPivot: false);
            _fillSr.color = fillColor;
            _fillSr.sortingOrder = 21;
            _fill = fill.transform;
        }

        private static Sprite GetWhiteSprite(bool centerPivot)
        {
            ref Sprite cache = ref centerPivot ? ref _whiteCenter : ref _whiteLeft;
            if (cache != null) return cache;

            var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point };
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();

            Vector2 pivot = centerPivot ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 0.5f);
            // 4 piksel / 4 ppu = 1 dünya birimi → localScale doğrudan dünya boyutu olur.
            cache = Sprite.Create(tex, new Rect(0, 0, 4, 4), pivot, 4f);
            return cache;
        }
    }
}
