using UnityEngine;

namespace Ulak.Core
{
    /// <summary>
    /// Hasar alabilen her şeyin (oyuncu, düşman, kırılabilir engel) paylaştığı arayüz.
    /// Kılıç, temas hasarı vb. sadece bu arayüzü bilir; somut tipe bağlı değildir.
    /// </summary>
    public interface IDamageable
    {
        bool IsAlive { get; }

        /// <param name="amount">Verilecek hasar.</param>
        /// <param name="knockback">Uygulanacak geri savurma kuvveti (dünya uzayında, dönüş yok = 0).</param>
        void TakeDamage(int amount, Vector2 knockback);
    }
}
