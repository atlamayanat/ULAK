using UnityEngine;

namespace Ulak.Gameplay
{
    /// <summary>
    /// Gündüz modu HUD'u: sol üstte puan + anlık hız.
    /// Greybox sadeliği için OnGUI — Canvas gerekmez.
    /// </summary>
    public class ScoreHUD : MonoBehaviour
    {
        private HorseController _horse;
        private GUIStyle _big;
        private GUIStyle _small;

        private void Awake()
        {
            _horse = GetComponent<HorseController>();
        }

        private void OnGUI()
        {
            if (_big == null)
            {
                _big = new GUIStyle
                {
                    fontSize = Mathf.Max(20, Screen.height / 24),
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, 0.85f, 0.2f) }
                };
                _small = new GUIStyle
                {
                    fontSize = Mathf.Max(14, Screen.height / 40),
                    normal = { textColor = Color.white }
                };
            }

            // Gölge + puan
            string text = $"Puan: {RideScore.Score}";
            var pos = new Rect(18, 14, 320, 44);
            var old = _big.normal.textColor;
            _big.normal.textColor = Color.black;
            GUI.Label(new Rect(pos.x + 2, pos.y + 2, pos.width, pos.height), text, _big);
            _big.normal.textColor = old;
            GUI.Label(pos, text, _big);

            if (_horse != null)
                GUI.Label(new Rect(18, pos.y + pos.height + 2, 320, 30),
                    $"Hız: {_horse.CurrentSpeed:F1} / {_horse.MaxSpeed:F0}", _small);
        }
    }
}
