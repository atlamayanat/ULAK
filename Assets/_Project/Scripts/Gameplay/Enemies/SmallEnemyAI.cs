using UnityEngine;
using Ulak.Core;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Ufak düşman (greybox v2):
    ///  - DEVRİYE: Oyuncuyu görmese de yavaşça sağa-sola gezerek arar.
    ///  - KOVALAMA: Oyuncu menzile girince hızlanıp peşine düşer;
    ///    görüşü kaybetse de bir süre ısrar eder (histerezis).
    ///  - ZIPLAMA: Önünde engel varsa ya da oyuncu yukarıdaysa
    ///    (cooldown'lu, yavaş tempolu) zıplar.
    ///  - Temasta oyuncuya hasar + knockback verir (cooldown'lu).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public class SmallEnemyAI : MonoBehaviour
    {
        [Header("Hareket")]
        [Tooltip("Kovalama hızı.")]
        [SerializeField] private float moveSpeed = 2.5f;
        [Tooltip("Devriye (arama) hızı — kovalamadan yavaş.")]
        [SerializeField] private float patrolSpeed = 1.2f;
        [Tooltip("Oyuncu bu mesafeye girince kovalama başlar.")]
        [SerializeField] private float aggroRange = 7f;
        [Tooltip("Kovalarken oyuncu bu mesafeden uzaklaşırsa devriyeye döner.")]
        [SerializeField] private float loseAggroRange = 12f;
        [Tooltip("Oyuncuyu bulmak için Tag. Sahnedeki oyuncuda bu Tag olmalı.")]
        [SerializeField] private string playerTag = "Player";

        [Header("Devriye")]
        [Tooltip("Devriyede yön değiştirme aralığı (sn, min).")]
        [SerializeField] private float patrolFlipMin = 2f;
        [Tooltip("Devriyede yön değiştirme aralığı (sn, max).")]
        [SerializeField] private float patrolFlipMax = 5f;

        [Header("Zıplama")]
        [Tooltip("Zıplama kuvveti (oyuncudan zayıf — yavaş tempolu).")]
        [SerializeField] private float jumpForce = 10f;
        [Tooltip("İki zıplama arası en az süre (sn).")]
        [SerializeField] private float jumpCooldown = 1.6f;
        [Tooltip("Önündeki engeli algılama mesafesi.")]
        [SerializeField] private float obstacleCheckDistance = 0.7f;
        [Tooltip("Zemin/engel sayılan layer'lar (boşsa Ground'a bağlanır).")]
        [SerializeField] private LayerMask groundLayer;

        [Header("Görünüm")]
        [Tooltip("Sprite varsayılan olarak sağa mı bakıyor? (Aynalama yönünü belirler.)")]
        [SerializeField] private bool spriteFacesRight = true;

        [Header("Temas hasarı")]
        [SerializeField] private int contactDamage = 1;
        [SerializeField] private float contactKnockback = 7f;
        [Tooltip("Aynı hedefe tekrar vurmadan önceki bekleme (sn).")]
        [SerializeField] private float contactCooldown = 0.8f;
        [Tooltip("Vuruş animasyonu başladıktan ne kadar sonra hasar girer (sn) — animasyonla senkron.")]
        [SerializeField] private float attackDamageDelay = 0.25f;
        [Tooltip("Hasar anında hedef hâlâ bu mesafedeyse isabet (kaçarsan ıskalama!).")]
        [SerializeField] private float attackHitRange = 1.6f;

        private Rigidbody2D _rb;
        private Health _health;
        private Knockback _knockback;
        private SpriteFlipbook _flipbook;
        private SpriteRenderer _sr;
        private Transform _player;
        private float _nextContactTime;
        private float _attackBusyUntil; // savururken yerinde durur
        private float _nextJumpTime;
        private float _patrolFlipTime;
        private int _patrolDir = 1;
        private bool _chasing;
        private bool _grounded;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();
            _knockback = GetComponent<Knockback>();
            _flipbook = GetComponent<SpriteFlipbook>();
            _sr = GetComponent<SpriteRenderer>();

            // Kendi kendine bağlanma: maske boş/her şey ise Ground'a daralt.
            if (groundLayer.value == 0 || groundLayer.value == -1)
            {
                int g = LayerMask.NameToLayer("Ground");
                if (g >= 0) groundLayer = 1 << g;
            }

            // Devriye başlangıcı: rastgele yön ve süre (sürüden ayrışsınlar).
            _patrolDir = Random.value < 0.5f ? -1 : 1;
            _patrolFlipTime = Time.time + Random.Range(patrolFlipMin, patrolFlipMax);
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

            UpdateGrounded();

            // Savurma sırasında dur — vuruş okunabilir olsun (kaçma fırsatı).
            if (Time.time < _attackBusyUntil)
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                return;
            }

            // --- Durum: kovala / devriye (histerezisli) ---
            float dx = 0f;
            if (_player != null)
            {
                dx = _player.position.x - transform.position.x;
                float adx = Mathf.Abs(dx);
                _chasing = _chasing ? adx <= loseAggroRange : adx <= aggroRange;
            }
            else
            {
                _chasing = false;
            }

            int dir;
            float speed;
            if (_chasing)
            {
                dir = dx >= 0f ? 1 : -1;
                speed = moveSpeed;
            }
            else
            {
                // Devriye: süre dolunca yön değiştir.
                if (Time.time >= _patrolFlipTime)
                {
                    _patrolDir = -_patrolDir;
                    _patrolFlipTime = Time.time + Random.Range(patrolFlipMin, patrolFlipMax);
                }
                dir = _patrolDir;
                speed = patrolSpeed;
            }

            _rb.linearVelocity = new Vector2(dir * speed, _rb.linearVelocity.y);

            // Gittiği yöne bak (oyuncu arkasına geçince döner).
            Face(dir);

            // --- Zıplama (cooldown'lu) ---
            if (_grounded && Time.time >= _nextJumpTime)
            {
                bool wallAhead = ObstacleAhead(dir);
                bool playerAbove = _chasing && _player != null
                                   && _player.position.y - transform.position.y > 1.1f
                                   && Mathf.Abs(dx) < 3f;
                if (wallAhead || playerAbove)
                    Jump();
            }
        }

        /// <summary>Sprite'ı verilen yatay yöne çevirir.</summary>
        private void Face(float dirX)
        {
            if (_sr == null || Mathf.Approximately(dirX, 0f)) return;
            _sr.flipX = spriteFacesRight ? dirX < 0f : dirX > 0f;
        }

        private void UpdateGrounded()
        {
            Vector2 feet = (Vector2)transform.position + Vector2.down * 0.55f;
            _grounded = Physics2D.OverlapCircle(feet, 0.15f, groundLayer);
        }

        private bool ObstacleAhead(int dir)
        {
            // Gövde hizasından ileri kısa ışın: basamak/duvar var mı?
            Vector2 origin = (Vector2)transform.position + new Vector2(0f, -0.1f);
            var hit = Physics2D.Raycast(origin, new Vector2(dir, 0f), obstacleCheckDistance, groundLayer);
            return hit.collider != null;
        }

        private void Jump()
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
            _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            _nextJumpTime = Time.time + jumpCooldown;
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
            _attackBusyUntil = Time.time + attackDamageDelay + 0.15f; // savurma boyunca dur

            // Vurmadan önce hedefe dön (arkadan yaklaşan oyuncuya da döner).
            Face(other.transform.position.x - transform.position.x);

            // Animasyon başlar; hasar, vuruş karesine denk gelecek şekilde gecikmeli girer.
            _flipbook?.PlayAttack();
            StartCoroutine(DealDamageAfterWindup(other.transform));
        }

        private System.Collections.IEnumerator DealDamageAfterWindup(Transform target)
        {
            yield return new WaitForSeconds(attackDamageDelay);

            if (!_health.IsAlive || target == null) yield break;

            // Hedef savurma sırasında kaçtıysa ISKALAR.
            if (Vector2.Distance(target.position, transform.position) > attackHitRange)
                yield break;

            var dmg = target.GetComponentInParent<IDamageable>();
            if (dmg == null || !dmg.IsAlive) yield break;

            float dirX = Mathf.Sign(target.position.x - transform.position.x);
            if (dirX == 0) dirX = -1f;
            Vector2 kb = new Vector2(dirX, 0.4f).normalized * contactKnockback;

            dmg.TakeDamage(contactDamage, kb);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, aggroRange);
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(transform.position, loseAggroRange);
        }
    }
}
