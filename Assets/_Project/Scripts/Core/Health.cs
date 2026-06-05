using UnityEngine;
using UnityEngine.Events;

namespace Ulak.Core
{
    /// <summary>
    /// Ortak can/hasar bileşeni. Oyuncu, düşman ve kırılabilir engel paylaşır.
    /// Hasar alınca opsiyonel olarak <see cref="Knockback"/> ve <see cref="DamageFlash"/> tetikler.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHealth = 3;

        /// <summary>Inspector'da görünür/serialize olur diye somut UnityEvent tipi.</summary>
        [System.Serializable] public class IntEvent : UnityEvent<int> { }

        [Header("Olaylar")]
        [Tooltip("Hasar alındığında (kalan can ile) tetiklenir.")]
        public IntEvent OnDamaged = new IntEvent();
        [Tooltip("Can sıfırlanınca tetiklenir.")]
        public UnityEvent OnDeath = new UnityEvent();

        private int _current;
        private Knockback _knockback;     // opsiyonel
        private DamageFlash _flash;       // opsiyonel
        private bool _dead;

        public int Current => _current;
        public int Max => maxHealth;
        public bool IsAlive => !_dead;

        private void Awake()
        {
            _current = maxHealth;
            _knockback = GetComponent<Knockback>();
            _flash = GetComponent<DamageFlash>();
        }

        public void TakeDamage(int amount, Vector2 knockback)
        {
            if (_dead || amount <= 0) return;

            _current = Mathf.Max(0, _current - amount);
            if (_flash != null) _flash.Flash();
            if (_knockback != null) _knockback.Apply(knockback);

            OnDamaged?.Invoke(_current);

            if (_current == 0)
                Die();
        }

        /// <summary>Canı tamamen doldurur (respawn/checkpoint için).</summary>
        public void ResetHealth()
        {
            _current = maxHealth;
            _dead = false;
        }

        private void Die()
        {
            _dead = true;
            OnDeath?.Invoke();
        }
    }
}
