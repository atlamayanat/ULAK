using UnityEngine;
using Ulak.Core;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Düşman öldüğünde greybox tepkisi: GameObject'i yok eder.
    /// Health.OnDeath olayına bağlanır (Inspector'dan ya da Awake'te otomatik).
    /// Gerçek ölüm efekti/animasyonu gelince burası genişler.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class EnemyDeath : MonoBehaviour
    {
        [SerializeField] private float destroyDelay = 0f;

        private void Awake()
        {
            GetComponent<Health>().OnDeath.AddListener(HandleDeath);
        }

        private void HandleDeath()
        {
            // Çarpışmayı hemen kapat ki ölürken son bir temas hasarı vermesin.
            foreach (var col in GetComponents<Collider2D>())
                col.enabled = false;

            Destroy(gameObject, destroyDelay);
        }
    }
}
