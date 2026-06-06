using UnityEngine;
using Ulak.Core;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Düşman öldüğünde greybox tepkisi: renkleri yavaşça solarak
    /// şeffaflaşır ve yok olur. Health.OnDeath olayına bağlanır.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class EnemyDeath : MonoBehaviour
    {
        [Tooltip("Solarak kaybolma süresi (sn).")]
        [SerializeField] private float fadeDuration = 0.8f;

        private void Awake()
        {
            GetComponent<Health>().OnDeath.AddListener(HandleDeath);
        }

        private void HandleDeath()
        {
            // Çarpışmayı hemen kapat ki ölürken son bir temas hasarı vermesin.
            foreach (var col in GetComponents<Collider2D>())
                col.enabled = false;

            // Can basma mekaniği: kesimi yapan oyuncuya 1 yük ver.
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                player.GetComponent<KillCharges>()?.Add(1);

            // Hasar flaşı solmayla çakışmasın.
            var flash = GetComponent<DamageFlash>();
            if (flash != null)
            {
                flash.StopAllCoroutines();
                flash.enabled = false;
            }

            StartCoroutine(FadeOutAndDestroy());
        }

        private System.Collections.IEnumerator FadeOutAndDestroy()
        {
            // Gövde + can barı dahil tüm sprite'ları birlikte solddur.
            var renderers = GetComponentsInChildren<SpriteRenderer>();
            var startColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                startColors[i] = renderers[i].color;

            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(1f - t / fadeDuration);
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null) continue;
                    var c = startColors[i];
                    c.a *= a;
                    renderers[i].color = c;
                }
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
