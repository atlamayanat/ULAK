using System.Collections;
using UnityEngine;

namespace Ulak.Core
{
    /// <summary>
    /// Hasar alınca SpriteRenderer'ı kısa süre renklendirir (geri besleme).
    /// Greybox'ta da çalışır; gerçek sprite gelince aynı kalır.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class DamageFlash : MonoBehaviour
    {
        [SerializeField] private Color flashColor = Color.red;
        [SerializeField] private float duration = 0.08f;

        private SpriteRenderer _sr;
        private Color _original;
        private Coroutine _routine;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _original = _sr.color;
        }

        public void Flash()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            _sr.color = flashColor;
            yield return new WaitForSeconds(duration);
            _sr.color = _original;
            _routine = null;
        }
    }
}
