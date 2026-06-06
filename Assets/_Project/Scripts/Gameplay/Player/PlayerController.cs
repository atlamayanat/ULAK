using UnityEngine;
using UnityEngine.InputSystem;
using Ulak.Core;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Oyuncu hareketi (greybox v1):
    ///  - Yatay hareket: A/D, sol/sağ ok veya gamepad sol çubuk.
    ///  - Zıplama: Space / W / yukarı ok / gamepad güney. Zemindeyken (coyote time'lı).
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

        [Header("Kafa Sıçraması")]
        [Tooltip("Canavar kafasından zıplarken canavara verilen hasar.")]
        [SerializeField] private int stompDamage = 1;
        [Tooltip("Canavar kafasından zıplamanın normal zıplamaya göre çarpanı.")]
        [SerializeField] private float stompJumpMultiplier = 1.35f;

        [Header("Dash")]
        [Tooltip("Atılma hızı (bakılan yöne).")]
        [SerializeField] private float dashSpeed = 16f;
        [Tooltip("Atılmanın sürdüğü süre (sn).")]
        [SerializeField] private float dashDuration = 0.14f;
        [Tooltip("İki atılma arası bekleme (sn).")]
        [SerializeField] private float dashCooldown = 1f;

        private Rigidbody2D _rb;
        private Knockback _knockback;
        private SpriteFlipbook _flipbook;
        private bool _isGrounded;
        private Collider2D _groundCollider; // üstünde durulan şey (zemin ya da canavar)
        private float _lastGroundedTime;
        private bool _jumpQueued;
        private bool _jumpedSinceGrounded;
        private bool _dashQueued;
        private float _dashUntil;
        private float _nextDashTime;
        private float _moveInput;

        public bool IsGrounded => _isGrounded;

        /// <summary>Son bakılan yön (1 = sağ, -1 = sol). Saldırı yönü için kullanılır.</summary>
        public int FacingX { get; private set; } = 1;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _knockback = GetComponent<Knockback>();
            _flipbook = GetComponent<SpriteFlipbook>();

            // --- Kendi kendine bağlanma (editor wiring'i boş kalsa bile çalış) ---
            if (groundCheck == null)
            {
                var t = transform.Find("GroundCheck");
                if (t != null) groundCheck = t;
            }
            // "Her şey" maskesi kendi collider'ımızı zemin sayar → sınırsız zıplama.
            // Ground + Enemy'ye daralt: düşman kafası da zıplanabilir zemin —
            // canavar üstüne düşünce sıkışmak yerine zıplayıp kurtulursun.
            if (groundLayer.value == -1 || groundLayer.value == 0)
            {
                int g = LayerMask.NameToLayer("Ground");
                int e = LayerMask.NameToLayer("Enemy");
                int mask = 0;
                if (g >= 0) mask |= 1 << g;
                if (e >= 0) mask |= 1 << e;
                if (mask != 0) groundLayer = mask;
            }
        }

        private void Update()
        {
            // Girdileri Update'te yakala (kaçırmamak için), FixedUpdate'te uygula.
            _moveInput = ReadMoveInput();
            if (_moveInput > 0.01f) FacingX = 1;
            else if (_moveInput < -0.01f) FacingX = -1;
            _flipbook?.SetFacing(FacingX);
            _flipbook?.SetMoving(Mathf.Abs(_moveInput) > 0.01f);

            if (JumpPressedThisFrame())
                _jumpQueued = true;

            if (DashPressedThisFrame())
                _dashQueued = true;
        }

        private void FixedUpdate()
        {
            UpdateGrounded();

            bool locked = _knockback != null && _knockback.IsBeingKnockedBack;

            // --- Dash: bakılan yöne kısa ve hızlı atılma (cooldown'lu) ---
            bool dashing = Time.time < _dashUntil;
            if (_dashQueued && !dashing && !locked && Time.time >= _nextDashTime)
            {
                _dashUntil = Time.time + dashDuration;
                _nextDashTime = Time.time + dashCooldown;
                dashing = true;
            }
            _dashQueued = false;

            if (dashing && !locked)
            {
                // Düz çizgide atıl: dikey hız sıfır (havada da çalışır).
                _rb.linearVelocity = new Vector2(FacingX * dashSpeed, 0f);
                _jumpQueued = false;
                return;
            }

            if (!locked)
            {
                // Yatay hareket: girdi yönünde sabit hız, dikey hızı koru.
                _rb.linearVelocity = new Vector2(_moveInput * runSpeed, _rb.linearVelocity.y);
            }

            // Zıplama: yerden (coyote payıyla) VE bu yere inişten beri zıplanmadıysa.
            bool canJump = Time.time - _lastGroundedTime <= coyoteTime && !_jumpedSinceGrounded;
            if (_jumpQueued && canJump && !locked)
            {
                float force = jumpForce;

                // KAFA SIÇRAMASI: canavar üstünden zıplıyorsak hasar ver + daha yükseğe fırla.
                if (_groundCollider != null
                    && _groundCollider.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                {
                    var dmg = _groundCollider.GetComponentInParent<IDamageable>();
                    if (dmg != null && dmg.IsAlive)
                        dmg.TakeDamage(stompDamage, Vector2.down * 3f); // ezme hissi
                    force *= stompJumpMultiplier;
                }

                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
                _rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
                _jumpedSinceGrounded = true; // yere değene kadar tekrar zıplama yok
            }
            _jumpQueued = false;
        }

        private void UpdateGrounded()
        {
            Vector2 point = groundCheck != null
                ? (Vector2)groundCheck.position
                : (Vector2)transform.position + Vector2.down * 0.5f;

            _groundCollider = Physics2D.OverlapCircle(point, groundCheckRadius, groundLayer);
            _isGrounded = _groundCollider != null;
            if (_isGrounded)
            {
                _lastGroundedTime = Time.time;
                // Yükselirken (zıplamanın hemen başında) hâlâ zemine yakınız —
                // bayrağı ancak düşerken/dururken sıfırla ki spam çift zıplatmasın.
                if (_rb.linearVelocity.y <= 0.01f)
                    _jumpedSinceGrounded = false;
            }
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

        private static bool DashPressedThisFrame()
        {
            bool mouse = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
            bool pad = Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame;
            return mouse || pad;
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
