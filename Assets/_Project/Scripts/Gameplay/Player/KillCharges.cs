using UnityEngine;
using UnityEngine.InputSystem;
using Ulak.Core;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Can basma mekaniği (greybox v1):
    ///  - Her canavar kesiminde 1 yük kazanılır (bkz. EnemyDeath → Add).
    ///  - Yükler sol üst köşedeki sayaçta gösterilir.
    ///  - 3 yüke ulaşınca H tuşu canı tamamen yeniler ve yükleri harcar.
    /// UI, greybox sadeliği için OnGUI ile çizilir — Canvas gerekmez.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class KillCharges : MonoBehaviour
    {
        [Tooltip("Biriktirilebilecek en fazla yük.")]
        [SerializeField] private int maxCharges = 3;
        [Tooltip("Bir can yenilemenin yük bedeli.")]
        [SerializeField] private int healCost = 3;

        private Health _health;
        private GUIStyle _counterStyle;
        private GUIStyle _hintStyle;

        /// <summary>Mevcut yük sayısı.</summary>
        public int Current { get; private set; }

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        /// <summary>Yük ekler (canavar kesimi). Üst sınırda kalanlar yanar.</summary>
        public void Add(int amount = 1)
        {
            Current = Mathf.Min(maxCharges, Current + amount);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.hKey.wasPressedThisFrame)
                TryHeal();
        }

        private void TryHeal()
        {
            if (Current < healCost) return;                 // yeterli yük yok
            if (_health.Current >= _health.Max) return;     // can zaten dolu, harcama
            Current -= healCost;
            _health.ResetHealth();
        }

        private void OnGUI()
        {
            if (_counterStyle == null)
            {
                _counterStyle = new GUIStyle
                {
                    fontSize = Mathf.Max(20, Screen.height / 24),
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
                _hintStyle = new GUIStyle
                {
                    fontSize = Mathf.Max(14, Screen.height / 40),
                    normal = { textColor = new Color(1f, 0.9f, 0.3f) }
                };
            }

            // Gölge + metin (okunabilirlik için)
            string text = $"Yük: {Current}/{maxCharges}";
            var pos = new Rect(18, 14, 320, 44);
            var shadow = new Rect(pos.x + 2, pos.y + 2, pos.width, pos.height);
            var old = _counterStyle.normal.textColor;
            _counterStyle.normal.textColor = Color.black;
            GUI.Label(shadow, text, _counterStyle);
            _counterStyle.normal.textColor = old;
            GUI.Label(pos, text, _counterStyle);

            // Yük hazırsa ipucu göster
            if (Current >= healCost && _health.Current < _health.Max)
                GUI.Label(new Rect(18, pos.y + pos.height + 2, 360, 30),
                    "H → Can yenile", _hintStyle);
        }
    }
}
