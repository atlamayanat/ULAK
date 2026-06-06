using UnityEngine;

public class SuYonetici : MonoBehaviour
{
    public Transform[] sular;

    [Header("Parallax ve Akýþ")]
    [Range(0f, 1f)]
    public float parallaxCarpani = 0.9f;
    [Tooltip("Karakter dursa bile suyun kendi kendine akma hýzý")]
    public float sabitAkimHizi = -0.5f; // Eksi deðer sola, artý deðer saða akýtýr

    [Header("Boþluk Kapatma")]
    public float ortusmePayi = 0.1f;

    private float genislik;
    private Transform kamera;
    private float sonKameraX;

    void Start()
    {
        kamera = Camera.main.transform;
        genislik = sular[0].GetComponent<SpriteRenderer>().bounds.size.x;
        sonKameraX = kamera.position.x;
    }

    void Update()
    {
        float kameraFarki = kamera.position.x - sonKameraX;

        for (int i = 0; i < sular.Length; i++)
        {
            // Hem kameraya göre parallax yap, hem de sabit hýzda akmaya devam et
            float hareket = (kameraFarki * parallaxCarpani) + (sabitAkimHizi * Time.deltaTime);
            sular[i].Translate(Vector3.right * hareket);

            // Sola doðru ekrandan çýktýysa saða ýþýnla
            if (sular[i].position.x < kamera.position.x - genislik)
            {
                float enSagdakiX = sular[i].position.x;
                for (int j = 0; j < sular.Length; j++)
                    if (sular[j].position.x > enSagdakiX) enSagdakiX = sular[j].position.x;

                Vector3 yeniPoz = sular[i].position;
                yeniPoz.x = enSagdakiX + genislik - ortusmePayi;
                sular[i].position = yeniPoz;
            }
            // Saða doðru ekrandan çýktýysa sola ýþýnla (Karakter geri koþarken veya su saða akarken)
            else if (sular[i].position.x > kamera.position.x + genislik)
            {
                float enSoldakiX = sular[i].position.x;
                for (int j = 0; j < sular.Length; j++)
                    if (sular[j].position.x < enSoldakiX) enSoldakiX = sular[j].position.x;

                Vector3 yeniPoz = sular[i].position;
                yeniPoz.x = enSoldakiX - genislik + ortusmePayi;
                sular[i].position = yeniPoz;
            }
        }

        sonKameraX = kamera.position.x;
    }
}