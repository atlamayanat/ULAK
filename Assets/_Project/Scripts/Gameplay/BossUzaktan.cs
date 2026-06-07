using UnityEngine;
using Ulak.Core;

public class BossProjectile : MonoBehaviour
{
    [Header("Mermi Ayarlarý")]
    public float speed = 9f;
    public int damage = 10;
    public float knockbackForce = 12f;
    public LayerMask engelKatmanlari;

    private Rigidbody2D rb;
    private bool isReflected = false; // Geri yansýtýldý mý kontrolü

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnTriggerEnter2D(Collider2D hit)
    {
        // 1. DURUM: Oyuncu kýlýcýyla mermiyi tam zamanýnda vurduysa (Geri Yansýtma)
        // Kýlýç vuruþ kutunun (Trigger) tagý "PlayerAttack" olmalý
        if (hit.CompareTag("PlayerAttack") && !isReflected)
        {
            Reflect();
            return;
        }

        // 2. DURUM: Normal mermi Oyuncuya çarptýysa
        if (hit.CompareTag("Player") && !isReflected)
        {
            var dmg = hit.GetComponent<IDamageable>();
            if (dmg != null)
            {
                float dirX = Mathf.Sign(hit.transform.position.x - transform.position.x);
                Vector2 kb = new Vector2(pushDirX(), 0.5f).normalized * knockbackForce;
                dmg.TakeDamage(damage, kb);
            }
            Destroy(gameObject);
        }
        // 3. DURUM: Yansýtýlan mermi gidip Boss'a çarptýysa
        else if (hit.CompareTag("Boss") && isReflected)
        {
            var dmg = hit.GetComponent<IDamageable>();
            if (dmg != null)
            {
                // Yansýtýlan mermi Boss'a kendi hasarýnýn 2 katýný vursun!
                Vector2 kb = rb.linearVelocity.normalized * knockbackForce;
                dmg.TakeDamage(damage * 2, kb);
            }
            Destroy(gameObject);
        }
        // 4. DURUM: Zemin veya Duvar katmanýna çarptýysa yok ol
        else if ((engelKatmanlari.value & (1 << hit.gameObject.layer)) > 0)
        {
            Destroy(gameObject);
        }
    }

    // Mermiyi tersine çeviren sihirli metod
    public void Reflect()
    {
        isReflected = true;

        // Hýz vektörünü tam tersine çevir ve %50 daha hýzlý gönder (Ödüllendirme hissi)
        if (rb != null)
        {
            rb.linearVelocity = -rb.linearVelocity * 1.5f;
        }

        // Görsel cila: Merminin rengini mavi/cyan yapalým ki yansýdýðý belli olsun
        SpriteRenderer spriteComp = GetComponent<SpriteRenderer>();
        if (spriteComp != null)
        {
            spriteComp.color = Color.cyan;
        }
    }

    private float pushDirX()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) return Mathf.Sign(p.transform.position.x - transform.position.x);
        return 1f;
    }
}