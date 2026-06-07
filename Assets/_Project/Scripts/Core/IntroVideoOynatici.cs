using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace Ulak.Core
{
    /// <summary>
    /// İntro videosunu kameranın önünde TAM EKRAN oynatır; video bitince
    /// (ya da oyuncu atlarsa) sonraki sahneye geçer.
    /// BaslangicIntro sahnesinde durur: Menü → intro → UmayKoy akışı.
    /// </summary>
    public class IntroVideoOynatici : MonoBehaviour
    {
        [Tooltip("Oynatılacak video (intro.mp4).")]
        public VideoClip klip;
        [Tooltip("Video bitince yüklenecek sahne.")]
        public string sonrakiSahne = "UmayKoy";
        [Tooltip("Tıklama/herhangi bir tuşla atlanabilsin mi?")]
        public bool atlanabilir = true;

        private VideoPlayer _vp;
        private bool _bitti;

        private void Start()
        {
            var cam = Camera.main;
            if (cam == null || klip == null) { Bitir(); return; }

            _vp = cam.gameObject.AddComponent<VideoPlayer>();
            _vp.playOnAwake = false;
            _vp.clip = klip;
            _vp.renderMode = VideoRenderMode.CameraNearPlane;
            _vp.aspectRatio = VideoAspectRatio.FitOutside;     // tam ekran, oran korunur
            _vp.audioOutputMode = VideoAudioOutputMode.Direct; // videoda ses varsa çalar
            _vp.isLooping = false;
            _vp.skipOnDrop = false;
            _vp.loopPointReached += _ => Bitir();
            StartCoroutine(Oynat());
        }

        private IEnumerator Oynat()
        {
            _vp.Prepare();
            while (!_vp.isPrepared) yield return null;
            _vp.Play();
        }

        private void Update()
        {
            if (!atlanabilir || _bitti) return;
            if (Time.timeSinceLevelLoad < 0.5f) return; // menü tıklaması sızmasın

            var kb = Keyboard.current;
            var ms = Mouse.current;
            if ((kb != null && kb.anyKey.wasPressedThisFrame) ||
                (ms != null && ms.leftButton.wasPressedThisFrame))
                Bitir();
        }

        private void Bitir()
        {
            if (_bitti) return;
            _bitti = true;
            SceneManager.LoadScene(sonrakiSahne);
        }
    }
}
