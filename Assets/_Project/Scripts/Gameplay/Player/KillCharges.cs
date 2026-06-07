using UnityEngine;
using UnityEngine.InputSystem;
using Ulak.Core;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Can basma mekaniği (greybox v3):
    ///  - Canavar kesiminde VE boss'a vuruşta 1 yük kazanılır
    ///    (bkz. EnemyDeath → Add, BossController.TakeDamage → Add).
    ///  - Yükler sol üst köşedeki sayaçta gösterilir (en çok 10).
    ///  - H tuşu MEVCUT TÜM yükleri tüketir; yük başına canın %10'u dolar
    ///    (örn. 3 yük → %30). 10 sn bekleme süresi var.
    /// UI, greybox sadeliği için OnGUI ile çizilir — Canvas gerekmez.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class KillCharges : MonoBehaviour
    {
        [Tooltip("Biriktirilebilecek en fazla yük.")]
        [SerializeField] private int maxCharges = 10;
        [Tooltip("Harcanan yük başına dolan can oranı (0.10 = %10).")]
        [SerializeField] private float healYuzdesi = 0.10f;
        [Tooltip("İki H basışı arasındaki bekleme süresi (sn).")]
        [SerializeField] private float healBeklemesi = 10f;

        private Health _health;
        private float _sonrakiHeal;
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
            if (Time.time < _sonrakiHeal) return;           // bekleme süresi dolmadı
            if (Current <= 0) return;                       // hiç yük yok
            if (_health.Current >= _health.Max) return;     // can zaten dolu, harcama
            int harcanan = Current;
            Current = 0;                                    // TÜM yükler tüketilir
            int miktar = Mathf.Max(1, Mathf.RoundToInt(_health.Max * healYuzdesi * harcanan));
            _health.Heal(miktar);
            _sonrakiHeal = Time.time + healBeklemesi;
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

            // İpucu: beklemede kalan süre ya da kullanım çağrısı
            float kalan = _sonrakiHeal - Time.time;
            if (kalan > 0f)
                GUI.Label(new Rect(18, pos.y + pos.height + 2, 360, 30),
                    $"H beklemede: {Mathf.CeilToInt(kalan)} sn", _hintStyle);
            else if (Current > 0 && _health.Current < _health.Max)
                GUI.Label(new Rect(18, pos.y + pos.height + 2, 360, 30),
                    $"H → %{Mathf.RoundToInt(healYuzdesi * 100f * Current)} can ({Current} yük)", _hintStyle);
        }
    }
}
