using UnityEngine;

namespace Ulak.Core
{
    /// <summary>
    /// Karo dokulu kutular için yamulma sigortası (greybox v1).
    ///  - Editörde objeyi Scale ile uzatırsan, ölçeği otomatik olarak
    ///    SpriteRenderer.size'a aktarır → doku gerilmek yerine KARO KARO çoğalır.
    ///  - BoxCollider2D boyutunu her zaman görsel boyutla eşit tutar.
    /// Bölüm tasarımında Rect Tool (T) ya da Scale — ikisi de güvenli hale gelir.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class TiledBoxSync : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private BoxCollider2D _col;

        private void OnEnable()
        {
            _sr = GetComponent<SpriteRenderer>();
            _col = GetComponent<BoxCollider2D>();
        }

        private void Update()
        {
            if (_sr == null || _sr.drawMode == SpriteDrawMode.Simple) return;

            // Scale ile uzatılmışsa: ölçeği size'a aktar (doku yamulmasın).
            Vector3 s = transform.localScale;
            if (Mathf.Abs(s.x - 1f) > 0.0001f || Mathf.Abs(s.y - 1f) > 0.0001f)
            {
                Vector2 size = _sr.size;
                _sr.size = new Vector2(
                    Mathf.Max(0.01f, size.x * Mathf.Abs(s.x)),
                    Mathf.Max(0.01f, size.y * Mathf.Abs(s.y)));
                transform.localScale = new Vector3(1f, 1f, s.z);
            }

            // Hitbox görselle birebir aynı kalsın.
            if (_col != null && (_col.size != _sr.size || _col.offset != Vector2.zero))
            {
                _col.size = _sr.size;
                _col.offset = Vector2.zero;
            }
        }
    }
}
