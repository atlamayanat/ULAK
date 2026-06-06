using UnityEngine;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Kesintisiz su akıntısı (greybox v1).
    ///  - Tek kaynak parçadan runtime'da birebir klonlar üretir → tüm
    ///    segmentler AYNI genişliktedir, boşluk/yırtılma matematiksel olarak imkânsız.
    ///  - Sürekli sola akar (karakter dursa bile).
    ///  - Ekranın solunda tamamen kaybolan segment, toplam genişlik kadar
    ///    sağa ışınlanır (float-kesin tur atma) — her iki yönde de çalışır.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SuAkinti : MonoBehaviour
    {
        [Tooltip("Akıntı hızı (birim/sn, sola doğru).")]
        [SerializeField] private float flowSpeed = 1.0f;
        [Tooltip("Toplam segment sayısı (ekran genişliğini bolca aşmalı).")]
        [SerializeField] private int copies = 4;
        [SerializeField] private Camera targetCamera;

        private Transform[] _segs;
        private float _w;

        private void Start()
        {
            if (targetCamera == null) targetCamera = Camera.main;

            var src = GetComponent<SpriteRenderer>();
            // Mikro bindirme (0.02): kenarlarda tek piksellik dikiş çizgisini yok eder.
            _w = src.bounds.size.x - 0.02f;

            _segs = new Transform[Mathf.Max(2, copies)];
            _segs[0] = transform;
            for (int i = 1; i < _segs.Length; i++)
            {
                var clone = Instantiate(gameObject, transform.parent);
                Destroy(clone.GetComponent<SuAkinti>()); // tek yönetici yeter
                clone.name = gameObject.name + "_seg" + i;
                clone.transform.position = transform.position + Vector3.right * (_w * i);
                _segs[i] = clone.transform;
            }
        }

        private void LateUpdate()
        {
            if (_segs == null || targetCamera == null) return;

            float camX = targetCamera.transform.position.x;
            float half = targetCamera.orthographicSize * targetCamera.aspect;
            float total = _w * _segs.Length;
            Vector3 flow = Vector3.left * (flowSpeed * Time.deltaTime);

            foreach (var s in _segs)
            {
                s.position += flow;

                // Tamamen ekran dışına çıkan segment tam tur atar (boşluksuz).
                if (s.position.x + _w * 0.5f < camX - half - 1f)
                    s.position += Vector3.right * total;
                else if (s.position.x - _w * 0.5f > camX + half + 1f)
                    s.position += Vector3.left * total;
            }
        }
    }
}
