using UnityEngine;
using Ulak.Core;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Ufak düşman (greybox v1):
    ///  - Oyuncuya doğru yatay yürür.
    ///  - Temasta oyuncuya hasar + knockback verir (cooldown'lu).
    ///  - Kendi knockback'ine otomatik tepki verir (Health üzerindeki Knockback bileşeni sayesinde).
    ///
    /// Hedef his: yaklaş → (oyuncu) zamanla → vur → geri savur → tekrar yaklaş.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public class SmallEnemyAI : MonoBehaviour
    {
        [Header("Hareket")]
        [SerializeField] private float moveSpeed = 2.5f;
        [Tooltip("Oyuncu bu mesafeden uzaktaysa düşman yerinde bekler (engellere yığılmayı önler).")]
        [SerializeField] private float aggroRange = 7f;
        [Tooltip("Oyuncuyu bulmak için Tag. Sahnedeki oyuncuda bu Tag olmalı.")]
        [SerializeField] private string playerTag = "Player";

        [Header("Temas hasarı")]
        [SerializeField] private int contactDamage = 1;
        [SerializeField] private float contactKnockback = 7f;
        [Tooltip("Aynı hedefe tekrar vurmadan önceki bekleme (sn).")]
        [SerializeField] private float contactCooldown = 0.8f;

        private Rigidbody2D _rb;
        private Health _health;
        private Knockback _knockback;
        private Transform _player;
        private float _nextContactTime;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();
            _knockback = GetComponent<Knockback>();
        }

        private void Start()
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null) _player = go.transform;
        }

        private void FixedUpdate()
        {
            if (!_health.IsAlive) return;
            if (_knockback != null && _knockback.IsBeingKnockedBack) return; // savruluyorsa kontrolü bırak
            if (_player == null) return;

            float dx = _player.position.x - transform.position.x;

            // Oyuncu menzil dışındaysa bekle — uzaktaki düşmanların
            // basamak/duvar diplerine yığılıp takılmasını önler.
            if (Mathf.Abs(dx) > aggroRange)
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                return;
            }

            float dirX = Mathf.Sign(dx);
            _rb.linearVelocity = new Vector2(dirX * moveSpeed, _rb.linearVelocity.y);
        }

        private void OnCollisionStay2D(Collision2D collision) => TryContactDamage(collision.collider);
        private void OnTriggerStay2D(Collider2D other) => TryContactDamage(other);

        private void TryContactDamage(Collider2D other)
        {
            if (!_health.IsAlive || Time.time < _nextContactTime) return;
            if (!other.CompareTag(playerTag)) return;

            var dmg = other.GetComponentInParent<IDamageable>();
            if (dmg == null || !dmg.IsAlive) return;

            _nextContactTime = Time.time + contactCooldown;

            // Oyuncuyu düşmandan uzağa (geriye) savur.
            float dirX = Mathf.Sign(other.transform.position.x - transform.position.x);
            if (dirX == 0) dirX = -1f;
            Vector2 kb = new Vector2(dirX, 0.4f).normalized * contactKnockback;

            dmg.TakeDamage(contactDamage, kb);
        }
    }
}
