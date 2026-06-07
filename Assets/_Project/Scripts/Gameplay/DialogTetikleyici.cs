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
        [Tooltip("Kare bu mesafeden tıklanabilir (çok uzaktan başlatılamasın).")]
        [SerializeField] private float tiklamaMesafesi = 7f;

        private GameObject _kare;
        private SpriteRenderer _kareSr;
        private float _dogusY;

        private void Start()
        {
            _kare = new GameObject("KonusmaIsareti");
            _kare.transform.SetParent(transform, false);
            _dogusY = 1.4f;
            _kare.transform.localPosition = new Vector3(0f, _dogusY, 0f);
            _kare.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
            _kareSr = _kare.AddComponent<SpriteRenderer>();
            _kareSr.sprite = BeyazKare();
            _kareSr.color = new Color(0.25f, 0.85f, 0.3f); // YEŞİL placeholder
            _kareSr.sortingOrder = 100;
        }

        private void Update()
        {
            if (_kare == null) return;

            // Diyalog sürerken işaret gizli.
            bool gizli = DialogYonetici.DialogSuruyor;
            if (_kare.activeSelf == gizli) _kare.SetActive(!gizli);
            if (gizli) return;

            // Yüzer animasyon (dikkat çeksin).
            _kare.transform.localPosition = new Vector3(
                0f, _dogusY + Mathf.Sin(Time.time * 3f) * 0.08f, 0f);

            // Tıklama kontrolü.
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 w = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            if (_kareSr.bounds.Contains(new Vector3(w.x, w.y, _kareSr.bounds.center.z)))
            {
                // Oyuncu yakın mı?
                var p = GameObject.FindGameObjectWithTag("Player");
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

            Transform npc = transform;
            DialogYonetici.Baslat(replikler,
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
