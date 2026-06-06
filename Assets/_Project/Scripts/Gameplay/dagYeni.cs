using UnityEngine;

public class DagYonetici : MonoBehaviour
{
    public Transform[] daglar;

    [Header("Parallax Ayarý")]
    [Range(0f, 1f)]
    public float parallaxCarpani = 0.8f;

    [Header("Boþluk Kapatma")]
    [Tooltip("Daðlarýn arasýna giren ince boþluðu kapatmak için iç içe geçme payý (Örn: 0.1 veya 0.2)")]
    public float ortusmePayi = 0.1f;

    private float genislik;
    private Transform kamera;
    private float sonKameraX;

    void Start()
    {
        kamera = Camera.main.transform;
        genislik = daglar[0].GetComponent<SpriteRenderer>().bounds.size.x;
        sonKameraX = kamera.position.x;
    }

    void Update()
    {
        float kameraFarki = kamera.position.x - sonKameraX;

        for (int i = 0; i < daglar.Length; i++)
        {
            // Daðlarý kameranýn hýzýyla oranlý hareket ettir
            float dagHareketi = kameraFarki * parallaxCarpani;
            daglar[i].Translate(Vector3.right * dagHareketi);

            // Dað kameranýn solundan tamamen çýktý mý?
            if (daglar[i].position.x < kamera.position.x - genislik)
            {
                // Sahnede o an "en saðda" olan daðý buluyoruz
                float enSagdakiX = daglar[i].position.x;
                for (int j = 0; j < daglar.Length; j++)
                {
                    if (daglar[j].position.x > enSagdakiX)
                    {
                        enSagdakiX = daglar[j].position.x;
                    }
                }

                // Çýkan daðý al, en saðdaki daðýn tam sonuna koy ama 'ortusmePayi' kadar geri çek (iç içe sok)
                Vector3 yeniPozisyon = daglar[i].position;
                yeniPozisyon.x = enSagdakiX + genislik - ortusmePayi;
                daglar[i].position = yeniPozisyon;
            }
        }

        sonKameraX = kamera.position.x;
    }
}