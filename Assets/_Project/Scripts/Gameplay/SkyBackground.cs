using UnityEngine;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Gün doğumu gökyüzü arka planı (greybox v1).
    ///  - Sprite kamerayı takip eder, her zaman ekranı kaplar (en geride çizilir).
    ///  - Başlangıçta görüntünün ÜST sınırı ekranın üst sınırına yapışıktır
    ///    (gece kısmı görünür). Zaman geçtikçe görünür pencere görüntü boyunca
    ///    yavaşça aşağı kayar; süre sonunda ALT sınır ekranın altına oturur
    ///    (gün doğumu → gündüz).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SkyBackground : MonoBehaviour
    {
        [Tooltip("Takip edilecek kamera. Boşsa Camera.main kullanılır.")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("Gece → gündüz geçişinin toplam süresi (sn).")]
        [SerializeField] private float sunriseDuration = 90f;

        [Tooltip("Geçiş eğrisi (0-1). Düz bırakılırsa sabit hız.")]
        [SerializeField] private AnimationCurve progressCurve = AnimationCurve.Linear(0, 0, 1, 1);

        private SpriteRenderer _sr;
        private float _elapsed;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (targetCamera == null) targetCamera = Camera.main;

            // En geride çizil (greybox nesneleri 0..21 aralığında).
            _sr.sortingOrder = -100;
        }

        private void LateUpdate()
        {
            if (targetCamera == null || _sr.sprite == null) return;

            _elapsed += Time.deltaTime;

            float camHalfH = targetCamera.orthographicSize;
            float camHalfW = camHalfH * targetCamera.aspect;
            Vector3 camPos = targetCamera.transform.position;

            // --- Ölçek: ekran genişliğini her zaman kapla ---
            Vector2 spriteSize = _sr.sprite.bounds.size; // ölçeksiz boyut
            float scale = (camHalfW * 2f) / spriteSize.x;
            // Görüntü, ekran yüksekliğinden kısa kalmasın (kaydırma payı da kalsın).
            float minScaleForHeight = (camHalfH * 2f) / spriteSize.y;
            if (scale < minScaleForHeight) scale = minScaleForHeight;
            transform.localScale = new Vector3(scale, scale, 1f);

            float worldH = spriteSize.y * scale;

            // --- Dikey kayma: üstte başla, süre boyunca pencere aşağı insin ---
            float t = sunriseDuration > 0f ? Mathf.Clamp01(_elapsed / sunriseDuration) : 1f;
            t = progressCurve.Evaluate(t);

            // t=0 → görüntünün üstü ekranın üstüne yapışık (merkez = camTop - H/2)
            // t=1 → görüntünün altı ekranın altına yapışık (merkez = camBottom + H/2)
            float topAlignedY = camPos.y + camHalfH - worldH * 0.5f;
            float bottomAlignedY = camPos.y - camHalfH + worldH * 0.5f;
            float y = Mathf.Lerp(topAlignedY, bottomAlignedY, t);

            transform.position = new Vector3(camPos.x, y, camPos.z + 10f);
        }
    }
}
