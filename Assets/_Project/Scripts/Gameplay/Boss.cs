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
    public LayerMask groundLayer; // Zeminleri alg�lamak i�in

    [Header("Boss Can Ayarlar�")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Yeni UI Ayarlar�")]
    public Image healthBarFill;
    public GameObject healthBarParent;

    [Header("Hareket ve Fazlar")]
    public float baseMoveSpeed = 3f;
    private float currentMoveSpeed;
    private Vector3 defaultPosition;
    private bool isPhase2 = false;
    private bool isResettingToCenter = false;

    [Header("Sald�r� ve Efektler")]
    public float meleeRange = 2.5f;
    public float attackCooldown = 2f;
    private float nextAttackTime = 0f;

    public GameObject rangedProjectilePrefab; // Uzak atak mermisi
    public GameObject meleeVisualPrefab;      // Yak�n atak efekti
    public Transform firePoint;               // ��k�� noktas�

    private SpriteRenderer sr;
    private Color originalColor;

    public bool IsAlive => currentHealth > 0;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        currentMoveSpeed = baseMoveSpeed;
        defaultPosition = transform.position;

        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;

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

        // 1. DURUM: OYUNCU ALT KATTA (Boss kenardan düşmek için inatla yürümeli)
        if (distY < -1.5f && isGrounded)
        {
            // Eğer duruyorsa veya hızı çok düşükse sağa doğru yürümeye başla
            if (Mathf.Abs(rb.linearVelocity.x) < 0.1f)
                dirX = 1f;
            else
                dirX = Mathf.Sign(rb.linearVelocity.x); // Hangi yöne gidiyorsa inatla o yöne devam et ki kenardan düşsün!
        }
        // 2. DURUM: NORMAL TAKİP (Oyuncu aynı katta veya üstte)
        else if (Mathf.Abs(distX) > 0.2f)
        {
            dirX = Mathf.Sign(distX);
        }

        // 3. DURUM: OYUNCU ÜST KATTA (Boss'un zıplaması lazım)
        if (isGrounded && distY > 1.5f && Mathf.Abs(distX) < 3f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Hareketi Uygula
        rb.linearVelocity = new Vector2(dirX * currentMoveSpeed, rb.linearVelocity.y);

        if (dirX != 0 && sr != null)
        {
            sr.flipX = (dirX < 0);
        }

        // SALDIRI KONTROLÜ
        if (Time.time >= nextAttackTime)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            // Atak yapması için oyuncuyla kabaca aynı katta (Y ekseninde) olması lazım
            if (Mathf.Abs(distY) < 2.5f)
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

        // Yak�n atak g�rselini FirePoint noktas�nda olu�tur ve 0.2 saniye sonra yok et
        if (meleeVisualPrefab != null && firePoint != null)
        {
            GameObject slash = Instantiate(meleeVisualPrefab, firePoint.position, Quaternion.identity);
            Destroy(slash, 0.2f);
        }
    }

    void RangedAttack()
    {
        nextAttackTime = Time.time + attackCooldown;

        // Mermiyi olu�tur ve oyuncuya do�ru f�rlat
        if (rangedProjectilePrefab != null && firePoint != null)
        {
            GameObject ok = Instantiate(rangedProjectilePrefab, firePoint.position, Quaternion.identity);

            // Oyuncunun o anki pozisyonuna do�ru bir vekt�r (y�n) hesapla
            Vector2 yon = (player.position - firePoint.position).normalized;
            ok.GetComponent<Rigidbody2D>().linearVelocity = yon * 8f; // Mermiyi h�zland�r

            Destroy(ok, 3f); // Ekranda sonsuza kadar gitmemesi i�in 3 saniye sonra sil
        }
    }

    public void TakeDamage(int damage, Vector2 knockback)
    {
        if (!IsAlive) return;
        currentHealth -= damage;

        // YEN� CAN BARI G�NCELLEMES� (Scale Method)
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

        // Senin k�l�� kodunun g�nderdi�i knockback'i fizik motoruna (velocity) uyguluyoruz
        rb.linearVelocity = new Vector2(knockback.x, rb.linearVelocity.y);

        yield return new WaitForSeconds(0.1f);
        if (sr != null) sr.color = originalColor;
    }

    IEnumerator TransitionToPhase2()
    {
        isResettingToCenter = true;

        rb.linearVelocity = Vector2.zero; // Ko�may� durdur
        rb.isKinematic = true;      // Merkeze u�arken yer�ekimi a�a�� �ekmesin

        while (Vector2.Distance(transform.position, defaultPosition) > 0.1f)
        {
            transform.position = Vector2.MoveTowards(transform.position, defaultPosition, baseMoveSpeed * 5f * Time.deltaTime);
            yield return null;
        }

        rb.isKinematic = false;     // Yer�ekimini geri a�
        isPhase2 = true;
        currentMoveSpeed = baseMoveSpeed * 1.75f;
        attackCooldown *= 0.6f;

        yield return new WaitForSeconds(1f);
        isResettingToCenter = false;
    }
}