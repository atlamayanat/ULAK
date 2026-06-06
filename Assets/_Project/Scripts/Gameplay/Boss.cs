using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Ulak.Core; // Senin hasar sistemin

public class BossController : MonoBehaviour, IDamageable
{
    [Header("Hedef ve Fizik")]
    public Transform player;
    private Rigidbody2D rb;
    public float jumpForce = 12f;
    public LayerMask groundLayer;

    [Header("Boss Can Ayarları")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Yeni UI Ayarları")]
    public Image healthBarFill;
    public GameObject healthBarParent;

    [Header("Hareket ve Fazlar")]
    public float baseMoveSpeed = 3f;
    private float currentMoveSpeed;
    private Vector3 defaultPosition;
    private bool isPhase2 = false;
    private bool isResettingToCenter = false;

    [Header("Saldırı ve Hasar Ayarları")]
    public float meleeRange = 2.5f;
    public float attackCooldown = 2f;
    private float nextAttackTime = 0f;
    public int meleeDamage = 20;     // Kılıç vuruş hasarı
    public int contactDamage = 10;   // Karaktere düz çarpma hasarı

    public GameObject rangedProjectilePrefab;
    public GameObject meleeVisualPrefab;
    public Transform firePoint;

    private SpriteRenderer sr;
    private Color originalColor;
    private Vector3 originalScale; // Objeyi doğru döndürmek için hafıza alanı

    public bool IsAlive => currentHealth > 0;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        currentMoveSpeed = baseMoveSpeed;
        defaultPosition = transform.position;

        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;

        // Objelerin boyut hafızasını alıyoruz (FlipX tuzağından kurtulmak için)
        originalScale = transform.localScale;

        if (healthBarParent != null)
            StartCoroutine(BarIntroAnimation());
    }

    void Update()
    {
        if (player == null || isResettingToCenter || !IsAlive) return;

        if (currentHealth <= maxHealth * 0.5f && !isPhase2)
        {
            StartCoroutine(TransitionToPhase2());
            return;
        }

        float distX = player.position.x - transform.position.x;
        float distY = player.position.y - transform.position.y;

        bool isGrounded = Physics2D.Raycast(transform.position, Vector2.down, 1.5f, groundLayer);
        float dirX = 0f;

        // 1. DURUM: OYUNCU ALT KATTA (Boss üst katta tam üstündeyse kenara yürüyüp düşmeli)
        if (distY < -1.5f && isGrounded)
        {
            if (Mathf.Abs(distX) < 1.2f)
            {
                // Tam üst üste hizalandılarsa donup kalma! Boşluğa ulaşana kadar son gittiğin yöne inatla devam et
                if (Mathf.Abs(rb.linearVelocity.x) < 0.1f)
                    dirX = 1f;
                else
                    dirX = Mathf.Sign(rb.linearVelocity.x);
            }
            else
            {
                // X eksenleri henüz aynı değilse oyuncunun olduğu taraftaki platform kenarına yürü
                dirX = Mathf.Sign(distX);
            }
        }
        // 2. DURUM: OYUNCU ÜST KATTA (Zıplama pozisyonu al)
        else if (distY > 1.5f && isGrounded)
        {
            dirX = Mathf.Sign(distX);
            // Tam altındaysa ve zıplayamıyorsa platformun altından çıkmak için hafifçe yana hamle yap
            if (Mathf.Abs(distX) < 0.2f) dirX = 1f;
        }
        // 3. DURUM: AYNI KATTALAR (Klasik Takip)
        else if (Mathf.Abs(distX) > 0.2f)
        {
            dirX = Mathf.Sign(distX);
        }

        // ÜST KATA ZIPLAMA TETİKLEYİCİSİ
        if (isGrounded && distY > 1.5f && Mathf.Abs(distX) < 3f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Hareketi Uygula
        rb.linearVelocity = new Vector2(dirX * currentMoveSpeed, rb.linearVelocity.y);

        // KESİN DÖNÜŞ ÇÖZÜMÜ: Sadece görseli değil, firePoint dahil tüm gövdeyi yönüne göre aynalıyoruz
        if (dirX != 0)
        {
            float yeniYönX = Mathf.Sign(dirX) * originalScale.x;
            transform.localScale = new Vector3(yeniYönX, originalScale.y, originalScale.z);
        }

        // SALDIRI KONTROLÜ
        if (Time.time >= nextAttackTime)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            // Sadece Y ekseninde yakınlarsa (aynı kat hizası) atak yapsın
            if (Mathf.Abs(distY) < 2.2f)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

                if (distanceToPlayer <= meleeRange)
                    MeleeAttack();
                else
                    RangedAttack();
            }
        }
    }

    void MeleeAttack()
    {
        nextAttackTime = Time.time + attackCooldown;

        if (meleeVisualPrefab != null && firePoint != null)
        {
            GameObject slash = Instantiate(meleeVisualPrefab, firePoint.position, Quaternion.identity);

            // Boss sola bakıyorsa kılıç efektini de sola doğru ters çevir
            if (transform.localScale.x < 0)
                slash.transform.localScale = new Vector3(-1, 1, 1);

            Destroy(slash, 0.2f);
        }

        // KILIÇ HASAR ALANI (Görünmez Küre Taraması)
        if (firePoint != null)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(firePoint.position, meleeRange * 0.8f);
            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    var dmg = hit.GetComponent<IDamageable>();
                    if (dmg != null)
                    {
                        // Oyuncuyu vurduğu yöne doğru geri fırlatma vektörü
                        float pushDirX = Mathf.Sign(hit.transform.position.x - transform.position.x);
                        Vector2 kb = new Vector2(pushDirX, 0.3f).normalized * 14f;

                        dmg.TakeDamage(meleeDamage, kb);
                    }
                }
            }
        }
    }

    // HOLLOW KNIGHT TARZI BEDENSEL TEMAS HASARI (Üstüne düşerse hasar yeriz)
    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !isResettingToCenter && IsAlive)
        {
            var dmg = collision.gameObject.GetComponent<IDamageable>();
            if (dmg != null)
            {
                float pushDirX = Mathf.Sign(collision.transform.position.x - transform.position.x);
                Vector2 kb = new Vector2(pushDirX, 0.3f).normalized * 10f;
                dmg.TakeDamage(contactDamage, kb);
            }
        }
    }

    void RangedAttack()
    {
        nextAttackTime = Time.time + attackCooldown;

        if (rangedProjectilePrefab != null && firePoint != null)
        {
            GameObject ok = Instantiate(rangedProjectilePrefab, firePoint.position, Quaternion.identity);
            Vector2 yon = (player.position - firePoint.position).normalized;

            // Merminin Rigidbody2D bileşenine erişip fırlatıyoruz
            ok.GetComponent<Rigidbody2D>().linearVelocity = yon * 9f;

            Destroy(ok, 3f);
        }
    }

    public void TakeDamage(int damage, Vector2 knockback)
    {
        if (!IsAlive) return;
        currentHealth -= damage;

        if (healthBarFill != null)
            healthBarFill.rectTransform.localScale = new Vector3(currentHealth / maxHealth, 1, 1);

        StartCoroutine(DamageEffect(knockback));

        if (currentHealth <= 0)
        {
            if (healthBarParent != null) Destroy(healthBarParent);
            Destroy(gameObject);
        }
    }

    IEnumerator BarIntroAnimation()
    {
        healthBarParent.transform.localScale = new Vector3(0, 1, 1);
        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime * 1.5f;
            healthBarParent.transform.localScale = new Vector3(Mathf.Lerp(0, 1, timer), 1, 1);
            yield return null;
        }
    }

    IEnumerator DamageEffect(Vector2 knockback)
    {
        if (sr != null) sr.color = Color.white;
        rb.linearVelocity = new Vector2(knockback.x, rb.linearVelocity.y);
        yield return new WaitForSeconds(0.1f);
        if (sr != null) sr.color = originalColor;
    }

    IEnumerator TransitionToPhase2()
    {
        isResettingToCenter = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic; // Unity 6 Uyarısız Yeni Kod Yapısı

        while (Vector2.Distance(transform.position, defaultPosition) > 0.1f)
        {
            transform.position = Vector2.MoveTowards(transform.position, defaultPosition, baseMoveSpeed * 5f * Time.deltaTime);
            yield return null;
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        isPhase2 = true;
        currentMoveSpeed = baseMoveSpeed * 1.75f;
        attackCooldown *= 0.6f;

        yield return new WaitForSeconds(1f);
        isResettingToCenter = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (firePoint != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.4f);
            Gizmos.DrawWireSphere(firePoint.position, meleeRange * 0.8f);
        }
    }
}