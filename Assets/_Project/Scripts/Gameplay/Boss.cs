using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Ulak.Core;

public class BossController : MonoBehaviour, IDamageable
{
    [Header("Hedef ve Fizik")]
    public Transform player;
    private Rigidbody2D rb;
    private Collider2D bossCollider;
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

    [Header("Saldırı ve Hasar")]
    public float meleeRange = 2.5f;
    public float attackCooldown = 2f;
    private float nextAttackTime = 0f;
    public int meleeDamage = 20;
    public int contactDamage = 10;

    [Tooltip("Boss kaç saniye yakın vuruş yapamazsa olduğu yere çakılıp uzaktan sıkmaya başlar?")]
    public float antiKiteSuresi = 5f; // YENİ: Inspector'dan ayarlanabilir süre

    public GameObject rangedProjectilePrefab;
    public GameObject meleeVisualPrefab;
    public Transform firePoint;

    private SpriteRenderer sr;
    private Color originalColor;
    private Vector3 originalScale;

    // --- DURUM (STATE) DEĞİŞKENLERİ ---
    private bool isLeaping = false;
    private bool isStunned = false;
    private bool isAttacking = false;
    private float lastMeleeTime = 0f;

    public bool IsAlive => currentHealth > 0;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        bossCollider = GetComponent<Collider2D>();
        currentHealth = maxHealth;
        currentMoveSpeed = baseMoveSpeed;
        defaultPosition = transform.position;

        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;

        originalScale = transform.localScale;
        lastMeleeTime = Time.time;

        if (healthBarParent != null)
            StartCoroutine(BarIntroAnimation());
    }

    void Update()
    {
        if (player == null || isResettingToCenter || !IsAlive || isLeaping || isStunned || isAttacking) return;

        if (currentHealth <= maxHealth * 0.5f && !isPhase2)
        {
            StartCoroutine(TransitionToPhase2());
            return;
        }

        float distX = player.position.x - transform.position.x;
        float distY = player.position.y - transform.position.y;
        bool isGrounded = Physics2D.Raycast(transform.position, Vector2.down, 1.5f, groundLayer);

        Vector2 bakisYonu = new Vector2(Mathf.Sign(transform.localScale.x), 0);
        bool duvaraCarpti = Physics2D.Raycast(transform.position, bakisYonu, 1.2f, groundLayer);

        // YENİ: Belirlediğin süre aşıldı mı?
        bool antiKiteAktif = (Time.time - lastMeleeTime > antiKiteSuresi);

        // --- SALDIRI KARAR MEKANİZMASI ---
        if (Time.time >= nextAttackTime && isGrounded)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            bool ayniKatta = Mathf.Abs(distY) < 2.5f;

            // 1. Yakın vurabiliyorsa vur (Sayacı Sıfırlar)
            if (ayniKatta && distanceToPlayer <= meleeRange && !duvaraCarpti)
            {
                StartCoroutine(SaldiriAnimasyonu(true));
                return;
            }
            // 2. Anti-Kite Aktifse VEYA normal uzak saldırı pozisyonundaysa uzaktan sık
            else if (antiKiteAktif || (ayniKatta && !duvaraCarpti))
            {
                StartCoroutine(SaldiriAnimasyonu(false));
                return;
            }
        }

        // --- HAREKET SİSTEMİ ---
        float dirX = 0f;

        // TARET MODU: Eğer Kite süresi dolduysa yürümeyi tamamen kes, olduğu yerde kalıp sana dönsün
        if (antiKiteAktif && isGrounded)
        {
            dirX = 0f;
            float yon = Mathf.Sign(distX);
            if (yon != 0)
                transform.localScale = new Vector3(yon * originalScale.x, originalScale.y, originalScale.z);
        }
        else
        {
            // NORMAL KOVALAMA
            if (distY < -1.5f)
            {
                if (Mathf.Abs(distX) < 1.5f)
                    dirX = Mathf.Sign(transform.localScale.x);
                else
                    dirX = Mathf.Sign(distX);
            }
            else
            {
                if (Mathf.Abs(distX) > 0.2f)
                    dirX = Mathf.Sign(distX);
            }

            if (isGrounded && duvaraCarpti && distY < 2f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
        }

        // Hareketi Uygula
        rb.linearVelocity = new Vector2(dirX * currentMoveSpeed, rb.linearVelocity.y);

        if (dirX != 0 && !antiKiteAktif)
        {
            float yeniYönX = Mathf.Sign(dirX) * originalScale.x;
            transform.localScale = new Vector3(yeniYönX, originalScale.y, originalScale.z);
        }
    }

    IEnumerator SaldiriAnimasyonu(bool isMelee)
    {
        isAttacking = true;

        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        float dirX = Mathf.Sign(player.position.x - transform.position.x);
        if (dirX != 0)
        {
            transform.localScale = new Vector3(dirX * originalScale.x, originalScale.y, originalScale.z);
        }

        yield return new WaitForSeconds(0.2f);

        if (isMelee)
        {
            MeleeAttack();
            lastMeleeTime = Time.time; // Yakın vuruş yaptı! Anti-Kite Taret modundan çıkıp tekrar kovalamaya başlar
        }
        else
        {
            RangedAttack();
        }

        yield return new WaitForSeconds(0.5f);

        isAttacking = false;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isLeaping || player == null || isStunned || isAttacking) return;

        BossZiplamaNoktasi ziplamaNoktasi = collision.GetComponent<BossZiplamaNoktasi>();
        if (ziplamaNoktasi != null)
        {
            if (player.position.y > transform.position.y + 1.5f)
            {
                StartCoroutine(ScriptedLeap(ziplamaNoktasi.hedefUstNokta.position));
            }
        }
    }

    IEnumerator ScriptedLeap(Vector3 endPos)
    {
        isLeaping = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (bossCollider != null) bossCollider.enabled = false;

        Vector3 startPos = transform.position;
        float duration = 0.4f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        if (bossCollider != null) bossCollider.enabled = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        isLeaping = false;

        if (Vector2.Distance(transform.position, player.position) <= meleeRange)
        {
            StartCoroutine(SaldiriAnimasyonu(true));
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
        if (collision.gameObject.CompareTag("Player") && !isResettingToCenter && IsAlive && !isLeaping)
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

        if (currentHealth <= 0)
        {
            if (healthBarParent != null) Destroy(healthBarParent);
            Destroy(gameObject);
            return;
        }

        if (!isAttacking && !isLeaping)
        {
            StartCoroutine(DamageEffect(knockback));
        }
        else
        {
            StartCoroutine(SadeceRenkDegistir());
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
        isStunned = true;

        if (sr != null) sr.color = Color.white;
        rb.linearVelocity = new Vector2(knockback.x, rb.linearVelocity.y);

        yield return new WaitForSeconds(0.2f);

        if (sr != null) sr.color = originalColor;
        isStunned = false;
    }

    IEnumerator SadeceRenkDegistir()
    {
        if (sr != null) sr.color = Color.white;
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
    }
}