using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Boşluğa düşünce ölüm (greybox v1):
    /// Karakter belirlenen Y eşiğinin altına düşerse sahne baştan yüklenir
    /// (hız, puan, konum — her şey sıfırlanır).
    /// </summary>
    public class FallDeath : MonoBehaviour
    {
        [Tooltip("Bu Y değerinin altına düşen karakter ölür ve sahne yeniden başlar.")]
        [SerializeField] private float fallY = -8f;

        private void Update()
        {
            if (transform.position.y < fallY)
                Restart();
        }

        private void Restart()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0)
                SceneManager.LoadScene(scene.buildIndex);
            else
                SceneManager.LoadScene(scene.name); // build settings dışındaysa isimle dene
        }
    }
}
