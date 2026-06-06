using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    private float length, startpos;
    public GameObject cam;
    public float parallaxEffect; // Ne kadar yavaþ hareket edeceði (Örn: 0.8)

    void Start()
    {
        startpos = transform.position.x;
        // Objenin geniþliðini otomatik ölçer (Sonsuz döngü için þart)
        length = GetComponent<SpriteRenderer>().bounds.size.x;
        
        // Eðer kamera atanmamýþsa, sahnedeki Main Camera'yý otomatik bulur
        if (cam == null) cam = Camera.main.gameObject;
    }

    void Update()
    {
        // Hollow Knight tarzý derinlik matematiði
        float temp = (cam.transform.position.x * (1 - parallaxEffect));
        float dist = (cam.transform.position.x * parallaxEffect);

        transform.position = new Vector3(startpos + dist, transform.position.y, transform.position.z);

        // Sonsuz Döngü: Kamera daðý geçerse, daðý bir sonrakinin peþine ýþýnla
        if (temp > startpos + length)
        {
            startpos += length;
        }
        else if (temp < startpos - length)
        {
            startpos -= length;
        }
    }
}