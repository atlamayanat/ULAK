using UnityEngine;
using UnityEngine.EventSystems; // Arayüz etkileþimlerini yakalamak için gerekli

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;
    public float hoverScaleMultiplier = 1.15f; // Buton %15 büyüyecek

    void Start()
    {
        // Butonun orijinal boyutunu hafýzaya alýyoruz
        originalScale = transform.localScale;
    }

    // Fare butonun üzerine geldiðinde çalýþýr
    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = originalScale * hoverScaleMultiplier;

        // EÐER ÝSTERSENÝZ: Ýleride buraya kýlýç çekme veya rüzgar sesi kodu eklenebilir
    }

    // Fare butonun üzerinden çekildiðinde çalýþýr
    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = originalScale;
    }
}