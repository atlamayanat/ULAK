using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ulak.Core
{
    /// <summary>
    /// Kalıcı arka plan müziği yöneticisi (greybox v1).
    ///  - DontDestroyOnLoad: tek kopya, sahneler arası yaşar → şarkının
    ///    KONUMU otomatik korunur (30. saniyede sahne değişirse 30'dan sürer).
    ///  - Yalnızca listedeki sahnelerde çalar; diğerlerinde duraklar
    ///    (pozisyon kaybolmaz), dönünce kaldığı yerden devam eder.
    ///  - Sahne açılışında yavaşça yükselerek girer (fade-in),
    ///    müzıksiz sahneye geçişte kısaca kısılarak susar.
    ///  - loop = true: parça biterse başa sarar.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MuzikYonetici : MonoBehaviour
    {
        public static MuzikYonetici Aktif { get; private set; }

        [SerializeField] private AudioClip muzik;
        [Tooltip("Müziğin çalacağı sahne adları.")]
        [SerializeField] private string[] muzikSahneleri =
            { "AT-GUNDUZ", "GECE-SAVAS", "BossFightArea" };
        [SerializeField, Range(0f, 1f)] private float hedefSes = 0.8f;
        [Tooltip("Sahne açılışında sesin yükselme süresi (sn).")]
        [SerializeField] private float girisSuresi = 1.6f;
        [Tooltip("Müziksiz sahneye geçişte kısılma süresi (sn).")]
        [SerializeField] private float cikisSuresi = 0.5f;

        private AudioSource _src;
        private Coroutine _fade;

        private void Awake()
        {
            if (Aktif != null) { Destroy(gameObject); return; } // tek kopya
            Aktif = this;
            DontDestroyOnLoad(gameObject);

            _src = GetComponent<AudioSource>();
            _src.clip = muzik;
            _src.loop = true;          // biterse başa sar
            _src.playOnAwake = false;
            _src.volume = 0f;

            SceneManager.sceneLoaded += SahneYuklendi;
            SahneKontrol(SceneManager.GetActiveScene().name);
        }

        private void OnDestroy()
        {
            if (Aktif == this)
            {
                Aktif = null;
                SceneManager.sceneLoaded -= SahneYuklendi;
            }
        }

        private void SahneYuklendi(Scene s, LoadSceneMode m) => SahneKontrol(s.name);

        private void SahneKontrol(string sahneAdi)
        {
            bool calmali = System.Array.Exists(muzikSahneleri, x => x == sahneAdi);
            if (_fade != null) StopCoroutine(_fade);

            if (calmali)
            {
                if (!_src.isPlaying)
                {
                    _src.UnPause();                    // duraklatıldıysa kaldığı yerden
                    if (!_src.isPlaying) _src.Play();  // hiç başlamadıysa baştan
                }
                _fade = StartCoroutine(SesGecisi(hedefSes, girisSuresi)); // yavaşça yüksel
            }
            else if (_src.isPlaying)
            {
                _fade = StartCoroutine(KisilipDurakla());
            }
        }

        private IEnumerator SesGecisi(float hedef, float sure)
        {
            float baslangic = _src.volume;
            float t = 0f;
            while (t < sure)
            {
                t += Time.unscaledDeltaTime; // pause menüsünden etkilenmesin
                _src.volume = Mathf.Lerp(baslangic, hedef, t / sure);
                yield return null;
            }
            _src.volume = hedef;
        }

        private IEnumerator KisilipDurakla()
        {
            yield return SesGecisi(0f, cikisSuresi);
            _src.Pause(); // pozisyon korunur
        }
    }
}
