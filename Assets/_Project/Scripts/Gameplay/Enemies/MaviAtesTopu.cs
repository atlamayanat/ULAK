using UnityEngine;
using Ulak.Core;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Gulyabani'nin mavi ateş topu (greybox v1).
    ///  - YÖRÜNGE: yoğunlaşma sırasında sahibinin yanında süzülür.
    ///  - TAKİP: fırlatılınca oyuncuyu ORTA hızda ve GEVŞEK biçimde izler
    ///    (sınırlı dönüş + salınım + aralıklı hedef güncelleme) — kaçılabilir.
    ///  - Ömrü dolunca kendiliğinden söner; kılıçla vurulursa patlar
    ///    (IDamageable, Enemy katmanı); oyuncuya çarparsa hasar verir.
    /// </summary>
    public class MaviAtesTopu : MonoBehaviour, IDamageable
    {
        private Transform _sahip;     // yörünge merkezi (Gulyabani)
        private Vector2 _ofset;       // sahibe göre süzülme noktası
        private Transform _hedef;     // oyuncu
        private float _hiz;
        private int _hasar;
        private float _omur;
        private float _fazSeed;       // küre başına farklı salınım fazı

        private bool _firladi;
        private bool _oldu;
        private float _dogum;
        private Vector2 _yon;
        private Vector2 _hedefNokta;
        private float _hedefGuncelleZamani;
        private SpriteRenderer _sr;
        private Collider2D _col;

        public bool IsAlive => !_oldu;

        /// <summary>Gulyabani tarafından üretim anında çağrılır.</summary>
        public void Kur(Transform sahip, Vector2 ofset, Transform hedef,
            float hiz, int hasar, float omur, float fazSeed)
        {
            _sahip = sahip;
            _ofset = ofset;
            _hedef = hedef;
            _hiz = hiz;
            _hasar = hasar;
            _omur = omur;
            _fazSeed = fazSeed;
            _dogum = Time.time; // fırlatılmazsa da (boss ölürse) süresi dolsun
            _sr = GetComponent<SpriteRenderer>();
            _col = GetComponent<Collider2D>();
        }

        /// <summary>Yoğunlaşma bitti — takibe başla.</summary>
        public void Firlat()
        {
            _firladi = true;
            _dogum = Time.time; // takip ömrü fırlatmadan itibaren sayılır
            if (_hedef != null)
                _yon = ((Vector2)_hedef.position - (Vector2)transform.position).normalized;
        }

        private void Update()
        {
            if (_oldu) return;

            // --- YÖRÜNGE: sahibinin yanında hafif salınımla bekle ---
            if (!_firladi)
            {
                // Boss öldü/yok oldu ya da fırlatma hiç gelmediyse yetim kalma.
                if (_sahip == null || Time.time - _dogum > 6f) { Sondur(); return; }
                Vector2 bob = Vector2.up * Mathf.Sin(Time.time * 5f + _fazSeed) * 0.12f;
                transform.position = (Vector2)_sahip.position + _ofset + bob;
                return;
            }

            // --- TAKİP: gevşek güdüm ---
            if (_hedef != null && Time.time >= _hedefGuncelleZamani)
            {
                _hedefNokta = (Vector2)_hedef.position + Vector2.up * 0.4f;
                _hedefGuncelleZamani = Time.time + 0.3f; // tembel yeniden hedefleme
            }

            Vector2 istenen = (_hedefNokta - (Vector2)transform.position).normalized;
            _yon = Vector2.Lerp(_yon, istenen, 2.5f * Time.deltaTime).normalized; // sınırlı dönüş
            Vector2 dik = new Vector2(-_yon.y, _yon.x);
            Vector2 salinim = dik * (Mathf.Sin(Time.time * 6f + _fazSeed) * 0.35f * _hiz * 0.2f);

            transform.position += (Vector3)((_yon * _hiz + salinim) * Time.deltaTime);

            if (Time.time - _dogum > _omur)
                Sondur(); // kaçmayı başardın — küre söndü
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_oldu || !_firladi) return;
            if (other.gameObject.layer == gameObject.layer) return; // kendi takımı

            if (other.CompareTag("Player"))
            {
                var dmg = other.GetComponentInParent<IDamageable>();
                if (dmg != null && dmg.IsAlive)
                    dmg.TakeDamage(_hasar, _yon * 5f);
                Sondur();
                return;
            }

            // Zemine/duvara çarptı.
            if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
                Sondur();
        }

        /// <summary>Kılıç/stomp ile patlatılabilir.</summary>
        public void TakeDamage(int amount, Vector2 knockback) => Sondur();

        private void Sondur()
        {
            if (_oldu) return;
            _oldu = true;
            if (_col != null) _col.enabled = false;
            StartCoroutine(SonusAnimasyonu());
        }

        private System.Collections.IEnumerator SonusAnimasyonu()
        {
            float t = 0f;
            Color baslangic = _sr != null ? _sr.color : Color.white;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                if (_sr != null)
                {
                    var c = baslangic;
                    c.a = Mathf.Lerp(baslangic.a, 0f, t / 0.25f);
                    _sr.color = c;
                    transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.4f, t / 0.25f); // patlayıp dağılma
                }
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
