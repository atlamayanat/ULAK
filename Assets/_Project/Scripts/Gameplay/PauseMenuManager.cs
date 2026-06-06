using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Slider'ý kullanabilmek için bu þart

public class PauseManager : MonoBehaviour
{
    [Header("UI Panelleri")]
    public GameObject pauseBackground;
    public GameObject pauseMenuUI;
    public GameObject settingsMenuUI; // Yeni Ayarlar Paneli

    [Header("Ayarlar")]
    public Slider volumeSlider; // Ekrana koyduðumuz Ses Çubuðu

    private bool isPaused = false;

    void Start()
    {
        // Oyun baþladýðýnda, slider'ýn çubuðunu oyunun mevcut ses seviyesine eþitle
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Eðer ayarlar menüsü açýksa ESC'ye basýnca direkt oyuna dönmesin, Pause menüye dönsün
            if (settingsMenuUI.activeSelf)
            {
                CloseSettings();
            }
            else if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pauseBackground.SetActive(false);
        pauseMenuUI.SetActive(false);
        settingsMenuUI.SetActive(false); // Garanti olsun diye bunu da kapatýyoruz

        Time.timeScale = 1f;
        isPaused = false;
    }

    void Pause()
    {
        pauseBackground.SetActive(true);
        pauseMenuUI.SetActive(true);

        Time.timeScale = 0f;
        isPaused = true;
    }

    // Ayarlar butonuna basýlýnca çalýþacak
    public void OpenSettings()
    {
        pauseMenuUI.SetActive(false); // Ana menüyü gizle
        settingsMenuUI.SetActive(true); // Ayarlarý göster
    }

    // Geri butonuna basýlýnca çalýþacak
    public void CloseSettings()
    {
        settingsMenuUI.SetActive(false); // Ayarlarý gizle
        pauseMenuUI.SetActive(true); // Ana menüyü geri getir
    }

    // Slider hareket ettikçe tetiklenecek (Dinamik Float)
    public void SetVolume(float volume)
    {
        AudioListener.volume = volume; // Tüm oyunun ses seviyesini ayarlar (0.0 ile 1.0 arasý)
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}