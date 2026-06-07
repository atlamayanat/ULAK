using UnityEngine;
using Ulak.Core;

namespace Ulak.Gameplay
{
    // --- DİĞER KODLARIN ÇÖKMESİNİ ENGELLEYEN PUAN SINIFI BURADA ---
    public static class RideScore
    {
        public static int Score { get; private set; }
        public static void Add(int points) => Score += points;
        public static void Reset() => Score = 0;
    }

    /// <summary>
    /// Altın kutu yerine kullanılan Pastırma objesi:
    ///  - Kılıç saldırısı isabet ederse parçalanır ve atın hızını artırır.
    /// </summary>
    public class GoldBox : MonoBehaviour, IDamageable
    {
        [Tooltip("Toplanınca atın hızına eklenecek miktar.")]
        [SerializeField] private float speedBoost = 1.0f;

        private bool _collected;

        public bool IsAlive => !_collected;

        public void TakeDamage(int amount, Vector2 knockback)
        {
            if (_collected) return;
            _collected = true;

            // Sahnedeki atı bul
            HorseController horse = Object.FindFirstObjectByType<HorseController>();

            if (horse != null)
            {
                // Atın hızını artır
                horse.ApplySpeedBoost(speedBoost);
            }

            // NOT: Eğer pastırma alınca "Hızın yanında 10 Puan da versin" dersen
            // aşağıdaki satırın başındaki yorum (//) işaretini silmen yeterli!
            // RideScore.Add(10);

            // Objeyi yok et
            Destroy(gameObject);
        }
    }
}