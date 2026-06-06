using UnityEngine;
using Ulak.Core;

public class BossProjectile : MonoBehaviour
{
    [Header("Mermi Ayarlarý")]
    public int damage = 10;
    public float knockbackForce = 15f;

    [Header("Çarpýþma Ayarlarý")]
    [Tooltip("Merminin çarpýp yok olacaðý katmanlar (Örn: Zemin, Duvar)")]
    public LayerMask engelKatmanlari;

    void OnTriggerEnter2D(Collider2D hit)
    {
        // 1. Çarptýðý þey Player ise
        if (hit.CompareTag("Player"))
        {
            var dmg = hit.GetComponent<IDamageable>();
            if (dmg != null)
            {
                // Hasar ve geri savurma (Knockback) uygula
                float dirX = Mathf.Sign(hit.transform.position.x - transform.position.x);
                Vector2 kb = new Vector2(dirX, 0.5f).normalized * knockbackForce;

                dmg.TakeDamage(damage, kb);
            }

            // Oyuncuya çarptýktan sonra mermiyi yok et
            Destroy(gameObject);
        }
        // 2. Çarptýðý þey seçtiðimiz Engel Katmanlarýndan (Zemin, Duvar vs.) biri ise
        else if ((engelKatmanlari.value & (1 << hit.gameObject.layer)) > 0)
        {
            // Duvara veya zemine çarptýðý an mermiyi yok et
            Destroy(gameObject);
        }
    }
}