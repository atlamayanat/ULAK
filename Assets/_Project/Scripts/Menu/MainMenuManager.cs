using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // Oyunundaki t�m sahneleri buraya Enum olarak tan�ml�yoruz.
    // �simlerin, Unity'deki sahne isimleriyle birebir ayn� olmas� gerekir.
    public enum SceneList
    {
        MainMenu,
        GameScene,
        CreditsMenu,
        UmayKoy,
        BaslangicIntro
    }

    [Header("Sahne Ayarlar�")]
    [Tooltip("Butona bas�ld���nda hangi sahneye gidilecek?")]
    public SceneList targetScene;

    public void PlayGame()
    {
        // Se�ilen Enum de�erini otomatik olarak String'e �evirip sahneyi y�kl�yoruz
        SceneManager.LoadScene(targetScene.ToString());
    }

    public void QuitGame()
    {
        Debug.Log("Oyun kapat�l�yor...");
        Application.Quit();
    }
}