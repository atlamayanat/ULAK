using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Kalıcı kurt yoldaş — cutscene'de kazanıldıktan sonra OYUN SONUNA DEK
    /// her sahnede karakterin ARKASINDAN koşar:
    ///  - DontDestroyOnLoad: sahne geçişlerinde yok olmaz.
    ///  - Her sahnede "Player" tag'li karakteri yeniden bulur (at ya da yaya).
    ///  - Karakter yön değiştirince arkasına geçer, sprite'ı da döner.
    ///  - Oyuncu olmayan sahnelerde (menü vb.) gizlenir.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class KurtYoldas : MonoBehaviour
    {
        /// <summary>Kurt yoldaş kazanıldı mı? (Cutscene tekrarını engeller.)</summary>
        public static bool Aktif { get; private set; }

        [Tooltip("Karaktere göre koşu konumu (X büyüklüğü = arkada kalma mesafesi).")]
        [SerializeField] private Vector2 followOffset = new Vector2(-2.2f, -0.45f);
        [Tooltip("Takip yumuşaklığı (büyük = daha sıkı takip).")]
        [SerializeField] private float followLerp = 8f;
        [SerializeField] private string playerTag = "Player";

        private Transform _player;
        private SpriteRenderer _sr;
        private Ulak.Core.SpriteFlipbook _book;
        private int _dir = 1;
        private float _lastPlayerX;
        private bool _sahip; // statik bayrağın sahibi bu kopya mı?

        private void Awake()
        {
            if (Aktif) { Destroy(gameObject); return; } // zaten bir yoldaş var
            Aktif = true;
            _sahip = true;
            DontDestroyOnLoad(gameObject);
            _sr = GetComponent<SpriteRenderer>();
            _book = GetComponent<Ulak.Core.SpriteFlipbook>();
            SceneManager.sceneLoaded += OnSceneLoaded;
            FindPlayer();
        }

        private void OnDestroy()
        {
            if (!_sahip) return;
            Aktif = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => FindPlayer();

        private void FindPlayer()
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            _player = go != null ? go.transform : null;
            if (_sr != null) _sr.enabled = _player != null; // oyuncu yoksa gizlen

            if (_player != null)
            {
                _lastPlayerX = _player.position.x;
                transform.position = Hedef(); // yeni sahnede ışınlanarak yetiş
            }
        }

        private Vector3 Hedef()
        {
            return new Vector3(
                _player.position.x - _dir * Mathf.Abs(followOffset.x),
                _player.position.y + followOffset.y, 0f);
        }

        private void LateUpdate()
        {
            if (_player == null) { FindPlayer(); return; }

            // Karakterin gittiği yönü izle → hep ARKASINDA kal.
            float dx = _player.position.x - _lastPlayerX;
            if (Mathf.Abs(dx) > 0.001f) _dir = dx > 0f ? 1 : -1;
            _lastPlayerX = _player.position.x;

            var hedef = Hedef();
            transform.position = Vector3.Lerp(
                transform.position, hedef, followLerp * Time.deltaTime);

            if (_sr != null) _sr.flipX = _dir < 0;

            // Karakter durunca kurt da dursun: oyuncu hareketsiz VE kurt
            // hedefine yetişmişse idle, aksi halde koşu animasyonu.
            if (_book != null)
            {
                bool kosuyor = Mathf.Abs(dx) > 0.0008f
                               || Vector2.Distance(transform.position, hedef) > 0.4f;
                _book.SetMoving(kosuyor);
            }
        }
    }
}
