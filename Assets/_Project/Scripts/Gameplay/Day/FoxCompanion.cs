using UnityEngine;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Tilki yoldaş (gündüz modu, greybox v1):
    /// Arada sırada atın yanında belirir, birlikte koşar, sonra kaybolur.
    /// Çarpışmaz, tamamen dekoratif — atın konumunu (zıplaması dahil) takip eder.
    /// Animasyon SpriteFlipbook'tan gelir; bu script sadece belirme/kaybolma
    /// döngüsünü ve takibi yönetir.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class FoxCompanion : MonoBehaviour
    {
        [Header("Zamanlama (sn)")]
        [Tooltip("Kaybolduktan sonra yeniden belirmeden önceki bekleme aralığı.")]
        [SerializeField] private float hiddenMin = 6f;
        [SerializeField] private float hiddenMax = 16f;
        [Tooltip("Birlikte koşma süresi aralığı.")]
        [SerializeField] private float runMin = 4f;
        [SerializeField] private float runMax = 9f;
        [Tooltip("Belirme/kaybolma geçiş süresi.")]
        [SerializeField] private float fadeTime = 0.7f;

        [Header("Takip")]
        [Tooltip("Ata göre konum (negatif X = atın arkası).")]
        [SerializeField] private Vector2 followOffset = new Vector2(-2.2f, -0.25f);
        [SerializeField] private string playerTag = "Player";

        private SpriteRenderer _sr;
        private Transform _horse;

        private enum State { Hidden, FadeIn, Running, FadeOut }
        private State _state = State.Hidden;
        private float _stateUntil;
        private float _fade;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            SetAlpha(0f);
            // İlk belirme erken gelsin ki oyuncu özelliği fark etsin.
            _stateUntil = Time.time + Random.Range(2f, 6f);
        }

        private void Start()
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null) _horse = go.transform;
        }

        private void LateUpdate()
        {
            if (_horse == null) return;

            // Atı takip et (zıplayınca tilki de zıplamış gibi görünür).
            transform.position = new Vector3(
                _horse.position.x + followOffset.x,
                _horse.position.y + followOffset.y,
                0f);

            switch (_state)
            {
                case State.Hidden:
                    if (Time.time >= _stateUntil)
                    {
                        _state = State.FadeIn;
                        _fade = 0f;
                    }
                    break;

                case State.FadeIn:
                    _fade += Time.deltaTime / fadeTime;
                    SetAlpha(Mathf.Clamp01(_fade));
                    if (_fade >= 1f)
                    {
                        _state = State.Running;
                        _stateUntil = Time.time + Random.Range(runMin, runMax);
                    }
                    break;

                case State.Running:
                    if (Time.time >= _stateUntil)
                    {
                        _state = State.FadeOut;
                        _fade = 0f;
                    }
                    break;

                case State.FadeOut:
                    _fade += Time.deltaTime / fadeTime;
                    SetAlpha(1f - Mathf.Clamp01(_fade));
                    if (_fade >= 1f)
                    {
                        _state = State.Hidden;
                        _stateUntil = Time.time + Random.Range(hiddenMin, hiddenMax);
                    }
                    break;
            }
        }

        private void SetAlpha(float a)
        {
            var c = _sr.color;
            c.a = a;
            _sr.color = c;
        }
    }
}
