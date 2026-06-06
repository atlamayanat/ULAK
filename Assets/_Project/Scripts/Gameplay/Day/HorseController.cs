using UnityEngine;
using UnityEngine.InputSystem;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Gündüz modu at koşusu (greybox v1):
    ///  - Oto-koşu: normal hızda başlar, koştukça giderek hızlanır (maks. hıza dek).
    ///  - Engele çarpınca at tökezler: hız ciddi düşer, baştan hızlanmak gerekir.
    ///  - Zıplama: Space / W / yukarı ok / gamepad güney (engellerden kaçmak için).
    ///  - Kılıç saldırısı SwordAttack üzerinden çalışır (altın kutuları kırmak için).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class HorseController : MonoBehaviour
    {
        [Header("Koşu")]
        [Tooltip("Başlangıç (normal) hız.")]
        [SerializeField] private float baseSpeed = 5f;
        [Tooltip("Ulaşılabilecek en yüksek hız.")]
        [SerializeField] private float maxSpeed = 14f;
        [Tooltip("Saniye başına hız artışı.")]
        [SerializeField] private float acceleration = 0.9f;

        [Header("Tökezleme")]
        [Tooltip("Engele çarpınca hız = baseSpeed × bu çarpan (sonra yeniden hızlanır).")]
        [SerializeField, Range(0.1f, 1f)] private float stumbleFactor = 0.4f;

        [Header("Zıplama")]
        [SerializeField] private float jumpForce = 13f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.18f;
        [SerializeField] private float coyoteTime = 0.1f;

        private Rigidbody2D _rb;
        private float _speed;
        private bool _isGrounded;
        private float _lastGroundedTime;
        private bool _jumpQueued;
        private bool _jumpedSinceGrounded;

        /// <summary>Anlık koşu hızı (HUD gösterimi için).</summary>
        public float CurrentSpeed => _speed;
        public float MaxSpeed => maxSpeed;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _speed = baseSpeed;
            RideScore.Reset();

            // Kendi kendine bağlanma
            if (groundCheck == null)
            {
                var t = transform.Find("GroundCheck");
                if (t != null) groundCheck = t;
            }
            if (groundLayer.value == 0 || groundLayer.value == -1)
            {
                int g = LayerMask.NameToLayer("Ground");
                if (g >= 0) groundLayer = 1 << g;
            }
        }

        private void Update()
        {
            if (JumpPressedThisFrame())
                _jumpQueued = true;
        }

        private void FixedUpdate()
        {
            UpdateGrounded();

            // Koştukça hızlan.
            _speed = Mathf.Min(maxSpeed, _speed + acceleration * Time.fixedDeltaTime);
            _rb.linearVelocity = new Vector2(_speed, _rb.linearVelocity.y);

            bool canJump = Time.time - _lastGroundedTime <= coyoteTime && !_jumpedSinceGrounded;
            if (_jumpQueued && canJump)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
                _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                _jumpedSinceGrounded = true;
            }
            _jumpQueued = false;
        }

        private void UpdateGrounded()
        {
            Vector2 point = groundCheck != null
                ? (Vector2)groundCheck.position
                : (Vector2)transform.position + Vector2.down * 0.6f;

            _isGrounded = Physics2D.OverlapCircle(point, groundCheckRadius, groundLayer);
            if (_isGrounded)
            {
                _lastGroundedTime = Time.time;
                if (_rb.linearVelocity.y <= 0.01f)
                    _jumpedSinceGrounded = false;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Sadece zemin/engel katmanı tökezletir.
            if ((groundLayer.value & (1 << collision.gameObject.layer)) == 0) return;

            // Önden çarpma mı? (sağa koşarken yüzü bize dönük normal)
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.x < -0.5f)
                {
                    Stumble();
                    break;
                }
            }
        }

        /// <summary>At tökezledi: hız ciddi düşer, yeniden hızlanmak gerekir.</summary>
        private void Stumble()
        {
            _speed = baseSpeed * stumbleFactor;
        }

        private static bool JumpPressedThisFrame()
        {
            var kb = Keyboard.current;
            bool key = kb != null && (kb.spaceKey.wasPressedThisFrame
                                      || kb.upArrowKey.wasPressedThisFrame
                                      || kb.wKey.wasPressedThisFrame);
            bool pad = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
            return key || pad;
        }
    }
}
