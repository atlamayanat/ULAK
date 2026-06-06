using UnityEngine;

namespace Ulak.Core
{
    /// <summary>
    /// Basit kare-değiştirme animasyonu (greybox v1).
    /// Animator gerekmez: verilen sprite karelerini sabit aralıkla döndürür.
    /// İki set destekler: idle ve yürüme. <see cref="SetMoving"/> ile geçiş yapılır.
    /// Yön çevirme için <see cref="SetFacing"/> çağrılır (1 = sağ, -1 = sol).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFlipbook : MonoBehaviour
    {
        [Header("Idle")]
        [Tooltip("Beklerken dönecek kareler.")]
        [SerializeField] private Sprite[] frames;
        [Tooltip("Idle kareler arası süre (sn).")]
        [SerializeField] private float frameInterval = 0.45f;

        [Header("Yürüme (opsiyonel)")]
        [Tooltip("Yürürken dönecek kareler. Boşsa idle kareler kullanılır.")]
        [SerializeField] private Sprite[] walkFrames;
        [Tooltip("Yürüme kareleri arası süre (sn) — adım temposu.")]
        [SerializeField] private float walkFrameInterval = 0.18f;

        private SpriteRenderer _sr;
        private float _timer;
        private int _index;
        private bool _moving;

        private Sprite[] ActiveFrames =>
            _moving && walkFrames != null && walkFrames.Length > 0 ? walkFrames : frames;

        private float ActiveInterval =>
            _moving && walkFrames != null && walkFrames.Length > 0 ? walkFrameInterval : frameInterval;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            ShowFrame(0);
        }

        private void Update()
        {
            var set = ActiveFrames;
            if (set == null || set.Length < 2) return;

            _timer += Time.deltaTime;
            if (_timer < ActiveInterval) return;

            _timer -= ActiveInterval;
            ShowFrame(_index + 1);
        }

        /// <summary>Yürüme/idle seti arasında geçiş yapar.</summary>
        public void SetMoving(bool moving)
        {
            if (_moving == moving) return;
            _moving = moving;
            _timer = 0f;
            ShowFrame(0); // yeni setin ilk karesinden başla
        }

        /// <summary>Sprite'ı yatay yöne göre çevirir (1 = sağ, -1 = sol).</summary>
        public void SetFacing(int facingX)
        {
            _sr.flipX = facingX < 0;
        }

        private void ShowFrame(int index)
        {
            var set = ActiveFrames;
            if (set == null || set.Length == 0) return;

            _index = index % set.Length;
            if (set[_index] != null)
                _sr.sprite = set[_index];
        }
    }
}
