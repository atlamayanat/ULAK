using UnityEngine;
using UnityEngine.InputSystem;
using Ulak.Core;

namespace Ulak.Gameplay
{
    /// <summary>
    /// NPC'nin kafasının üstünde süzülen YEŞİL KARE (konuşma balonu görseli
    /// gelince sprite'ı değişecek). Tıklanınca ilgili bölümün karşılıklı
    /// diyaloğu başlar; oyuncunun her tıklaması repliği ilerletir.
    /// BALAMIR replikleri oyuncunun, diğerleri bu NPC'nin üstünde çıkar.
    /// </summary>
    public class DialogTetikleyici : MonoBehaviour
    {
        [Tooltip("replikler.txt içindeki BÖLÜM numarası.")]
        [SerializeField] private int bolumNo = 2;
        [SerializeField] private TextAsset replikDosyasi;
        [Tooltip("Bölümün kaçıncı repliğinden başlanacak (0 = ilk).")]
        [SerializeField] private int baslangicReplik = 0;
        [Tooltip("Kaçıncı replikte bitecek (dahil). -1 = bölümün sonuna kadar.")]
        [SerializeField] private int bitisReplik = -1;
        [Tooltip("İkon bu mesafeden tıklanabilir (çok uzaktan başlatılamasın).")]
        [SerializeField] private float tiklamaMesafesi = 7f;
        [Tooltip("Oyuncu bu mesafeye girince diyalog OTOMATİK başlar (tıklamasız).")]
        [SerializeField] private float otomatikMesafe = 2.3f;
        [Tooltip("Konuşma balonu ikonu (boşsa yeşil kare placeholder).")]
        [SerializeField] private Sprite ikonSprite;

        private GameObject _kare;
        private SpriteRenderer _kareSr;
        private float _dogusY;
        private bool _otomatikYapildi; // sahne başına bir kez otomatik tetiklenir

        private void Start()
        {
            _kare = new GameObject("KonusmaIsareti");
            _kare.transform.SetParent(transform, false);
            _dogusY = 1.4f;
            _kare.transform.localPosition = new Vector3(0f, _dogusY, 0f);
            _kareSr = _kare.AddComponent<SpriteRenderer>();
            if (ikonSprite != null)
            {
                _kareSr.sprite = ikonSprite;       // gerçek konuşma ikonu
                _kareSr.color = Color.white;
                _kare.transform.localScale = Vector3.one;
            }
            else
            {
                _kareSr.sprite = BeyazKare();      // placeholder
                _kareSr.color = new Color(0.25f, 0.85f, 0.3f);
                _kare.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
            }
            _kareSr.sortingOrder = 100;
        }

        private void Update()
        {
            if (_kare == null) return;

            // Diyalog sürerken işaret gizli.
            bool gizli = DialogYonetici.DialogSuruyor;
            if (_kare.activeSelf == gizli) _kare.SetActive(!gizli);
            if (gizli) return;

            // İdle salınım: 1 piksellik yavaş aşağı-yukarı.
            _kare.transform.localPosition = new Vector3(
                0f, _dogusY + Mathf.Sin(Time.time * 2f) * 0.03f, 0f);

            var p = GameObject.FindGameObjectWithTag("Player");

            // OTOMATİK tetikleme: önünden geçerken karakter durup konuşmayı başlatır.
            if (!_otomatikYapildi && p != null
                && Vector2.Distance(p.transform.position, transform.position) <= otomatikMesafe)
            {
                _otomatikYapildi = true; // geri dönüşte tekrar tetiklenmesin
                DiyalogBaslat(p.transform);
                return;
            }

            // Elle tetikleme: ikona tıklama.
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 w = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            if (_kareSr.bounds.Contains(new Vector3(w.x, w.y, _kareSr.bounds.center.z)))
            {
                if (p != null && Vector2.Distance(p.transform.position, transform.position) > tiklamaMesafesi)
                    return;
                DiyalogBaslat(p != null ? p.transform : null);
            }
        }

        private void DiyalogBaslat(Transform oyuncu)
        {
            var replikler = ReplikDeposu.Bolum(replikDosyasi, bolumNo);
            if (replikler == null || replikler.Count == 0)
            {
                Debug.LogWarning("[Ulak] Bölüm " + bolumNo + " replikleri bulunamadı.");
                return;
            }

            // İstenen aralığı al (bölüm birden çok NPC'ye bölünebilsin).
            int bas = Mathf.Clamp(baslangicReplik, 0, replikler.Count - 1);
            int son = bitisReplik < 0 ? replikler.Count - 1
                                      : Mathf.Clamp(bitisReplik, bas, replikler.Count - 1);
            var dilim = replikler.GetRange(bas, son - bas + 1);

            Transform npc = transform;
            DialogYonetici.Baslat(dilim,
                konusan => konusan == "BALAMIR" && oyuncu != null ? oyuncu : npc);
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
    }
}
