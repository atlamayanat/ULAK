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

        [Header("Saldırı (opsiyonel)")]
        [Tooltip("Saldırıda BİR KEZ oynatılacak kareler (PlayAttack ile tetiklenir).")]
        [SerializeField] private Sprite[] attackFrames;
        [Tooltip("Saldırı kareleri arası süre (sn).")]
        [SerializeField] private float attackFrameInterval = 0.1f;

        private SpriteRenderer _sr;
        private float _timer;
        private int _index;
        private bool _moving;
        private int _attackIndex = -1; // -1 = saldırı oynamıyor
        private float _attackTimer;

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
            // Saldırı animasyonu önceliklidir: bir kez oynar, sonra normale döner.
            if (_attackIndex >= 0)
            {
                _attackTimer += Time.deltaTime;
                if (_attackTimer >= attackFrameInterval)
                {
                    _attackTimer -= attackFrameInterval;
                    _attackIndex++;
                    if (_attackIndex >= attackFrames.Length)
                    {
                        _attackIndex = -1; // bitti → normal akışa dön
                        ShowFrame(0);
                    }
                    else
                    {
                        _sr.sprite = attackFrames[_attackIndex];
                    }
                }
                return;
            }

            var set = ActiveFrames;
            if (set == null || set.Length < 2) return;

            _timer += Time.deltaTime;
            if (_timer < ActiveInterval) return;

            _timer -= ActiveInterval;
            ShowFrame(_index + 1);
        }

        /// <summary>Saldırı karelerini baştan bir kez oynatır (varsa).</summary>
        public void PlayAttack()
        {
            if (attackFrames == null || attackFrames.Length == 0) return;
            _attackIndex = 0;
            _attackTimer = 0f;
            _sr.sprite = attackFrames[0];
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
