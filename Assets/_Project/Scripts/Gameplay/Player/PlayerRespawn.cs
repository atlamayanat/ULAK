using UnityEngine;
using Ulak.Core;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Greybox v1 ölüm tepkisi: oyuncu ölünce başlangıç noktasına döner ve canı dolar.
    /// İleride GameManager checkpoint sistemiyle değişecek (bkz. ULAK_PLAN.md §3.4).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerRespawn : MonoBehaviour
    {
        [SerializeField] private Transform spawnPoint;

        private Health _health;
        private Rigidbody2D _rb;
        private Vector3 _startPos;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _rb = GetComponent<Rigidbody2D>();
            _startPos = transform.position;
            _health.OnDeath.AddListener(Respawn);
        }

        private void Respawn()
        {
            transform.position = spawnPoint != null ? spawnPoint.position : _startPos;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            _health.ResetHealth();
        }
    }
}
