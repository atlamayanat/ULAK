using UnityEngine;

namespace Ulak.Core
{
    /// <summary>
    /// Basit kare-değiştirme animasyonu (greybox v1).
    /// Animator gerekmez: verilen sprite karelerini sabit aralıkla döndürür.
    /// Aynı objede <see cref="Ulak.Gameplay.PlayerController"/> benzeri bir
    /// FacingX kaynağı varsa sprite'ı yöne göre çevirmek için
    /// <see cref="SetFacing"/> çağrılabilir.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFlipbook : MonoBehaviour
    {
        [Tooltip("Sırayla gösterilecek kareler.")]
        [SerializeField] private Sprite[] frames;
        [Tooltip("Kareler arası süre (sn).")]
        [SerializeField] private float frameInterval = 0.45f;

        private SpriteRenderer _sr;
        private float _timer;
        private int _index;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (frames != null && frames.Length > 0 && frames[0] != null)
                _sr.sprite = frames[0];
        }

        private void Update()
        {
            if (frames == null || frames.Length < 2) return;

            _timer += Time.deltaTime;
            if (_timer < frameInterval) return;

            _timer -= frameInterval;
            _index = (_index + 1) % frames.Length;
            if (frames[_index] != null)
                _sr.sprite = frames[_index];
        }

        /// <summary>Sprite'ı yatay yöne göre çevirir (1 = sağ, -1 = sol).</summary>
        public void SetFacing(int facingX)
        {
            _sr.flipX = facingX < 0;
        }
    }
}
