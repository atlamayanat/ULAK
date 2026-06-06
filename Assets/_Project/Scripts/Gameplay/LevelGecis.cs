using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LevelGecis : MonoBehaviour
{
    [Header("Gidilecek Sahne")]
    public string sonrakiSahneAdi;

    [Header("Sinematik Geçiþ Ayarlarý")]
    public Image fadePaneli; // Siyah panelin Image bileþeni buraya gelecek
    public float fadeHizi = 1.5f; // Kararma ve açýlma hýzý (Saniye bazýnda)

    private bool yukleniyor = false;
    private void Awake()
    {
        // SAHNE YÜKLENDÝÐÝ MÝLÝSANÝYE ÇALIÞIR (Kamera daha görüntüyü çizmeden önce)
        if (fadePaneli != null)
        {
            fadePaneli.gameObject.SetActive(true); // Paneli zorla aç

            Color ilkRenk = fadePaneli.color;
            ilkRenk.a = 1f; // Rengi anýnda tam (katý) siyah yap
            fadePaneli.color = ilkRenk;
        }
    }

    private void Start()
    {
        // KAMERA KAYDA GÝRDÝÐÝ AN ÇALIÞIR (Açýlma animasyonunu baþlat)
        if (fadePaneli != null)
        {
            StartCoroutine(EkraniGörünürYap());
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // SAHNE BÝTERKEN: Oyuncu duvara çarpýnca ekran kararýp yeni sahneye geçer (Fade Out)
        if (collision.CompareTag("Player") && !yukleniyor)
        {
            StartCoroutine(EkraniKarartVeSahneDegistir());
        }
    }

    // Ekraný Siyahlýktan Kurtarma (Fade In)
    IEnumerator EkraniGörünürYap()
    {
        float alfa = 1f; // Tam siyah baþla

        while (alfa > 0f)
        {
            alfa -= Time.deltaTime * fadeHizi;

            Color yeniRenk = fadePaneli.color;
            yeniRenk.a = Mathf.Clamp01(alfa);
            fadePaneli.color = yeniRenk;

            yield return null;
        }

        // Ekran tamamen açýlýnca paneli kapatýyoruz
        fadePaneli.gameObject.SetActive(false);
    }

    // Ekraný Simsiyah Yapma (Fade Out)
    IEnumerator EkraniKarartVeSahneDegistir()
    {
        yukleniyor = true;
        fadePaneli.gameObject.SetActive(true); // Paneli geri aç

        float alfa = 0f; // Saydam baþla

        while (alfa < 1f)
        {
            alfa += Time.deltaTime * fadeHizi;

            Color yeniRenk = fadePaneli.color;
            yeniRenk.a = Mathf.Clamp01(alfa);
            fadePaneli.color = yeniRenk;

            yield return null;
        }

        // Ekran tamamen karardýktan sonra yeni sahneyi yükle
        SceneManager.LoadScene(sonrakiSahneAdi);
    }
}