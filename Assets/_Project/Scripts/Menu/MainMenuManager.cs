using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // Oyunundaki tüm sahneleri buraya Enum olarak tanýmlýyoruz.
    // Ýsimlerin, Unity'deki sahne isimleriyle birebir ayný olmasý gerekir.
    public enum SceneList
    {
        MainMenu,
        GameScene,
        CreditsMenu,
        UmayKoy
    }

    [Header("Sahne Ayarlarý")]
    [Tooltip("Butona basýldýðýnda hangi sahneye gidilecek?")]
    public SceneList targetScene;

    public void PlayGame()
    {
        // Seçilen Enum deðerini otomatik olarak String'e çevirip sahneyi yüklüyoruz
        SceneManager.LoadScene(targetScene.ToString());
    }

    public void QuitGame()
    {
        Debug.Log("Oyun kapatýlýyor...");
        Application.Quit();
    }
}