using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ulak.Core;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Gulyabani final ara sahnesi:
    ///  1. Boss'a son vuruş atılınca tetiklenir (BossController.TakeDamage → Baslat).
    ///  2. Oyuncu 3 no.lu, boss 5 no.lu ışınlanma noktasına ışınlanır.
    ///  3. Ana karakter "Onu öldüremiyorum..." repliğini söyler.
    ///  4. Küçük kurt ekranın soluna doğru koşup kaybolur.
    ///  5. Arkadan 12 BÜYÜK kurt peş peşe koşarak boss'un olduğu noktaya varır.
    ///  6. Ekran yavaşça kararır.
    /// </summary>
    public class GulyabaniFinalSahnesi : MonoBehaviour
    {
        [Header("Işınlanma Noktaları")]
        [Tooltip("Oyuncunun ışınlanacağı nokta (IsinlanmaNoktasi_3).")]
        public Transform oyuncuNoktasi;
        [Tooltip("Boss'un ışınlanacağı nokta (IsinlanmaNoktasi_5).")]
        public Transform bossNoktasi;

        [Header("Kurt Sürüsü")]
        [Tooltip("Büyük kurtların koşu kareleri (kurt_r0_c0..c5).")]
        public Sprite[] kurtKosuKareleri;
        [Tooltip("Büyük kurt ölçeği (küçük kurdun büyütülmüşü).")]
        public float kurtOlcek = 1.8f;
        public int kurtSayisi = 12;
        public float kurtHizi = 9f;
        [Tooltip("İki kurdun çıkışı arasındaki süre (sn) — küçük değer = sıkı sürü.")]
        public float kurtAraligi = 0.12f;

        [Header("Kararma")]
        public float kararmaSuresi = 0.35f;
        [Tooltip("İlk kurt bu X konumunu geçince kararma başlar.")]
        public float kararmaTetikX = 1f;

        [Header("Final Videosu")]
        [Tooltip("Kararmadan sonra oynatılacak video (gulyabanininsonu.mp4).")]
        public UnityEngine.Video.VideoClip finalVideo;
        [Tooltip("Videonun toplam oynatma süresi (sn). 0 = orijinal hız.")]
        public float videoSuresi = 0f;

        [Header("Gulyabani'nin Sonu")]
        [Tooltip("Videodan sonra boss'un yerini alan, gözü kapalı yerde yatış görseli.")]
        public Sprite yatanGulyabani;

        [Header("Kapanış Sinematiği")]
        [Tooltip("Oyuncu sahneden çıkıp ekran karardıktan sonra tam ekran oynar (finalimizss.mp4).")]
        public UnityEngine.Video.VideoClip kapanisVideosu;
        [Tooltip("Kapanış sinematiği boyunca arkada çalan davul sesi (davul.mp3).")]
        public AudioClip davulSesi;
        [Tooltip("Sinematik bitince gösterilecek jenerik görseli (jenerik.png).")]
        public Sprite jenerikGorseli;

        private bool _basladi;
        private float _karartma; // 0..1 ekran karartma düzeyi
        private bool _jenerikGoster;
        private AudioSource _davul;
        private Transform _oyuncu;
        private readonly List<GameObject> _suru = new List<GameObject>(); // büyük kurtlar

        /// <summary>Son vuruşta BossController tarafından çağrılır.</summary>
        public void Baslat(BossController boss)
        {
            if (_basladi) return;
            _basladi = true;
            StartCoroutine(Akis(boss));
        }

        private IEnumerator Akis(BossController boss)
        {
            // --- 1) Dünyayı dondur ---
            var oyuncuGo = GameObject.FindGameObjectWithTag("Player");
            _oyuncu = oyuncuGo != null ? oyuncuGo.transform : null;
            if (_oyuncu == null) yield break;

            KontrolleriKilitle(oyuncuGo);
            BossuDondur(boss);

            // Videoyu ŞİMDİDEN arka planda yüklemeye başla — kararma anında
            // hazır olur, siyah ekranda bekleme ("yükleme ekranı") kalmaz.
            var vp = VideoyuHazirla(finalVideo);

            // Havadaki mavi ateş toplarını söndür.
            foreach (var top in FindObjectsByType<MaviAtesTopu>(FindObjectsSortMode.None))
                Destroy(top.gameObject);

            // --- 2) Işınlanmalar ---
            if (oyuncuNoktasi != null)
            {
                _oyuncu.position = ZemineOturt(oyuncuNoktasi.position, 1.05f);
                var rb = _oyuncu.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
                // Oyuncu boss'a (sağa) baksın.
                var o = _oyuncu.localScale;
                _oyuncu.localScale = new Vector3(Mathf.Abs(o.x), o.y, o.z);
            }
            if (bossNoktasi != null && boss != null)
            {
                boss.transform.position = ZemineOturt(bossNoktasi.position, 1.7f);
                // Boss oyuncuya (sola) baksın — büyücü deseni solak, +1 = sola bakış.
                var b = boss.transform.localScale;
                boss.transform.localScale = new Vector3(Mathf.Abs(b.x), b.y, b.z);
            }

            yield return new WaitForSeconds(0.7f); // sahne otursun

            // --- 3) Replik ---
            bool replikBitti = false;
            DialogYonetici.Baslat(
                new List<Replik> { new Replik("BALAMIR", "Onu öldüremiyorum...") },
                k => _oyuncu,
                () => replikBitti = true);
            while (!replikBitti) yield return null;

            yield return new WaitForSeconds(0.4f);

            // --- 4) Küçük kurt ekran dışına koşar ---
            yield return KucukKurtKacisi();

            yield return new WaitForSeconds(0.6f); // kısa nefes

            // --- 5) 12 büyük kurt arka arkaya ---
            float cikisX = KameraSolKenari() - 2.5f;
            float hedefX = bossNoktasi != null ? bossNoktasi.position.x : 4f;
            float zeminY = ZemineOturt(new Vector3(hedefX, -3.5f, 0f), kurtOlcek * 0.5f).y;

            _suru.Clear();
            for (int i = 0; i < kurtSayisi; i++)
            {
                var kurt = BuyukKurtYap(new Vector3(cikisX, zeminY, 0f), i);
                _suru.Add(kurt);
                StartCoroutine(KurtKosusu(kurt, hedefX, null));
                yield return new WaitForSeconds(kurtAraligi);
            }

            // İlk kurt tetik X'ini geçer geçmez kararma başlar
            // (sürü karanlıkta koşmaya devam eder).
            bool gecti = false;
            while (!gecti)
            {
                foreach (var k in _suru)
                    if (k != null && k.transform.position.x >= kararmaTetikX) { gecti = true; break; }
                if (!gecti) yield return null;
            }

            // --- 6) Ekran yavaşça kararır ---
            float t = 0f;
            while (t < kararmaSuresi)
            {
                t += Time.deltaTime;
                _karartma = Mathf.Clamp01(t / kararmaSuresi);
                yield return null;
            }
            _karartma = 1f;

            // --- 7) Final videosu (müzik çalmaya devam eder) ---
            if (vp != null)
                yield return VideoyuOynat(vp, videoSuresi);

            // --- 8) Gulyabani yere serilir; perde açılır, kurtlar geldikleri yoldan döner ---
            if (boss != null) GulyabaniyiYatir(boss); // kurtlar tarafından yenilmiş
            yield return Perde(0f, 0.8f);
            yield return SuruGeriDoner();

            // --- 9) Replik ---
            bool replik2Bitti = false;
            DialogYonetici.Baslat(
                new List<Replik> { new Replik("BALAMIR", "Sanırım onları takip etmem gerekiyor...") },
                k => _oyuncu,
                () => replik2Bitti = true);
            while (!replik2Bitti) yield return null;

            yield return new WaitForSeconds(0.3f);

            // --- 10) Oyuncu kurtların peşinden koşup sahneden çıkar ---
            // (kapanış sinematiği bu sırada arka planda hazırlanır)
            var kapanisVp = VideoyuHazirla(kapanisVideosu);
            yield return OyuncuCikisi();

            // --- 11) Son kararma ---
            yield return Perde(1f, 1.2f);

            // --- 12) Kapanış sinematiği: tam ekran + davul eşliğinde ---
            if (kapanisVp != null)
            {
                if (davulSesi != null)
                {
                    _davul = gameObject.AddComponent<AudioSource>();
                    _davul.clip = davulSesi;
                    _davul.loop = true;
                    _davul.playOnAwake = false;
                    _davul.Play();
                }
                yield return VideoyuOynat(kapanisVp, 0f);
                if (_davul != null) yield return DavulSondur(0.7f);
            }

            // --- 13) Jenerik: sinematikten hemen sonra ---
            if (jenerikGorseli != null)
                _jenerikGoster = true; // OnGUI siyah perdenin üstüne çizer
        }

        private IEnumerator DavulSondur(float sure)
        {
            float bas = _davul.volume, t = 0f;
            while (t < sure)
            {
                t += Time.deltaTime;
                _davul.volume = Mathf.Lerp(bas, 0f, t / sure);
                yield return null;
            }
            _davul.Stop();
        }

        /// <summary>Boss'u gözü kapalı, yere serilmiş görseline çevirir.</summary>
        private void GulyabaniyiYatir(BossController boss)
        {
            var sr = boss.GetComponent<SpriteRenderer>();
            if (yatanGulyabani == null || sr == null)
            {
                Destroy(boss.gameObject); // görsel yoksa eski davranış: yok et
                return;
            }

            // Flipbook kapatılmazsa yatan kareyi her karede idle ile ezer.
            var fb = boss.GetComponent<SpriteFlipbook>();
            if (fb != null) fb.enabled = false;

            sr.sprite = yatanGulyabani;
            sr.color = Color.white;
            boss.transform.localScale = Vector3.one; // dövüşteki yön/ölçek kalıntısını sıfırla

            // Cesedi zemine oturt (pivot merkez → yarı yükseklik kadar yukarı).
            var poz = bossNoktasi != null ? bossNoktasi.position : boss.transform.position;
            var zem = Physics2D.Raycast((Vector2)poz + Vector2.up * 2f, Vector2.down, 14f,
                LayerMask.GetMask("Ground"));
            float tabanY = zem.collider != null ? zem.point.y : poz.y - 1.7f;
            boss.transform.position = new Vector3(
                poz.x, tabanY + yatanGulyabani.bounds.extents.y + 0.02f, poz.z);

            var col = boss.GetComponent<Collider2D>();
            if (col != null) col.enabled = false; // ceset fiziksel engel olmasın
        }

        /// <summary>Siyah perdeyi hedef değere yumuşakça götürür (0 = açık, 1 = kapalı).</summary>
        private IEnumerator Perde(float hedef, float sure)
        {
            float bas = _karartma, t = 0f;
            while (t < sure)
            {
                t += Time.deltaTime;
                _karartma = Mathf.Lerp(bas, hedef, Mathf.Clamp01(t / sure));
                yield return null;
            }
            _karartma = hedef;
        }

        /// <summary>Sürü, geldiği yoldan (sola) koşarak sahneyi terk eder.</summary>
        private IEnumerator SuruGeriDoner()
        {
            float cikisX = KameraSolKenari() - 2.5f;
            int kalan = 0;
            foreach (var k in _suru)
            {
                if (k == null) continue;
                kalan++;
                StartCoroutine(KurtGeriKosusu(k, cikisX, () => kalan--));
                yield return new WaitForSeconds(Random.Range(0.04f, 0.18f)); // sürü doğal dağılsın
            }
            while (kalan > 0) yield return null;
        }

        private IEnumerator KurtGeriKosusu(GameObject kurt, float cikisX, System.Action bitti)
        {
            var sr = kurt.GetComponent<SpriteRenderer>();
            if (sr != null) sr.flipX = true; // sola dön
            var book = kurt.GetComponent<SpriteFlipbook>();
            if (book != null) book.SetMoving(true);

            float hiz = kurtHizi * Random.Range(0.92f, 1.12f);
            while (kurt != null && kurt.transform.position.x > cikisX)
            {
                kurt.transform.position += Vector3.left * hiz * Time.deltaTime;
                yield return null;
            }
            if (kurt != null) Destroy(kurt);
            bitti?.Invoke();
        }

        /// <summary>Oyuncu sola dönüp koşarak ekrandan çıkar.</summary>
        private IEnumerator OyuncuCikisi()
        {
            if (_oyuncu == null) yield break;

            var book = _oyuncu.GetComponent<SpriteFlipbook>();
            if (book != null) { book.SetFacing(-1); book.SetMoving(true); }

            var rb = _oyuncu.GetComponent<Rigidbody2D>();
            float cikisX = KameraSolKenari() - 0.9f; // görüş dışı ama sol duvara girmeden
            while (_oyuncu != null && _oyuncu.position.x > cikisX)
            {
                _oyuncu.position += Vector3.left * 5.5f * Time.deltaTime;
                if (rb != null) rb.linearVelocity = Vector2.zero; // fizik itmesin
                yield return null;
            }
            if (book != null) book.SetMoving(false);
        }

        /// <summary>VideoPlayer'ı kurar ve arka planda yüklemeye başlar.</summary>
        private UnityEngine.Video.VideoPlayer VideoyuHazirla(UnityEngine.Video.VideoClip k)
        {
            var cam = Camera.main;
            if (cam == null || k == null) return null;

            var vp = cam.gameObject.AddComponent<UnityEngine.Video.VideoPlayer>();
            vp.playOnAwake = false;
            vp.clip = k;
            vp.renderMode = UnityEngine.Video.VideoRenderMode.CameraNearPlane;
            vp.aspectRatio = UnityEngine.Video.VideoAspectRatio.FitOutside;   // TAM EKRAN
            vp.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None; // müzik sürsün
            vp.isLooping = false;
            vp.skipOnDrop = false; // yavaş oynatmada kare atlamasın
            vp.Prepare();          // sinematik akarken arka planda yüklenir
            return vp;
        }

        private IEnumerator VideoyuOynat(UnityEngine.Video.VideoPlayer vp, float istenenSure)
        {
            // Genelde çoktan hazır — önceki sahne adımları boyunca yüklendi.
            while (!vp.isPrepared) yield return null;

            // Klibi istenen süreye yay (0 = orijinal hız).
            if (istenenSure > 0.05f && vp.clip != null && vp.clip.length > 0.01)
                vp.playbackSpeed = (float)(vp.clip.length / istenenSure);

            // İlk kareyi getirip DURDUR — perde ilk karenin üstünden açılsın,
            // videonun başı perde arkasında kaybolmasın.
            vp.Play();
            while (vp.frame < 1) yield return null;
            vp.Pause();

            yield return Perde(0f, 0.3f);            // ilk kare görünür
            yield return new WaitForSeconds(0.15f);  // kısa nefes

            vp.Play();                               // baştan sona oynar
            while (vp.isPlaying) yield return null;

            yield return Perde(1f, 0.6f);            // son kareden siyaha
            vp.Stop();
            Destroy(vp); // kamera üzerinde kalıntı bırakma
        }

        // ---- yardımcılar ----

        private void KontrolleriKilitle(GameObject oyuncu)
        {
            var pc = oyuncu.GetComponent<PlayerController>();
            if (pc != null) pc.enabled = false;
            var sa = oyuncu.GetComponentInChildren<SwordAttack>();
            if (sa != null) sa.enabled = false;
            var kc = oyuncu.GetComponent<KillCharges>();
            if (kc != null) kc.enabled = false; // sayaç da sinematikte gizlensin
        }

        private void BossuDondur(BossController boss)
        {
            if (boss == null) return;
            boss.StopAllCoroutines();
            boss.enabled = false;
            var rb = boss.GetComponent<Rigidbody2D>();
            if (rb != null) { rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Kinematic; }
            var sr = boss.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.white; // yarım kalmış flash/fade temizliği

            // Saldırı pozunda yakalandıysa idle nefes döngüsüne dön.
            var fb = boss.GetComponent<SpriteFlipbook>();
            if (fb != null) { fb.enabled = true; fb.SetMoving(false); }
        }

        private Vector3 ZemineOturt(Vector3 nokta, float pivotYuksekligi)
        {
            var hit = Physics2D.Raycast((Vector2)nokta + Vector2.up * 2f, Vector2.down, 14f,
                LayerMask.GetMask("Ground"));
            if (hit.collider != null) return new Vector3(nokta.x, hit.point.y + pivotYuksekligi, nokta.z);
            return nokta;
        }

        private float KameraSolKenari()
        {
            var cam = Camera.main;
            if (cam == null) return -22f;
            return cam.transform.position.x - cam.orthographicSize * cam.aspect;
        }

        private IEnumerator KucukKurtKacisi()
        {
            var kurt = FindFirstObjectByType<KurtYoldas>();
            if (kurt == null) yield break;

            kurt.enabled = false; // takibi bırak
            var sr = kurt.GetComponent<SpriteRenderer>();
            var book = kurt.GetComponent<SpriteFlipbook>();
            if (sr != null) sr.flipX = true; // sola koşacak
            if (book != null) book.SetMoving(true);

            float hedefX = KameraSolKenari() - 2f;
            while (kurt != null && kurt.transform.position.x > hedefX)
            {
                kurt.transform.position += Vector3.left * (kurtHizi * 1.2f) * Time.deltaTime;
                yield return null;
            }
            if (kurt != null) Destroy(kurt.gameObject);
        }

        private GameObject BuyukKurtYap(Vector3 poz, int sira)
        {
            var go = new GameObject("BuyukKurt_" + (sira + 1));
            // Sürü hissi: hafif dikey saçılım + minik ölçek farkı.
            go.transform.position = poz + Vector3.up * Random.Range(-0.18f, 0.3f);
            go.transform.localScale = Vector3.one * (kurtOlcek * Random.Range(0.92f, 1.08f));

            var sr = go.AddComponent<SpriteRenderer>();
            if (kurtKosuKareleri != null && kurtKosuKareleri.Length > 0)
                sr.sprite = kurtKosuKareleri[0];
            sr.sortingOrder = 11 + (sira % 3); // katmanlı sürü görünümü, boss'un üstünde
            sr.flipX = false;                  // sağa koşuyorlar

            if (kurtKosuKareleri != null && kurtKosuKareleri.Length > 1)
            {
                var book = go.AddComponent<SpriteFlipbook>();
                var bf = System.Reflection.BindingFlags.Instance |
                         System.Reflection.BindingFlags.NonPublic;
                // Durunca da boş kalmasın diye idle = ilk koşu karesi.
                typeof(SpriteFlipbook).GetField("frames", bf)
                    ?.SetValue(book, new[] { kurtKosuKareleri[0] });
                typeof(SpriteFlipbook).GetField("walkFrames", bf)
                    ?.SetValue(book, kurtKosuKareleri);
                typeof(SpriteFlipbook).GetField("walkFrameInterval", bf)
                    ?.SetValue(book, 0.08f); // hızlı koşu temposu
                book.SetMoving(true);
            }
            return go;
        }

        private IEnumerator KurtKosusu(GameObject kurt, float hedefX, System.Action varinca)
        {
            // SÜRÜ: dar saçılım — hepsi Gulyabani'nin ÜSTÜNE yığılır.
            float durmaX = hedefX + Random.Range(-0.6f, 0.6f);
            float hiz = kurtHizi * Random.Range(0.92f, 1.12f); // tempo farkıyla doğal sürü
            while (kurt != null && kurt.transform.position.x < durmaX)
            {
                kurt.transform.position += Vector3.right * hiz * Time.deltaTime;
                yield return null;
            }
            if (kurt != null)
            {
                var book = kurt.GetComponent<SpriteFlipbook>();
                if (book != null) book.SetMoving(false); // varınca dur
            }
            varinca?.Invoke();
        }

        private void OnGUI()
        {
            if (_karartma > 0f)
            {
                var eski = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, _karartma);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = eski;
            }

            // Jenerik — siyah perdenin ÜSTÜNE, tam ekran (oran korunur).
            if (_jenerikGoster && jenerikGorseli != null)
            {
                var tex = jenerikGorseli.texture;
                float sw = Screen.width, sh = Screen.height;
                float olcek = Mathf.Max(sw / tex.width, sh / tex.height);
                float w = tex.width * olcek, h = tex.height * olcek;
                GUI.DrawTexture(new Rect((sw - w) / 2f, (sh - h) / 2f, w, h), tex);
            }
        }
    }
}
