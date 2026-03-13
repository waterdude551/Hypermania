using Game.View.Overlay;
using UnityEngine;

namespace Scenes.Menus.MainMenu
{
    [RequireComponent(typeof(MusicReactive))]
    public class MainMenuLogo : MonoBehaviour
    {
        private MusicReactive _musicReactive;
        private RectTransform _rectTransform;

        private Vector3 _baseScale;

        [SerializeField]
        private float _scaleFactor;

        [SerializeField]
        private float _minZRotation;

        [SerializeField]
        private float _maxZRotation;

        [SerializeField]
        private float _rotationPeriod;

        public void Awake()
        {
            _musicReactive = GetComponent<MusicReactive>();
            _rectTransform = GetComponent<RectTransform>();
            _baseScale = _rectTransform.localScale;
        }

        public void Update()
        {
            float musicValue = _musicReactive.GetMusicValue();
            _rectTransform.localScale = _baseScale + musicValue * _scaleFactor * _baseScale;
            transform.rotation = Quaternion.Euler(
                0f,
                0f,
                (0.5f + 0.5f * Mathf.Sin(Time.realtimeSinceStartup * 2 * Mathf.PI / _rotationPeriod))
                    * (_maxZRotation - _minZRotation)
                    + _minZRotation
            );
        }
    }
}
