using UnityEngine;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Su yansıması (greybox v1).
    ///  - Sahnedeki "daglar*" objelerinin GÖRSELLERİNDEN (kodlarına dokunmadan)
    ///    dikey aynalanmış kopyalar kurar; gökyüzü için de bir band ekler.
    ///  - Yansımalar SpriteMask sayesinde yalnızca su yüzeyinin içinde görünür.
    ///  - Yürürken: konteyner kamerayı dağlardan FARKLI oranda izler →
    ///    yansıma gerçek dağlara göre kayar (su yüzeyi hissi).
    ///  - Dururken: sabit süzülme (drift) — akıntı yansımayı da taşır.
    /// </summary>
    public class SuYansima : MonoBehaviour
    {
        [Header("Ayna")]
        [Tooltip("Su yüzeyi çizgisi (yansımaların üst kenarı buraya yapışır).")]
        [SerializeField] private float waterLineY = -3.45f;
        [SerializeField, Range(0f, 1f)] private float reflectionAlpha = 0.35f;
        [SerializeField] private Color reflectionTint = new Color(0.65f, 0.8f, 1f);
        [Tooltip("Gökyüzü yansıma bandı için sprite (gokyuzu).")]
        [SerializeField] private Sprite skySprite;

        [Header("Hareket")]
        [Tooltip("Yürüyüşte kamerayı izleme oranı — dağların paralaksından farklı olmalı.")]
        [SerializeField, Range(0f, 1f)] private float walkParallax = 0.55f;
        [Tooltip("Sabit süzülme hızı (birim/sn) — su aksa yansıma da kayar.")]
        [SerializeField] private float driftSpeed = 0.3f;
        [SerializeField] private int baseSortingOrder = 4; // su yüzeyinin üstünde, maskeli

        private Camera _cam;
        private float _camStartX;
        private Vector3 _startPos;
        private float _wrapSpan = 72f; // drift sarmalama aralığı (dağ çifti genişliği)

        private void Start()
        {
            _cam = Camera.main;
            _camStartX = _cam != null ? _cam.transform.position.x : 0f;
            _startPos = transform.position;
            BuildMirrors();
        }

        private void BuildMirrors()
        {
            // --- Gökyüzü yansıma bandı (en altta) ---
            if (skySprite != null)
            {
                var sky = NewMirror("SkyYansima", skySprite, baseSortingOrder, reflectionAlpha * 0.8f);
                float sw = skySprite.bounds.size.x;
                float sh = skySprite.bounds.size.y;
                sky.transform.localScale = new Vector3(160f / sw, -(3.5f / sh), 1f);
                sky.transform.position = new Vector3(transform.position.x, waterLineY - 1.75f, 0f);
            }

            // --- Dağ aynaları: daglar* görsellerinden kopya ---
            // (Kök şart değil — hiyerarşinin herhangi bir yerindeki dağları bul.)
            foreach (var src in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                var r = src.gameObject;
                if (!r.name.StartsWith("daglar")) continue;
                if (src.sprite == null) continue;

                float w = src.bounds.size.x;
                float h = src.bounds.size.y;
                if (_wrapSpan < w * 2f) _wrapSpan = w * 2f;

                // Geniş kapsama: her katmanın 2 tekrarı (çift genişliği kadar arayla).
                for (int rep = 0; rep < 2; rep++)
                {
                    var m = NewMirror(r.name + "_yansima" + rep, src.sprite,
                        baseSortingOrder + 1, reflectionAlpha);
                    var msr = m.GetComponent<SpriteRenderer>();
                    msr.drawMode = src.drawMode;
                    if (src.drawMode != SpriteDrawMode.Simple) msr.size = src.size;

                    // Dikey ayna + üst kenarı su çizgisine yapıştır.
                    m.transform.localScale = new Vector3(
                        r.transform.localScale.x,
                        -Mathf.Abs(r.transform.localScale.y), 1f);
                    m.transform.position = new Vector3(
                        r.transform.position.x + rep * w * 2f,
                        waterLineY - h * 0.5f, 0f);
                }
            }
        }

        private GameObject NewMirror(string objName, Sprite s, int order, float alpha)
        {
            var go = new GameObject(objName);
            go.transform.SetParent(transform, true);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            var c = reflectionTint;
            c.a = alpha;
            sr.color = c;
            sr.sortingOrder = order;
            // Yalnızca su yüzeyinin (SpriteMask) içinde görün.
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            return go;
        }

        private void LateUpdate()
        {
            if (_cam == null) return;
            float camDx = _cam.transform.position.x - _camStartX;
            float drift = (driftSpeed * Time.time) % _wrapSpan; // sonsuz süzülme, taşma yok
            transform.position = new Vector3(
                _startPos.x + camDx * walkParallax - drift,
                _startPos.y, _startPos.z);
        }
    }
}
