using UnityEngine;

public class SuYonetici : MonoBehaviour
{
    public Transform[] sular;

    [Header("Hareket Ayarlarý")]
    [Range(0f, 1f)]
    public float parallaxCarpani = 0.9f;

    private float genislik;
    private Transform kamera;
    private Vector3 sonKameraPozisyonu;

    void Start()
    {
        kamera = Camera.main.transform;
        // 0.05f yýrtýlmalarý engeller
        genislik = sular[0].GetComponent<SpriteRenderer>().bounds.size.x - 0.05f;
        sonKameraPozisyonu = kamera.position;
    }

    void LateUpdate()
    {
        float kameraHareketX = kamera.position.x - sonKameraPozisyonu.x;

        for (int i = 0; i < sular.Length; i++)
        {
            // Parallax hareketi
            sular[i].position += new Vector3(kameraHareketX * parallaxCarpani, 0, 0);

            // Su parçasý kameranýn ÇOK SOLUNDA kaldýysa (Ekrandan iyice çýktýysa)
            if (kamera.position.x - sular[i].position.x > (genislik * 1.5f))
            {
                // En saðdaki parçayý bul
                float enSagdakiX = sular[0].position.x;
                for (int j = 1; j < sular.Length; j++)
                {
                    if (sular[j].position.x > enSagdakiX) enSagdakiX = sular[j].position.x;
                }

                // Onu en saðdaki parçanýn bitiþiðine milimetrik yapýþtýr
                sular[i].position = new Vector3(enSagdakiX + genislik, sular[i].position.y, sular[i].position.z);
            }
            // Su parçasý kameranýn ÇOK SAÐINDA kaldýysa (Karakter sola koþuyorsa)
            else if (sular[i].position.x - kamera.position.x > (genislik * 1.5f))
            {
                // En soldaki parçayý bul
                float enSoldakiX = sular[0].position.x;
                for (int j = 1; j < sular.Length; j++)
                {
                    if (sular[j].position.x < enSoldakiX) enSoldakiX = sular[j].position.x;
                }

                // Onu en soldaki parçanýn bitiþiðine milimetrik yapýþtýr
                sular[i].position = new Vector3(enSoldakiX - genislik, sular[i].position.y, sular[i].position.z);
            }
        }

        sonKameraPozisyonu = kamera.position;
    }
}