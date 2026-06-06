using UnityEngine;
using Ulak.Core; // Senin hasar sistemini tanýmasý için

public class BossProjectile : MonoBehaviour
{
    [Header("Mermi Ayarlarý")]
    public int damage = 10;
    public float knockbackForce = 15f;

    // Mermi bir þeye çarptýðýnda otomatik tetiklenir
    void OnTriggerEnter2D(Collider2D hit)
    {
        // 1. Çarptýðý þey Player ise
        if (hit.CompareTag("Player"))
        {
            // Oyuncudaki hasar sistemini bul
            var dmg = hit.GetComponent<IDamageable>();
            if (dmg != null)
            {
                // Hasar ve geri savurma (Knockback) uygula
                float dirX = Mathf.Sign(hit.transform.position.x - transform.position.x);
                Vector2 kb = new Vector2(dirX, 0.5f).normalized * knockbackForce;

                dmg.TakeDamage(damage, kb);
                Debug.Log("Mermi oyuncuya çarptý ve hasar verdi!");
            }

            // Çarptýktan sonra mermiyi yok et
            Destroy(gameObject);
        }
        // 2. Çarptýðý þey Zemin ise (Duvarlardan geçip gitmesin)
        else if (hit.gameObject.layer == LayerMask.NameToLayer("Zemin"))
        {
            Destroy(gameObject);
        }
    }
}