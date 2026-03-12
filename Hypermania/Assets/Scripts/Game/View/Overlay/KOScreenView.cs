using Game.Sim;
using TMPro;
using UnityEngine;

namespace Game.View.Overlay
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class KOScreenView : MonoBehaviour
    {
        TextMeshProUGUI _text;

        public void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        public void Render(in GameState state)
        {
            if (
                state.GameMode == GameMode.RoundEnd
                && state.RealFrame - state.RoundEndStart > GameManager.ROLLBACK_FRAMES
            )
            {
                _text.SetText("KO");
            }
            else
            {
                _text.SetText("");
            }
        }
    }
}
