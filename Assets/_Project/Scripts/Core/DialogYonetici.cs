using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ulak.Core
{
    /// <summary>
    /// Diyalog akışını yürüten tekil yönetici (greybox v1).
    ///  - Konuşma balonu: koyu KARE çerçeve + beyaz metin (balon görseli gelince
    ///    sadece arka plan sprite'ı değişecek).
    ///  - Balon, o an konuşan karakterin kafasının üstünde belirir.
    ///  - Oyuncunun her SOL TIK'ı bir sonraki repliğe geçirir; bitince kapanır.
    ///  - Diyalog boyunca oyuncu kontrolü kilitlenir (yönetici neyi kapattıysa
    ///    onu geri açar — cutscene'lerin kendi kilitleriyle çakışmaz).
    /// </summary>
    public class DialogYonetici : MonoBehaviour
    {
        public static DialogYonetici Aktif { get; private set; }
        public static bool DialogSuruyor => Aktif != null && Aktif._sira >= 0;

        // ---- balon görselleri ----
        private GameObject _balon;
        private SpriteRenderer _kare;
        private TextMesh _yazi;

        // ---- akış ----
        private List<Replik> _replikler;
        private System.Func<string, Transform> _konusanBul;
        private System.Action _bitince;
        private int _sira = -1;
        private float _acilisZamani;

        // diyalog için bizim kapattıklarımız
        private readonly List<Behaviour> _kapatilanlar = new List<Behaviour>();

        /// <summary>Diyalog başlatır. konusanBul: isim → sahnedeki Transform.</summary>
        public static void Baslat(List<Replik> replikler,
            System.Func<string, Transform> konusanBul, System.Action bitince = null)
        {
            if (replikler == null || replikler.Count == 0) { bitince?.Invoke(); return; }

            if (Aktif == null)
            {
                var go = new GameObject("DialogYonetici");
                Aktif = go.AddComponent<DialogYonetici>();
                Aktif.BalonKur();
            }
            Aktif.IcBaslat(replikler, konusanBul, bitince);
        }

        private void IcBaslat(List<Replik> replikler,
            System.Func<string, Transform> konusanBul, System.Action bitince)
        {
            _replikler = replikler;
            _konusanBul = konusanBul;
            _bitince = bitince;
            _sira = 0;
            _acilisZamani = Time.time;

            OyuncuyuKilitle();
            ReplikGoster();
        }

        private void Update()
        {
            if (_sira < 0) return;
            if (Time.time - _acilisZamani < 0.25f) return; // açılış tıkını yutma
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

            _sira++;
            if (_sira >= _replikler.Count) Bitir();
            else ReplikGoster();
        }

        private void LateUpdate()
        {
            // Balon, konuşanı izlesin (konuşan hareket ederse üstünde kalsın).
            if (_sira < 0 || _sira >= _replikler.Count) return;
            var sahibi = _konusanBul?.Invoke(_replikler[_sira].Konusan);
            if (sahibi != null)
                _balon.transform.position = sahibi.position + Vector3.up * 1.9f;
        }

        private void ReplikGoster()
        {
            var r = _replikler[_sira];
            string metin = r.Konusan + ":\n" + Sar(r.Metin, 30);
            _yazi.text = metin;

            // Kareyi metne göre boyutla.
            int satirSayisi = metin.Split('\n').Length;
            float h = 0.35f * satirSayisi + 0.3f;
            _kare.transform.localScale = new Vector3(4.6f, h, 1f);

            _balon.SetActive(true);
        }

        private void Bitir()
        {
            _sira = -1;
            _balon.SetActive(false);
            OyuncuyuSerbestBirak();
            var cb = _bitince;
            _bitince = null;
            cb?.Invoke();
        }

        // ---- balon inşası ----
        private void BalonKur()
        {
            _balon = new GameObject("KonusmaBalonu");
            _balon.transform.SetParent(transform);

            var kareGo = new GameObject("Kare");
            kareGo.transform.SetParent(_balon.transform, false);
            _kare = kareGo.AddComponent<SpriteRenderer>();
            _kare.sprite = BeyazKare();
            _kare.color = new Color(0.07f, 0.07f, 0.12f, 0.93f);
            _kare.sortingOrder = 200;

            var yaziGo = new GameObject("Yazi");
            yaziGo.transform.SetParent(_balon.transform, false);
            yaziGo.transform.localPosition = new Vector3(0f, 0f, 0f);
            _yazi = yaziGo.AddComponent<TextMesh>();
            _yazi.anchor = TextAnchor.MiddleCenter;
            _yazi.alignment = TextAlignment.Center;
            _yazi.characterSize = 0.07f;
            _yazi.fontSize = 48;
            _yazi.color = Color.white;
            yaziGo.GetComponent<MeshRenderer>().sortingOrder = 201;

            _balon.SetActive(false);
        }

        private static Sprite _beyaz;
        private static Sprite BeyazKare()
        {
            if (_beyaz != null) return _beyaz;
            var tex = new Texture2D(4, 4);
            var px = new Color32[16];
            for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px); tex.Apply();
            _beyaz = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return _beyaz;
        }

        /// <summary>Kelime sınırından satır sarma (TextMesh sarmayı bilmez).</summary>
        private static string Sar(string s, int genislik)
        {
            var sb = new System.Text.StringBuilder();
            int son = 0;
            foreach (var kelime in s.Split(' '))
            {
                if (son + kelime.Length + 1 > genislik) { sb.Append('\n'); son = 0; }
                else if (sb.Length > 0) { sb.Append(' '); son++; }
                sb.Append(kelime);
                son += kelime.Length;
            }
            return sb.ToString();
        }

        // ---- oyuncu kilidi ----
        private void OyuncuyuKilitle()
        {
            _kapatilanlar.Clear();
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;

            foreach (var b in p.GetComponents<Behaviour>())
            {
                string t = b.GetType().Name;
                if (t != "PlayerController" && t != "HorseController" && t != "SwordAttack") continue;
                if (!b.enabled) continue; // başkası kapatmış — ona dokunma
                b.enabled = false;
                _kapatilanlar.Add(b);
            }
            var rb = p.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        private void OyuncuyuSerbestBirak()
        {
            StartCoroutine(GecikmeliAc());
        }

        private System.Collections.IEnumerator GecikmeliAc()
        {
            // kapanış tıklaması saldırı tetiklemesin
            yield return null;
            yield return null;
            foreach (var b in _kapatilanlar)
                if (b != null) b.enabled = true;
            _kapatilanlar.Clear();
        }
    }
}
