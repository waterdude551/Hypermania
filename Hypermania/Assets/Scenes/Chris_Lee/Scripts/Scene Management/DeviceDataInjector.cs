/*
using System;
using System.Collections;
using Game;
using Game.Runners;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameRunnerMode {
    Training,
    Local,
    Online
}

//this entire class is fcked lowkey :skull:
public class DeviceDataInjector : MonoBehaviour
{
    #region Global
    public static DeviceDataInjector Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += UpdateGameDevices;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= UpdateGameDevices;
        }
    }

    #endregion Global

    private InputDevice _player1Device;
    private InputDevice _player2Device;
    private GameRunnerMode _gameMode;

    public void RegisterDevices(InputDevice player1, InputDevice player2, GameRunnerMode gameMode) {
        _player1Device = player1;
        _player2Device = player2;
        _gameMode = gameMode;
    }
    
    //fuck
    public void UpdateGameDevices(Scene scene, LoadSceneMode sceneMode) {
        StartCoroutine(UpdateGameDeviceAction(scene, sceneMode));
    }

    private IEnumerator UpdateGameDeviceAction(Scene scene, LoadSceneMode sceneMode) {
        yield return new WaitForSeconds(1f);
        if (scene.name == SceneDataBank.BATTLE) {
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            GameRunner runner = null;
            
            switch (_gameMode) {
                case GameRunnerMode.Local:
                    //if (_player1Device == null || _player2Device == null) yield break;
                    runner = gameManager.GetComponentInChildren<LocalRunner>();
                    runner._options.LocalPlayers[0].InputDevice = _player1Device;
                    runner._options.LocalPlayers[1].InputDevice = _player2Device;
                    runner._options.Players[0].HealOnActionable = false;
                    runner._options.Players[1].HealOnActionable = false;
                    break;
                case GameRunnerMode.Training:
                    //if (_player1Device == null) yield break;
                    runner = gameManager.GetComponentInChildren<LocalRunner>();
                    runner._options.LocalPlayers[0].InputDevice = _player1Device;
                    runner._options.LocalPlayers[1].InputDevice = _player2Device;
                    runner._options.Players[0].HealOnActionable = true;
                    runner._options.Players[1].HealOnActionable = true;
                    break;
                /*case GameMode.Online:
                    runner = gameManager.GetComponentInChildren<MultiplayerRunner>();
                    break;#1#
            }
            
            if (gameManager != null && runner != null) {
                gameManager.Runner = runner;
                gameManager.StartLocalGame();
            }
        }
    }
}
*/
