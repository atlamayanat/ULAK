using UnityEngine;
using Ulak.Core;

namespace Ulak.Gameplay
{
    /// <summary>
    /// AT-GUNDUZ: müziğin temposunu (pitch) atın ANLIK hızına bağlar.
    /// At dururken yavaş, son süratte hızlı tempo — değişim yumuşatılır.
    /// Sahneden çıkınca MuzikYonetici pitch'i kendiliğinden 1'e döndürür.
    /// </summary>
    public class AtMuzikTemposu : MonoBehaviour
    {
        [Tooltip("Boş bırakılırsa sahnedeki atı kendisi bulur.")]
        [SerializeField] private HorseController at;
        [Tooltip("At dururken müzik temposu.")]
        [SerializeField] private float minPitch = 0.9f;
        [Tooltip("At son süratteyken müzik temposu.")]
        [SerializeField] private float maxPitch = 1.25f;
        [Tooltip("Tempo değişiminin yumuşaklığı (büyük = daha çabuk tepki).")]
        [SerializeField] private float yumusatma = 2.5f;

        private float _pitch = 1f;

        private void Start()
        {
            if (at == null) at = FindFirstObjectByType<HorseController>();
        }

        private void Update()
        {
            if (MuzikYonetici.Aktif == null) return;
            if (at == null)
            {
                at = FindFirstObjectByType<HorseController>(); // respawn sonrası yeniden bul
                if (at == null) return;
            }

            float oran = at.MaxSpeed > 0.01f
                ? Mathf.Clamp01(at.CurrentSpeed / at.MaxSpeed)
                : 0f;
            float hedef = Mathf.Lerp(minPitch, maxPitch, oran);
            _pitch = Mathf.Lerp(_pitch, hedef, yumusatma * Time.deltaTime);
            MuzikYonetici.Aktif.TempoAyarla(_pitch);
        }
    }
}
