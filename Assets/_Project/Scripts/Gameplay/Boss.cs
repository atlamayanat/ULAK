using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Ulak.Core;

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
    public int meleeDamage = 20;
    public int contactDamage = 10;

    public GameObject rangedProjectilePrefab;
    public GameObject meleeVisualPrefab;
    public Transform firePoint;

    private SpriteRenderer sr;
    private Color originalColor;
    private Vector3 originalScale;

    // --- YENİ YAPAY ZEKA DEĞİŞKENLERİ ---
    private float ceilingEscapeDir = 0f; // Tavandan kaçış yönü kilidi
    public float tavanKontrolMesafesi = 3.5f; // Yukarı doğru atılacak lazerin boyu

    public bool IsAlive => currentHealth > 0;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        currentMoveSpeed = baseMoveSpeed;
        defaultPosition = transform.position;

        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;

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

        // Çevre Kontrolleri (Lazerler)
        bool isGrounded = Physics2D.Raycast(transform.position, Vector2.down, 1.5f, groundLayer);

        Vector2 bakisYonu = new Vector2(Mathf.Sign(transform.localScale.x), 0);
        bool duvaraCarpti = Physics2D.Raycast(transform.position, bakisYonu, 1.2f, groundLayer);

        // YENİ LAZER: Tam kafasının üstünde engelleyici bir platform/tavan var mı?
        bool kafaUstuTavanVar = Physics2D.Raycast(transform.position, Vector2.up, tavanKontrolMesafesi, groundLayer);

        float dirX = 0f;

        // Havada donma kontrolü
        if (!isGrounded)
        {
            dirX = Mathf.Sign(rb.linearVelocity.x);
            if (Mathf.Abs(rb.linearVelocity.x) < 0.1f) dirX = Mathf.Sign(distX);
        }
        // 1. DURUM: OYUNCU ÜST KATTA VE BOSS TAVANIN ALTINDA (Gelişmiş Çıkış Arama)
        else if (distY > 1.5f && kafaUstuTavanVar)
        {
            // Eğer henüz bir kaçış yönü seçmediyse, en mantıklı yönü kilitle
            if (ceilingEscapeDir == 0f)
            {
                // Oyuncu ne taraftaysa o taraftaki çıkışa doğru gitmeyi dene
                if (Mathf.Abs(distX) > 0.5f)
                    ceilingEscapeDir = Mathf.Sign(distX);
                else
                    ceilingEscapeDir = (rb.linearVelocity.x >= 0) ? 1f : -1f; // Tam altındaysa mevcut yönünü koru
            }

            // Çıkış yolunda haritanın dış duvarına çarparsa körü körüne duvara yürüme, yönü tersine çevir!
            if (duvaraCarpti)
            {
                ceilingEscapeDir = -ceilingEscapeDir;
            }

            dirX = ceilingEscapeDir;
        }
        // 2. DURUM: OYUNCU ÜST KATTA AMA BOSS BOŞLUKTA (Tavan bitti, direkt zıpla!)
        else if (distY > 1.5f && !kafaUstuTavanVar)
        {
            ceilingEscapeDir = 0f; // Kaçış kilidini sıfırla
            dirX = Mathf.Sign(distX); // Oyuncuya yönel

            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce); // Yukarı fırla!
            }
        }
        // 3. DURUM: OYUNCU ALT KATTA (Aşağı düşme zekası)
        else if (distY < -1.5f)
        {
            ceilingEscapeDir = 0f;
            if (Mathf.Abs(distX) < 1.2f)
            {
                dirX = (Mathf.Abs(rb.linearVelocity.x) < 0.1f) ? 1f : Mathf.Sign(rb.linearVelocity.x);
            }
            else
            {
                dirX = Mathf.Sign(distX);
            }
        }
        // 4. DURUM: AYNI KATTALAR (Standart Takip)
        else
        {
            ceilingEscapeDir = 0f;
            if (Mathf.Abs(distX) > 0.2f)
            {
                dirX = Mathf.Sign(distX);
            }
        }

        // Genel Duvar Aşma (Aynı kattayken bir kutuya/engele çarparsa zıplaması için)
        if (isGrounded && duvaraCarpti && !kafaUstuTavanVar)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Hareketi Uygula
        rb.linearVelocity = new Vector2(dirX * currentMoveSpeed, rb.linearVelocity.y);

        // Yön Aynalama
        if (dirX != 0)
        {
            float yeniYönX = Mathf.Sign(dirX) * originalScale.x;
            transform.localScale = new Vector3(yeniYönX, originalScale.y, originalScale.z);
        }

        // SALDIRI KONTROLÜ
        if (Time.time >= nextAttackTime)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (Mathf.Abs(distY) < 2.2f && !duvaraCarpti)
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
            if (transform.localScale.x < 0) slash.transform.localScale = new Vector3(-1, 1, 1);
            Destroy(slash, 0.2f);
        }

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
                        float pushDirX = Mathf.Sign(hit.transform.position.x - transform.position.x);
                        Vector2 kb = new Vector2(pushDirX, 0.3f).normalized * 14f;
                        dmg.TakeDamage(meleeDamage, kb);
                    }
                }
            }
        }
    }

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
        rb.bodyType = RigidbodyType2D.Kinematic;

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

        // Editor'de tavan kontrol çizgisini mavi görmek için
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * tavanKontrolMesafesi);
    }
}