using UnityEngine;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Greybox takip kamerası. Yol modunda oyuncuyu yatayda takip eder.
    /// İleride Pixel Perfect Camera + sınırlandırma eklenecek (bkz. ULAK_PLAN.md §5).
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(2f, 0f, -10f);
        [Tooltip("Takip yumuşaklığı (0 = anında, büyük = yumuşak).")]
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private bool followY = false;

        private Vector3 _velocity;

        private void Start()
        {
            // Kendi kendine bağlanma: editor wiring'i boş kalsa bile oyuncuyu bul.
            if (target == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) target = p.transform;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = target.position + offset;
            if (!followY) desired.y = transform.position.y;
            desired.z = offset.z;

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
        }
    }
}
