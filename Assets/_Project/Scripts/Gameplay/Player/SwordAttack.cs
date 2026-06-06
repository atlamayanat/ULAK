using UnityEngine;
using UnityEngine.InputSystem;
using Ulak.Core;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Kılıç saldırısı (greybox v1):
    ///  - Girdi: sağ tık (plan kararı) / gamepad batı.
    ///  - Bakılan yönde kutu hitbox açar; menzildeki IDamageable'lara hasar + knockback.
    ///  - Cooldown ile spam engellenir (ritim hissi).
    ///
    /// Hasar anlık (OverlapBox) hesaplanır; ayrı bir hitbox collider'ı gerekmez.
    /// Yön, PlayerController.FacingX'ten okunur (yoksa sağ varsayılır).
    /// </summary>
    public class SwordAttack : MonoBehaviour
    {
        [Header("Saldırı")]
        [SerializeField] private int damage = 1;
        [SerializeField] private float cooldown = 0.45f;

        [Header("Hitbox (yerel uzayda, +X = bakılan yön)")]
        [Tooltip("Hitbox merkezinin oyuncuya göre ofseti (X, bakılan yöne çevrilir).")]
        [SerializeField] private Vector2 hitboxOffset = new Vector2(0.9f, 0f);
        [SerializeField] private Vector2 hitboxSize = new Vector2(1.6f, 1.3f);
        [Tooltip("Vurulabilen layer'lar (düşmanlar / kırılabilir engeller).")]
        [SerializeField] private LayerMask targetLayers = ~0;

        [Header("Geri savurma")]
        [SerializeField] private float knockbackForce = 18f;
        [Tooltip("Knockback'in yukarı bileşeni (0 = düz yatay, 1 = 45°).")]
        [SerializeField, Range(0f, 1f)] private float knockbackUp = 0.45f;

        [Header("Görsel telgraf (opsiyonel)")]
        [Tooltip("Saldırı anında kısa süre açılacak görsel (ör. kılıç çizgisi). Boş bırakılabilir.")]
        [SerializeField] private GameObject slashVisual;
        [SerializeField] private float slashVisualTime = 0.1f;

        private float _nextReadyTime;
        private PlayerController _pc;
        // OverlapBox alloc'suz tarama için tampon.
        private readonly Collider2D[] _hits = new Collider2D[8];

        private void Awake()
        {
            _pc = GetComponent<PlayerController>();

            // Kendi kendine bağlanma: maske "her şey" kalmışsa Enemy layer'a daralt.
            if (targetLayers.value == -1 || targetLayers.value == 0)
            {
                int e = LayerMask.NameToLayer("Enemy");
                if (e >= 0) targetLayers = 1 << e;
            }
        }

        private void Update()
        {
            if (AttackPressedThisFrame() && Time.time >= _nextReadyTime)
                DoAttack();
        }

        private float Facing => _pc != null ? _pc.FacingX : 1f;

        private void DoAttack()
        {
            _nextReadyTime = Time.time + cooldown;

            float face = Facing;
            Vector2 center = (Vector2)transform.position
                             + new Vector2(hitboxOffset.x * face, hitboxOffset.y);
            var filter = new ContactFilter2D { useLayerMask = true, useTriggers = true };
            filter.SetLayerMask(targetLayers);

            int count = Physics2D.OverlapBox(center, hitboxSize, 0f, filter, _hits);
            for (int i = 0; i < count; i++)
            {
                Collider2D col = _hits[i];
                if (col == null || col.transform == transform) continue;

                var dmg = col.GetComponentInParent<IDamageable>();
                if (dmg == null || !dmg.IsAlive) continue;

                // Knockback yönü: hedefe doğru yatay + biraz yukarı.
                float dirX = Mathf.Sign(col.bounds.center.x - transform.position.x);
                if (dirX == 0) dirX = face;
                Vector2 kb = new Vector2(dirX, knockbackUp).normalized * knockbackForce;

                dmg.TakeDamage(damage, kb);
            }

            if (slashVisual != null)
            {
                // Savurma efektini bakılan yöne konumlandır ve aynala.
                slashVisual.transform.localPosition =
                    new Vector3(hitboxOffset.x * face, hitboxOffset.y, 0f);
                var vsr = slashVisual.GetComponent<SpriteRenderer>();
                if (vsr != null) vsr.flipX = face < 0;

                StopAllCoroutines(); // üst üste saldırıda eski kapanışı iptal et
                StartCoroutine(ShowSlash());
            }
        }

        private System.Collections.IEnumerator ShowSlash()
        {
            slashVisual.SetActive(true);
            yield return new WaitForSeconds(slashVisualTime);
            slashVisual.SetActive(false);
        }

        private static bool AttackPressedThisFrame()
        {
            bool mouse = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
            bool pad = Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame;
            return mouse || pad;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
            float face = Application.isPlaying ? Facing : 1f;
            Vector2 center = (Vector2)transform.position
                             + new Vector2(hitboxOffset.x * face, hitboxOffset.y);
            Gizmos.DrawWireCube(center, hitboxSize);
        }
    }
}
