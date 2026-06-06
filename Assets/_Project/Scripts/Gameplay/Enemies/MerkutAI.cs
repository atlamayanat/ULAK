using UnityEngine;
using Ulak.Core;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Merküt — uçan, uzaktan hasar veren canavar (greybox v1):
    ///  - Havada süzülür (hafif salınımla), yerçekiminden etkilenmez.
    ///  - Oyuncuyla arasında tercih ettiği mesafeyi korur (yaklaşma/kaçma).
    ///  - Menzildeyken belirli aralıklarla oyuncuya ok (mermi) fırlatır.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public class MerkutAI : MonoBehaviour
    {
        [Header("Uçuş")]
        [SerializeField] private float moveSpeed = 2.2f;
        [Tooltip("Oyuncuyla korunmak istenen yatay mesafe.")]
        [SerializeField] private float preferredRange = 7f;
        [SerializeField] private float aggroRange = 12f;
        [Tooltip("Havada salınım genliği (birim).")]
        [SerializeField] private float bobAmplitude = 0.35f;
        [SerializeField] private float bobFrequency = 2f;

        [Header("Atış")]
        [SerializeField] private float fireInterval = 2.2f;
        [SerializeField] private float projectileSpeed = 7f;
        [SerializeField] private int projectileDamage = 1;
        [Tooltip("Mermi görseli (boşsa kendi sprite'ı küçültülerek kullanılır).")]
        [SerializeField] private Sprite projectileSprite;

        [Header("Görünüm")]
        [Tooltip("Sprite varsayılan olarak sağa mı bakıyor? (Kartal görseli SOLA bakar.)")]
        [SerializeField] private bool spriteFacesRight = true;

        [SerializeField] private string playerTag = "Player";

        private Rigidbody2D _rb;
        private Health _health;
        private Knockback _knockback;
        private SpriteRenderer _sr;
        private Transform _player;
        private float _baseY;
        private float _nextFireTime;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();
            _knockback = GetComponent<Knockback>();
            _sr = GetComponent<SpriteRenderer>();
            _rb.gravityScale = 0f; // uçan birim
            _baseY = transform.position.y;
        }

        private void Start()
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null) _player = go.transform;
            _nextFireTime = Time.time + fireInterval * 0.5f;
        }

        private void FixedUpdate()
        {
            if (!_health.IsAlive) return;
            if (_knockback != null && _knockback.IsBeingKnockedBack) return;

            // Dikey: taban yüksekliği etrafında salınım.
            float desiredY = _baseY + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            float vy = (desiredY - transform.position.y) * 4f;

            float vx = 0f;
            bool playerInRange = false;
            if (_player != null)
            {
                float dx = _player.position.x - transform.position.x;
                if (Mathf.Abs(dx) <= aggroRange)
                {
                    playerInRange = true;
                    // Tercih edilen mesafeyi koru: hangi taraftaysa o tarafta kal.
                    float side = transform.position.x >= _player.position.x ? 1f : -1f;
                    float desiredX = _player.position.x + side * preferredRange;
                    vx = Mathf.Clamp(desiredX - transform.position.x, -1f, 1f) * moveSpeed;
                }
            }

            _rb.linearVelocity = new Vector2(vx, vy);

            // Kartal her zaman tetikte: oyuncu varsa yüzü hep ona dönük.
            if (_player != null)
                Face(_player.position.x - transform.position.x);
            else if (Mathf.Abs(vx) > 0.01f)
                Face(vx);
        }

        private void Update()
        {
            if (!_health.IsAlive || _player == null) return;
            if (Time.time < _nextFireTime) return;
            if (Mathf.Abs(_player.position.x - transform.position.x) > aggroRange) return;

            Fire();
            _nextFireTime = Time.time + fireInterval;
        }

        private void Fire()
        {
            var go = new GameObject("MerkutOku");
            go.transform.position = transform.position;
            go.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
            go.layer = gameObject.layer; // Enemy katmanı → oyuncunun kılıcıyla da vurulabilir

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = projectileSprite != null ? projectileSprite : GetComponent<SpriteRenderer>().sprite;
            sr.color = Color.white; // mermi sprite'ı kendi rengini taşır (kırmızı top)
            sr.sortingOrder = 11;

            Vector2 dir = ((Vector2)_player.position + Vector2.up * 0.3f
                           - (Vector2)transform.position).normalized;

            var proj = go.AddComponent<EnemyProjectile>();
            proj.Init(dir, projectileSpeed, projectileDamage);
        }

        /// <summary>Sprite'ı verilen yatay yöne çevirir.</summary>
        private void Face(float dirX)
        {
            if (_sr == null || Mathf.Approximately(dirX, 0f)) return;
            _sr.flipX = spriteFacesRight ? dirX < 0f : dirX > 0f;
        }
    }
}
