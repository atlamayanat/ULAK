using UnityEngine;
using UnityEngine.InputSystem;
using Ulak.Core;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Oyuncu hareketi (greybox v1):
    ///  - Yatay hareket: A/D, sol/sağ ok veya gamepad sol çubuk.
    ///  - Zıplama: Space / gamepad güney. Zemindeyken (coyote time'lı).
    ///  - Duvarlara çarpma: fizik motoru halleder (Rigidbody2D + Collider).
    ///  - Knockback sırasında kontrol bırakılır (savrulma okunabilir kalsın).
    ///
    /// Not: v1'de girdi doğrudan cihazdan okunuyor (Inspector wiring gerekmez).
    /// İleride InputRouter + Action Map'e taşınacak (bkz. ULAK_PLAN.md §6).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Hareket")]
        [SerializeField] private float runSpeed = 5f;

        [Header("Zıplama")]
        [SerializeField] private float jumpForce = 12f;
        [Tooltip("Zemin sayılan layer(lar).")]
        [SerializeField] private LayerMask groundLayer = ~0;
        [Tooltip("Ayak hizasındaki zemin kontrol noktası (boş bırakılırsa collider altı kullanılır).")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.15f;
        [Tooltip("Yere değdikten sonra zıplamaya izin verilen tampon süre (coyote time).")]
        [SerializeField] private float coyoteTime = 0.1f;

        private Rigidbody2D _rb;
        private Knockback _knockback;
        private bool _isGrounded;
        private float _lastGroundedTime;
        private bool _jumpQueued;
        private float _moveInput;

        public bool IsGrounded => _isGrounded;

        /// <summary>Son bakılan yön (1 = sağ, -1 = sol). Saldırı yönü için kullanılır.</summary>
        public int FacingX { get; private set; } = 1;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _knockback = GetComponent<Knockback>();
        }

        private void Update()
        {
            // Girdileri Update'te yakala (kaçırmamak için), FixedUpdate'te uygula.
            _moveInput = ReadMoveInput();
            if (_moveInput > 0.01f) FacingX = 1;
            else if (_moveInput < -0.01f) FacingX = -1;

            if (JumpPressedThisFrame())
                _jumpQueued = true;
        }

        private void FixedUpdate()
        {
            UpdateGrounded();

            bool locked = _knockback != null && _knockback.IsBeingKnockedBack;

            if (!locked)
            {
                // Yatay hareket: girdi yönünde sabit hız, dikey hızı koru.
                _rb.linearVelocity = new Vector2(_moveInput * runSpeed, _rb.linearVelocity.y);
            }

            bool canJump = Time.time - _lastGroundedTime <= coyoteTime;
            if (_jumpQueued && canJump && !locked)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
                _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                _lastGroundedTime = -999f; // çift zıplamayı engelle
            }
            _jumpQueued = false;
        }

        private void UpdateGrounded()
        {
            Vector2 point = groundCheck != null
                ? (Vector2)groundCheck.position
                : (Vector2)transform.position + Vector2.down * 0.5f;

            _isGrounded = Physics2D.OverlapCircle(point, groundCheckRadius, groundLayer);
            if (_isGrounded)
                _lastGroundedTime = Time.time;
        }

        private static bool JumpPressedThisFrame()
        {
            bool kb = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            bool pad = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
            return kb || pad;
        }

        private static float ReadMoveInput()
        {
            float v = 0f;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v += 1f;
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                float x = pad.leftStick.x.ReadValue();
                if (Mathf.Abs(x) > 0.2f) v += x; // ölü bölge
            }

            return Mathf.Clamp(v, -1f, 1f);
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 point = groundCheck != null
                ? (Vector2)groundCheck.position
                : (Vector2)transform.position + Vector2.down * 0.5f;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(point, groundCheckRadius);
        }
    }
}
