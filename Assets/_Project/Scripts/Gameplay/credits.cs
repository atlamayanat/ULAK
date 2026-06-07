using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsRoller : MonoBehaviour
{
    [Header("Kayma Ayarlarý")]
    [Tooltip("Yazýnýn yukarý kayma hýzý")]
    public float kaymaHizi = 100f;

    [Tooltip("Yazý hangi Y eksenine ulaþýnca ana menüye dönsün? (Örn: 1500)")]
    public float bitisYPosisyonu = 1500f;

    [Header("Sahne Ayarlarý")]
    [Tooltip("Dönülecek ana menü sahnesinin tam adý")]
    public string anaMenuSahneAdi = "MainMenu";

    private RectTransform _rectTransform;

    void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        // Metni her karede Y ekseninde yukarý doðru kaydýr
        _rectTransform.anchoredPosition += Vector2.up * kaymaHizi * Time.deltaTime;

        // Yazý ekranýn üstünden tamamen çýktý mý?
        if (_rectTransform.anchoredPosition.y >= bitisYPosisyonu)
        {
            AnaMenuyeDon();
        }

        // Oyuncu beklemek istemezse Space veya ESC ile atlayabilsin
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
        {
            AnaMenuyeDon();
        }
    }

    void AnaMenuyeDon()
    {
        SceneManager.LoadScene(anaMenuSahneAdi);
    }
}