using System;
using System.Collections.Generic;
using Design;
using Game.Sim;
using UnityEngine;

namespace Game.View
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Conductor))]
    public class GameView : MonoBehaviour
    {
        private Conductor _conductor;
        public FighterView[] Fighters => _fighters;

        private FighterView[] _fighters;
        private ManiaView[] _manias;
        private CharacterConfig[] _characters;
        private float Zoom = 5f;

        public ManiaViewConfig Config;

        [SerializeField]
        private DJ_CameraControl CameraControl;
        [SerializeField]
        private DJ_CameraIndicator CameraIndicator;

        public void Init(CharacterConfig[] characters)
        {
            _conductor = GetComponent<Conductor>();
            if (_conductor == null)
            {
                throw new InvalidOperationException(
                    "Conductor was null. Did you forget to assign a conductor component to the GameView?"
                );
            }
            _fighters = new FighterView[characters.Length];
            _manias = new ManiaView[characters.Length];
            _characters = characters;
            for (int i = 0; i < characters.Length; i++)
            {
                _fighters[i] = Instantiate(_characters[i].Prefab);
                _fighters[i].name = "Fighter View";
                _fighters[i].transform.SetParent(transform, true);
                _fighters[i].Init(characters[i]);

                float xPos = i - ((float)characters.Length - 1) / 2;
                GameObject maniaView = new GameObject("Mania View");
                _manias[i] = maniaView.AddComponent<ManiaView>();
                _manias[i].transform.SetParent(transform, true);
                _manias[i].Init(new Vector2(8f * xPos, 0f), Config);
            }
            _conductor.Init();
        }

        public void Render(in GameState state, GlobalConfig config)
        {
            for (int i = 0; i < _characters.Length; i++)
            {
                _fighters[i].Render(state.Frame, state.Fighters[i]);
                _manias[i].Render(state.Frame, state.Manias[i]);
            }
            _conductor.RequestSlice(state.Frame);

            List<Vector2> interestPoints = new List<Vector2>();
            for (int i = 0; i < _characters.Length; i++)
            {
                interestPoints.Add((Vector2)state.Fighters[i].Position);
            }
            // Debug testing for zoom, remove later
            if (Input.GetKeyDown(KeyCode.P))
            {
                if (Zoom == 5f)
                {
                    Zoom = 4f;
                }
                else
                {
                    Zoom = 5f;
                }
            }
            CameraControl.UpdateCamera(interestPoints, Zoom, Time.deltaTime);
            CameraIndicator.Track(_fighters);
        }

        public void DeInit()
        {
            for (int i = 0; i < _characters.Length; i++)
            {
                _fighters[i].DeInit();
                Destroy(_fighters[i].gameObject);
                _manias[i].DeInit();
                Destroy(_manias[i].gameObject);
            }
            _fighters = null;
            _characters = null;
        }
    }
}
