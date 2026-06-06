using UnityEngine;
using Ulak.Core;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Gündüz modu koşu puanı. Sahne başında sıfırlanır (HorseController.Awake).
    /// </summary>
    public static class RideScore
    {
        public static int Score { get; private set; }
        public static void Add(int points) => Score += points;
        public static void Reset() => Score = 0;
    }

    /// <summary>
    /// Altın kutu (greybox v1):
    ///  - Trigger collider: at çarpsa da etkilenmez, içinden geçer.
    ///  - Kılıç saldırısı isabet ederse parçalanır ve puan kazandırır.
    ///  - SwordAttack, IDamageable üzerinden vurur (Enemy layer'da olmalı).
    /// </summary>
    public class GoldBox : MonoBehaviour, IDamageable
    {
        [Tooltip("Kırılınca kazanılan puan.")]
        [SerializeField] private int points = 10;

        private bool _broken;

        public bool IsAlive => !_broken;

        public void TakeDamage(int amount, Vector2 knockback)
        {
            if (_broken) return;
            _broken = true;
            RideScore.Add(points);
            Destroy(gameObject);
        }
    }
}
