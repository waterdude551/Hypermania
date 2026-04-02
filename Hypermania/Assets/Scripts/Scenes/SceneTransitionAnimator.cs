using System.Collections;
using UnityEngine;

namespace Scenes
{
    public class SceneTransitionAnimator : MonoBehaviour
    {
        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private float _fadeInTime;

        [SerializeField]
        private float _fadeOutTime;

        public IEnumerator FadeInBlack()
        {
            yield return FadeTo(1f, _fadeInTime);
        }

        public IEnumerator FadeOutBlack()
        {
            yield return FadeTo(0f, _fadeOutTime);
        }

        public IEnumerator FadeTo(float targetAlpha, float duration)
        {
            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }
            _canvasGroup.alpha = targetAlpha;
        }
    }
}
