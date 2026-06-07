using UnityEngine;
using UnityEngine.InputSystem;
using Ulak.Core;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Kurtla tanışma ara sahnesi (AT-GUNDUZ):
    ///  1) BEKLEME — kurt yol kenarında durur.
    ///  2) SAHNE — at yaklaşınca otomatik durur (kontroller + animasyon donar).
    ///     Şimdilik replik yok; kısa bekleme sonrası SOL TIK ile devam edilir.
    ///     (Replik sistemi gelince bu faza diyalog kutusu bağlanacak.)
    ///  3) YOLDAŞ — bu andan itibaren kurt oyun sonuna dek atın yanında koşar.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class KurtCutscene : MonoBehaviour
    {
        [Header("Tetikleme")]
        [Tooltip("At bu mesafeye gelince sahne başlar.")]
        [SerializeField] private float triggerDistance = 6f;

        [Header("Replik")]
        [Tooltip("replikler.txt — BÖLÜM 3 (Balamir'in kurtla tanışması) otomatik oynar.")]
        [SerializeField] private TextAsset replikDosyasi;
        [SerializeField] private int bolumNo = 3;

        [Header("Yoldaş Takibi")]
        [Tooltip("Ata göre koşu konumu (negatif X = atın arkası).")]
        [SerializeField] private Vector2 followOffset = new Vector2(-2.6f, -0.45f);

        [SerializeField] private string playerTag = "Player";

        private enum Phase { Bekleme, Sahne, Yoldas }
        private Phase _phase = Phase.Bekleme;

        private Transform _horse;
        private HorseController _hc;
        private SpriteFlipbook _horseBook;
        private SwordAttack _atk;
        private SpriteRenderer _sr;
        private float _stopTime;

        private void Start()
        {
            // Kurt zaten kazanıldıysa (sahne tekrar yüklendi/ziyaret edildi)
            // sahnedeki kopya kendini kaldırır — cutscene tekrarlanmaz.
            if (KurtYoldas.Aktif)
            {
                Destroy(gameObject);
                return;
            }

            _sr = GetComponent<SpriteRenderer>();
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null)
            {
                _horse = go.transform;
                _hc = go.GetComponent<HorseController>();
                _horseBook = go.GetComponent<SpriteFlipbook>();
                _atk = go.GetComponent<SwordAttack>();
            }
        }

        private void Update()
        {
            if (_horse == null) return;

            switch (_phase)
            {
                case Phase.Bekleme:
                    if (Mathf.Abs(_horse.position.x - transform.position.x) <= triggerDistance)
                        SahneyiBaslat();
                    break;

                case Phase.Sahne:
                    // Akış DialogYonetici'de: replikler bitince DevamEt çağrılır.
                    break;
            }
        }

        private void SahneyiBaslat()
        {
            _phase = Phase.Sahne;
            _stopTime = Time.time;

            // Atı durdur: kontrol + saldırı + animasyon donar.
            if (_hc != null)
            {
                var rb = _hc.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
                _hc.enabled = false;
            }
            if (_horseBook != null) _horseBook.enabled = false;
            if (_atk != null) _atk.enabled = false;

            // Kurt oyuncuya dönsün.
            if (_sr != null)
                _sr.flipX = _horse.position.x < transform.position.x;

            // Replikler OTOMATİK başlar; her tıklama ilerletir, bitince yolculuk sürer.
            var replikler = Ulak.Core.ReplikDeposu.Bolum(replikDosyasi, bolumNo);
            if (replikler != null && replikler.Count > 0)
            {
                Transform horse = _horse;
                Ulak.Core.DialogYonetici.Baslat(replikler,
                    k => horse, // Balamir kendi kendine konuşuyor → balon oyuncuda
                    () => StartCoroutine(DevamEt()));
            }
            else
            {
                // Replik yoksa eski davranış: kısa bekleme sonrası tıkla-devam yerine direkt devam.
                StartCoroutine(DevamEt());
            }
        }

        private System.Collections.IEnumerator DevamEt()
        {
            _phase = Phase.Yoldas;

            if (_horseBook != null) _horseBook.enabled = true;
            if (_hc != null) _hc.enabled = true;

            // Devam tıklaması kılıç saldırısı tetiklemesin: 2 kare sonra aç.
            yield return null;
            yield return null;
            if (_atk != null) _atk.enabled = true;

            // Kurt artık KALICI yoldaş: sahneler arası taşınır, hep arkandan koşar.
            var yoldas = gameObject.AddComponent<KurtYoldas>();
            var f = typeof(KurtYoldas).GetField("followOffset",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            f?.SetValue(yoldas, followOffset);
            Destroy(this); // cutscene görevi bitti
        }
    }
}
