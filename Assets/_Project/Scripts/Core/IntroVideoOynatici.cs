using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace Ulak.Core
{
    /// <summary>
    /// Başlangıç akışı (BaslangicIntro sahnesi):
    ///  1. BİLGİ EKRANI — giris_bilgi.png tam ekran gösterilir,
    ///     giris_bilgi_ses çalar. Ses bitince YA DA oyuncu tıklayınca geçilir.
    ///  2. İNTRO — intro.mp4 tam ekran oynar, davul sesi eş zamanlı başlar ve
    ///     animasyon boyunca çalar (video sesi kapalı: yalnız davul duyulur).
    ///  3. Video bitince (ya da atlanırsa) sonraki sahne yüklenir.
    /// </summary>
    public class IntroVideoOynatici : MonoBehaviour
    {
        [Header("1) Bilgi Ekranı")]
        [Tooltip("Tam ekran bilgi görseli (giris_bilgi.png).")]
        public Sprite bilgiGorseli;
        [Tooltip("Bilgi ekranında çalan ses (giris_bilgi_ses.wav).")]
        public AudioClip bilgiSesi;
        [Tooltip("Ses dosyası yoksa bilgi ekranının kalma süresi (sn).")]
        public float bilgiVarsayilanSure = 6f;

        [Header("2) İntro Videosu")]
        [Tooltip("Oynatılacak video (intro.mp4).")]
        public VideoClip klip;
        [Tooltip("Video boyunca çalan davul sesi (davul.mp3).")]
        public AudioClip davulSesi;
        [Tooltip("Video bitince yüklenecek sahne.")]
        public string sonrakiSahne = "UmayKoy";
        [Tooltip("Video tıklama/tuşla atlanabilsin mi?")]
        public bool atlanabilir = true;

        private VideoPlayer _vp;
        private AudioSource _src;
        private bool _bilgiGoster;
        private bool _videoBitti;
        private bool _yuklendi;

        private void Start()
        {
            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            StartCoroutine(Akis());
        }

        private IEnumerator Akis()
        {
            // --- 1) BİLGİ EKRANI: görsel + ses; ses bitince ya da tıklayınca geç ---
            if (bilgiGorseli != null)
            {
                _bilgiGoster = true;
                float fazBasi = Time.time;
                float bitis = fazBasi + bilgiVarsayilanSure;
                if (bilgiSesi != null)
                {
                    _src.clip = bilgiSesi;
                    _src.loop = false;
                    _src.Play();
                    bitis = fazBasi + bilgiSesi.length;
                }
                while (Time.time < bitis && !Tiklandi(fazBasi)) yield return null;
                _src.Stop();
                _bilgiGoster = false;
            }

            // --- 2) VİDEO + DAVUL (eş zamanlı) ---
            var cam = Camera.main;
            if (cam == null || klip == null) { SahneyiYukle(); yield break; }

            _vp = cam.gameObject.AddComponent<VideoPlayer>();
            _vp.playOnAwake = false;
            _vp.clip = klip;
            _vp.renderMode = VideoRenderMode.CameraNearPlane;
            _vp.aspectRatio = VideoAspectRatio.FitOutside;   // tam ekran
            _vp.audioOutputMode = VideoAudioOutputMode.None; // yalnız davul duyulsun
            _vp.isLooping = false;
            _vp.skipOnDrop = false;
            _vp.loopPointReached += _ => _videoBitti = true;

            _vp.Prepare();
            while (!_vp.isPrepared) yield return null;

            if (davulSesi != null)
            {
                _src.clip = davulSesi;
                _src.loop = true; // animasyon boyunca sürsün
                _src.Play();
            }
            _vp.Play();

            float videoBasi = Time.time;
            while (!_videoBitti)
            {
                if (atlanabilir && Tiklandi(videoBasi)) break;
                yield return null;
            }
            SahneyiYukle(); // sahne değişince davul da kendiliğinden susar
        }

        /// <summary>Referans andan 0.5 sn geçtikten sonra tık/tuş algılar
        /// (önceki ekranın tıklaması yeni faza sızmasın).</summary>
        private bool Tiklandi(float referans)
        {
            if (Time.time - referans < 0.5f) return false;
            var kb = Keyboard.current;
            var ms = Mouse.current;
            return (kb != null && kb.anyKey.wasPressedThisFrame)
                || (ms != null && ms.leftButton.wasPressedThisFrame);
        }

        private void SahneyiYukle()
        {
            if (_yuklendi) return;
            _yuklendi = true;
            SceneManager.LoadScene(sonrakiSahne);
        }

        private void OnGUI()
        {
            if (!_bilgiGoster || bilgiGorseli == null) return;
            // Tam ekran kapla — oran korunur, taşan kenar kırpılır.
            var tex = bilgiGorseli.texture;
            float sw = Screen.width, sh = Screen.height;
            float olcek = Mathf.Max(sw / tex.width, sh / tex.height);
            float w = tex.width * olcek, h = tex.height * olcek;
            GUI.DrawTexture(new Rect((sw - w) / 2f, (sh - h) / 2f, w, h), tex);
        }
    }
}
