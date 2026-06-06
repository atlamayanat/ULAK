using UnityEngine;
using Ulak.Core;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Düşman mermisi (greybox v1). Merküt tarafından kod ile üretilir:
    /// verilen yönde sabit hızla uçar; oyuncuya çarparsa hasar + knockback,
    /// zemine/duvara çarparsa yok olur. Süre dolunca da kendini temizler.
    /// </summary>
    public class EnemyProjectile : MonoBehaviour
    {
        [SerializeField] private float lifetime = 5f;

        private int _damage = 1;
        private Vector2 _dir;

        /// <summary>Merküt tarafından üretim anında çağrılır.</summary>
        public void Init(Vector2 direction, float speed, int damage)
        {
            _dir = direction.normalized;
            _damage = damage;

            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearVelocity = _dir * speed;

            var col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f; // 0.25 ölçekle dünya yarıçapı ≈ 0.125

            Destroy(gameObject, lifetime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Kendi takımına (Enemy katmanı) çarpmaz.
            if (other.gameObject.layer == gameObject.layer) return;

            if (other.CompareTag("Player"))
            {
                var dmg = other.GetComponentInParent<IDamageable>();
                if (dmg != null && dmg.IsAlive)
                    dmg.TakeDamage(_damage, _dir * 5f);
                Destroy(gameObject);
                return;
            }

            // Zemine/duvara çarptı.
            if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
                Destroy(gameObject);
        }
    }
}
