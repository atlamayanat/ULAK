using UnityEngine;

namespace Ulak.Core
{
    /// <summary>
    /// Geri savurma. Bir darbe (impulse) uygular ve kısa bir süre kontrolü kilitler.
    /// Hareket script'leri (PlayerController / SmallEnemyAI) <see cref="IsBeingKnockedBack"/>
    /// true iken kendi hareketlerini uygulamaz — böylece savrulma hissi bozulmaz.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Knockback : MonoBehaviour
    {
        [Tooltip("Savrulduktan sonra kontrolün kilitli kalacağı süre (sn).")]
        [SerializeField] private float controlLockTime = 0.3f;

        private Rigidbody2D _rb;
        private float _lockedUntil;

        /// <summary>Şu an savrulma kilidinde mi? Hareket script'leri bunu kontrol etmeli.</summary>
        public bool IsBeingKnockedBack => Time.time < _lockedUntil;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        /// <summary>Bir darbe uygular ve kontrolü kısa süre kilitler.</summary>
        public void Apply(Vector2 impulse)
        {
            if (impulse == Vector2.zero) return;

            // Mevcut hızı sıfırlayıp temiz bir savrulma ver (daha okunabilir his).
            _rb.linearVelocity = Vector2.zero;
            _rb.AddForce(impulse, ForceMode2D.Impulse);
            _lockedUntil = Time.time + controlLockTime;
        }
    }
}
