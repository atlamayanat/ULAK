using UnityEngine;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Basit paralaks katmanı (greybox v1).
    /// Kamera hareketinin belirli bir oranını takip eder:
    /// factor 1 = kameraya yapışık (sonsuz uzak), 0 = sabit dünya objesi.
    /// Uzak dağlar için ~0.85-0.95 iyi sonuç verir.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("Kamera hareketini takip oranı (1 = tam takip/en uzak).")]
        [SerializeField, Range(0f, 1f)] private float factor = 0.9f;
        [Tooltip("Takip edilecek kamera. Boşsa Camera.main.")]
        [SerializeField] private Camera targetCamera;

        private Vector3 _startPos;
        private float _camStartX;

        private void Start()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            _startPos = transform.position;
            if (targetCamera != null) _camStartX = targetCamera.transform.position.x;
        }

        private void LateUpdate()
        {
            if (targetCamera == null) return;
            float camDeltaX = targetCamera.transform.position.x - _camStartX;
            transform.position = new Vector3(
                _startPos.x + camDeltaX * factor,
                _startPos.y,
                _startPos.z);
        }
    }
}
