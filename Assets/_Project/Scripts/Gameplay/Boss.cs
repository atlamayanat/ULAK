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

    [Tooltip("Boss kaç saniye hasar veremezse/alamazsa olduğu yere çakılıp uzaktan sıkmaya başlar?")]
    public float antiKiteSuresi = 5f;

    public GameObject rangedProjectilePrefab;
    public GameObject meleeVisualPrefab;
    public Transform firePoint;

    [Header("Büyü Atışı (Mavi Ateş)")]
    [Tooltip("Mavi ateş animasyon kareleri (sheet'ten dilimli).")]
    public Sprite[] maviAtesKareleri;
    public int kureSayisi = 4;
    public float yogunlasmaSuresi = 1f;     // küreler yanında bekler
    public float kureAraligi = 0.12f;       // peş peşe fırlatma arası
    public float kureHizi = 5f;
    public int kureHasari = 10;
    public float kureOmru = 4.5f;
    [Tooltip("Büyü için gereken asgari oyuncu mesafesi (yakında pençe tercih edilir).")]
    public float buyuMinMesafe = 4.5f;
    [Tooltip("İki büyü atışı arasındaki bekleme süresi (sn).")]
    public float buyuBeklemesi = 10f;
    private float sonrakiBuyu = 0f;

    [Header("Işınlanma")]
    [Tooltip("Sabit ışınlanma noktaları — oyuncuya en yakın olana ışınlanır.")]
    public Transform[] isinlanmaNoktalari;
    public float isinlanmaBeklemesi = 15f;
    [Tooltip("Oyuncu bundan uzaksa ışınlanabilir.")]
    public float isinlanmaMinMesafe = 6f;
    public float isinlanmaFadeSuresi = 0.5f;
    private float sonrakiIsinlanma = 0f;
    private float sonYuzDonusu = 0f; // yüz çevirme titreme kilidi
    private static readonly Color isinlanmaRengi = new Color(0.45f, 0.75f, 1f);

    private SpriteRenderer sr;
    private Color originalColor;
    private Vector3 originalScale;

    // --- DURUM (STATE) DEĞİŞKENLERİ ---
    private bool isLeaping = false;
    private bool isStunned = false;
    private bool isAttacking = false;
    private float lastCombatTime = 0f;

    // Otomatik Kat Değiştirme Sistemi İçin Hafıza Alanı
    private BossZiplamaNoktasi[] tumZiplamaNoktalari;

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

        // Can barı: Filled moduna al — çerçeve boyutu SABİT kalır,
        // yalnızca dolgu soldan boşalır (localScale küçültmesi bar
        // boyutunu bozuyordu).
        if (healthBarFill != null)
        {
            healthBarFill.type = Image.Type.Filled;
            healthBarFill.fillMethod = Image.FillMethod.Horizontal;
            healthBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            healthBarFill.fillAmount = 1f;
            healthBarFill.rectTransform.localScale = Vector3.one; // eski ölçek kalıntısını sıfırla
        }

        originalScale = transform.localScale;
        lastCombatTime = Time.time;
        sonrakiIsinlanma = Time.time + 5f; // açılışta hemen ışınlanmasın
        sonrakiBuyu = Time.time + 3f;      // açılışta hemen küre yağdırmasın

        // Haritadaki tüm görünmez zıplama collider'larını dinamik olarak bulur
        tumZiplamaNoktalari = FindObjectsByType<BossZiplamaNoktasi>(FindObjectsSortMode.None);

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
        // Zemin kontrolü collider TABANINDAN yapılır — sprite büyüyünce merkezden
        // atılan 1.5'lik ışın yere yetişmiyordu (büyü/ışınlanma/zıplama kilitleniyordu).
        Vector2 taban = bossCollider != null
            ? new Vector2(bossCollider.bounds.center.x, bossCollider.bounds.min.y + 0.05f)
            : (Vector2)transform.position;
        bool isGrounded = Physics2D.Raycast(taban, Vector2.down, 0.6f, groundLayer);

        Vector2 bakisYonu = new Vector2(Mathf.Sign(transform.localScale.x), 0);
        bool duvaraCarpti = Physics2D.Raycast(transform.position, bakisYonu, 1.2f, groundLayer);

        bool antiKiteAktif = (Time.time - lastCombatTime > antiKiteSuresi);

        // --- IŞINLANMA: ara sıra oyuncunun yakınına belir ---
        if (Time.time >= sonrakiIsinlanma && isGrounded
            && Vector2.Distance(transform.position, player.position) > isinlanmaMinMesafe)
        {
            StartCoroutine(Isinlan());
            return;
        }

        // --- SALDIRI KARAR MEKANİZMASI ---
        if (Time.time >= nextAttackTime && isGrounded)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            bool ayniKatta = Mathf.Abs(distY) < 2.5f;

            if (ayniKatta && distanceToPlayer <= meleeRange && !duvaraCarpti)
            {
                StartCoroutine(SaldiriAnimasyonu(true)); // PENÇE
                return;
            }
            else if (Time.time >= sonrakiBuyu
                     && (antiKiteAktif
                         || (ayniKatta && !duvaraCarpti && distanceToPlayer >= buyuMinMesafe)))
            {
                StartCoroutine(SaldiriAnimasyonu(false)); // BÜYÜ ATIŞI
                return;
            }
        }

        // --- HAREKET SİSTEMİ ---
        float dirX = 0f;

        if (antiKiteAktif && isGrounded)
        {
            dirX = 0f;
            YuzCevir(distX);
        }
        else
        {
            // 1. DURUM: OYUNCU ALT KATTAYSA
            if (distY < -1.5f)
            {
                if (Mathf.Abs(distX) < 1.5f)
                    dirX = Mathf.Sign(transform.localScale.x); // Kenara kadar yürüyüp düş
                else
                    dirX = Mathf.Sign(distX);
            }
            // 2. DURUM: OYUNCU ÜST KATTAYSA (Kritik Çözüm!)
            else if (distY > 1.5f)
            {
                // Doğrudan oyuncunun altına gitme, en yakın merdivene/zıplama trigger'ına koş!
                if (tumZiplamaNoktalari != null && tumZiplamaNoktalari.Length > 0)
                {
                    Transform enYakinNokta = tumZiplamaNoktalari[0].transform;
                    float enYakinMesafe = Vector2.Distance(transform.position, enYakinNokta.position);

                    for (int i = 1; i < tumZiplamaNoktalari.Length; i++)
                    {
                        float m = Vector2.Distance(transform.position, tumZiplamaNoktalari[i].transform.position);
                        if (m < enYakinMesafe)
                        {
                            enYakinMesafe = m;
                            enYakinNokta = tumZiplamaNoktalari[i].transform;
                        }
                    }
                    dirX = Mathf.Sign(enYakinNokta.position.x - transform.position.x);
                }
                else
                {
                    dirX = Mathf.Sign(distX);
                }
            }
            // 3. DURUM: AYNI KATTALARSA
            else
            {
                // Ölü bölge geniş tutuldu — oyuncu dibimizdeyken her karede
                // sağ-sol işaret değiştirip titremeyelim.
                if (Mathf.Abs(distX) > 0.6f)
                    dirX = Mathf.Sign(distX);
            }

            if (isGrounded && duvaraCarpti && distY < 2f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
        }

        rb.linearVelocity = new Vector2(dirX * currentMoveSpeed, rb.linearVelocity.y);

        if (dirX != 0 && !antiKiteAktif)
            YuzCevir(dirX);
    }

    /// <summary>
    /// Yüz çevirme — hızlı sağ-sol titremesini önlemek için en erken
    /// 0.3 sn'de bir döner. Saldırı anında zorla=true ile anında döner.
    /// </summary>
    void YuzCevir(float yon, bool zorla = false)
    {
        if (yon == 0) return;
        float hedefX = Mathf.Sign(yon) * originalScale.x;
        if (Mathf.Approximately(transform.localScale.x, hedefX)) return; // zaten o yöne bakıyor
        if (!zorla && Time.time - sonYuzDonusu < 0.3f) return;           // titreme kilidi
        sonYuzDonusu = Time.time;
        transform.localScale = new Vector3(hedefX, originalScale.y, originalScale.z);
    }

    IEnumerator SaldiriAnimasyonu(bool isMelee)
    {
        isAttacking = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        float dirX = Mathf.Sign(player.position.x - transform.position.x);
        if (dirX != 0)
        {
            YuzCevir(dirX, true); // vururken hedefe mutlaka dön
        }

        yield return new WaitForSeconds(0.2f);

        if (isMelee) MeleeAttack();
        else yield return BuyuAtisi(); // 1 sn yoğunlaşma + 4 küre peş peşe

        lastCombatTime = Time.time;
        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
    }

    /// <summary>
    /// BÜYÜ ATIŞI: 1 sn yoğunlaşır (küreler yanında süzülür), ardından
    /// küreleri peş peşe oyuncuya gönderir. Küreler gevşek güdümlüdür —
    /// kaçılabilir ya da kılıçla patlatılabilir.
    /// </summary>
    IEnumerator BuyuAtisi()
    {
        nextAttackTime = Time.time + attackCooldown;
        sonrakiBuyu = Time.time + buyuBeklemesi; // büyünün kendi bekleme süresi

        // Küreleri yanında çağır (yay düzeni).
        Vector2[] ofsetler =
        {
            new Vector2(-1.3f, 1.6f), new Vector2(-0.45f, 2.1f),
            new Vector2(0.45f, 2.1f), new Vector2(1.3f, 1.6f)
        };

        var kureler = new System.Collections.Generic.List<Ulak.Gameplay.MaviAtesTopu>();
        int adet = Mathf.Min(kureSayisi, ofsetler.Length);
        for (int i = 0; i < adet; i++)
        {
            var go = new GameObject("MaviAtes");
            go.layer = gameObject.layer; // Enemy → oyuncunun kılıcı vurabilir
            go.transform.position = (Vector2)transform.position + ofsetler[i];
            go.transform.localScale = Vector3.one * 0.9f;

            var srK = go.AddComponent<SpriteRenderer>();
            if (maviAtesKareleri != null && maviAtesKareleri.Length > 0)
                srK.sprite = maviAtesKareleri[0];
            srK.sortingOrder = 11;

            if (maviAtesKareleri != null && maviAtesKareleri.Length > 1)
            {
                var book = go.AddComponent<SpriteFlipbook>();
                book.SetFrameInterval(0.1f);
                // frames alanı private — yansıma ile doldur (prefabsız üretim).
                typeof(SpriteFlipbook)
                    .GetField("frames", System.Reflection.BindingFlags.Instance |
                                        System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(book, maviAtesKareleri);
            }

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.3f;

            var top = go.AddComponent<Ulak.Gameplay.MaviAtesTopu>();
            top.Kur(transform, ofsetler[i], player, kureHizi, kureHasari, kureOmru, i * 1.7f);
            kureler.Add(top);
        }

        // 1 sn yoğunlaşma — küreler yörüngede bekler.
        yield return new WaitForSeconds(yogunlasmaSuresi);

        // Peş peşe fırlat.
        foreach (var k in kureler)
        {
            if (k != null) k.Firlat();
            yield return new WaitForSeconds(kureAraligi);
        }
    }

    /// <summary>
    /// IŞINLANMA: maviye dönüp saydamlaşarak kaybolur, oyuncunun yakınında
    /// (dibinde değil) ters animasyonla belirir.
    /// </summary>
    IEnumerator Isinlan()
    {
        sonrakiIsinlanma = Time.time + isinlanmaBeklemesi;

        Vector3 hedefPoz;
        if (isinlanmaNoktalari != null && isinlanmaNoktalari.Length > 0)
        {
            // Sabit noktalardan OYUNCUYA en yakın olanı seç.
            Transform enIyi = null;
            float enKisa = float.MaxValue;
            foreach (var n in isinlanmaNoktalari)
            {
                if (n == null) continue;
                float m = Vector2.Distance(n.position, player.position);
                if (m < enKisa) { enKisa = m; enIyi = n; }
            }
            if (enIyi == null) yield break;
            hedefPoz = new Vector3(enIyi.position.x, enIyi.position.y, transform.position.z);
        }
        else
        {
            // Yedek: nokta tanımlanmamışsa oyuncunun 2.5-4 birim yanına ışınlan.
            float yan = Random.value < 0.5f ? -1f : 1f;
            float hedefX = Mathf.Clamp(player.position.x + yan * Random.Range(2.5f, 4f), -17.5f, 9.5f);
            var zemin = Physics2D.Raycast(
                new Vector2(hedefX, player.position.y + 3f), Vector2.down, 14f, groundLayer);
            if (zemin.collider == null) yield break; // güvenli zemin yok — vazgeç
            hedefPoz = new Vector3(hedefX, zemin.point.y + 1.45f, transform.position.z);
        }

        // Y'yi zemine oturt — nokta havada dursa bile boss yere basar.
        var zem = Physics2D.Raycast((Vector2)hedefPoz + Vector2.up * 1.5f, Vector2.down, 12f, groundLayer);
        if (zem.collider != null) hedefPoz.y = zem.point.y + 1.7f;

        isLeaping = true; // mevcut AI kapıları bu bayrağı zaten sayıyor
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        if (bossCollider != null) bossCollider.enabled = false;

        // Maviye dönüp saydamlaş.
        yield return RenkGecisi(originalColor, isinlanmaRengi, 1f, 0f, isinlanmaFadeSuresi);

        transform.position = hedefPoz;

        // Yeni konumda ters animasyonla belir.
        yield return RenkGecisi(isinlanmaRengi, originalColor, 0f, 1f, isinlanmaFadeSuresi);

        if (bossCollider != null) bossCollider.enabled = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        isLeaping = false;
        lastCombatTime = Time.time;
    }

    IEnumerator RenkGecisi(Color rBas, Color rSon, float aBas, float aSon, float sure)
    {
        float t = 0f;
        while (t < sure)
        {
            t += Time.deltaTime;
            if (sr != null)
            {
                Color c = Color.Lerp(rBas, rSon, t / sure);
                c.a = Mathf.Lerp(aBas, aSon, t / sure);
                sr.color = c;
            }
            yield return null;
        }
        if (sr != null)
        {
            Color son = rSon; son.a = aSon; sr.color = son;
        }
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
                        lastCombatTime = Time.time;
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
                lastCombatTime = Time.time;
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

            // Yeni fırlatılan mermiye hızını veriyoruz
            var projectileScript = ok.GetComponent<BossProjectile>();
            if (projectileScript != null)
            {
                ok.GetComponent<Rigidbody2D>().linearVelocity = yon * projectileScript.speed;
            }
            else
            {
                ok.GetComponent<Rigidbody2D>().linearVelocity = yon * 9f;
            }
            Destroy(ok, 4f);
        }
    }

    public void TakeDamage(int damage, Vector2 knockback)
    {
        if (!IsAlive) return;
        currentHealth -= damage;
        lastCombatTime = Time.time;

        // Boss dövüşünde canavar kesimi yok — her isabet oyuncuya 1 yük kazandırır.
        if (player != null)
        {
            var yukler = player.GetComponentInParent<Ulak.Gameplay.KillCharges>();
            if (yukler != null) yukler.Add(1);
        }

        if (healthBarFill != null)
            healthBarFill.fillAmount = Mathf.Clamp01(currentHealth / maxHealth); // boyut sabit, dolgu boşalır

        if (currentHealth <= 0)
        {
            if (healthBarParent != null) Destroy(healthBarParent);

            // Son vuruş: boss ölmez — final ara sahnesi devreye girer.
            var araSahne = FindFirstObjectByType<Ulak.Gameplay.GulyabaniFinalSahnesi>();
            if (araSahne != null)
            {
                araSahne.Baslat(this);
                return;
            }

            Destroy(gameObject); // ara sahne kurulmamışsa eski davranış
            return;
        }

        if (!isAttacking && !isLeaping) StartCoroutine(DamageEffect(knockback));
        else StartCoroutine(SadeceRenkDegistir());
    }

    IEnumerator BarIntroAnimation() { /* aynı kalacak */ yield return null; }
    IEnumerator DamageEffect(Vector2 knockback) { isStunned = true; if (sr != null) sr.color = Color.white; rb.linearVelocity = new Vector2(knockback.x, rb.linearVelocity.y); yield return new WaitForSeconds(0.2f); if (sr != null) sr.color = originalColor; isStunned = false; }
    IEnumerator SadeceRenkDegistir() { if (sr != null) sr.color = Color.white; yield return new WaitForSeconds(0.1f); if (sr != null) sr.color = originalColor; }
    IEnumerator TransitionToPhase2() { isResettingToCenter = true; rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Kinematic; while (Vector2.Distance(transform.position, defaultPosition) > 0.1f) { transform.position = Vector2.MoveTowards(transform.position, defaultPosition, baseMoveSpeed * 5f * Time.deltaTime); yield return null; } rb.bodyType = RigidbodyType2D.Dynamic; isPhase2 = true; currentMoveSpeed = baseMoveSpeed * 1.75f; attackCooldown *= 0.6f; yield return new WaitForSeconds(1f); isResettingToCenter = false; }
}