using System;
using BB.Resources;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameStateManager : MonoBehaviour
{
	[SerializeField] GameObject gameOverPopup;
	[SerializeField] Health castle;
	[SerializeField] GameObject enemyPool;
	[SerializeField] TextMeshProUGUI gameSpeed;
	[SerializeField] GameObject villageWarning;
	[SerializeField] GameObject murlockList;
	[SerializeField] GameObject coordsCanvas;
	private TextMeshProUGUI _menuTitle;
	private bool _isPaused = false;
	private bool _showDebug = false;
	private bool _gameOver = false;
	public static GameStateManager Instance;

	public static event Action GameRestarted;
	void Start()
	{
		Instance = this;
		gameOverPopup.SetActive(false);
		_menuTitle = gameOverPopup.transform.GetChild(0)
									.transform.GetChild(0)
									.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
	}

	void Update()
	{
		if(Input.GetKeyDown(KeyCode.Tab))
			_showDebug = !_showDebug;
		UpdateDebug();

		if (_gameOver) return;

		if (castle.IsDead())
			castle.gameObject.SetActive(false);
			// GameOver();
		
		if (Input.GetKeyUp(KeyCode.Escape))
			PauseGame();
	}

	private void UpdateDebug()
	{
		murlockList.SetActive(_showDebug);
		coordsCanvas.SetActive(_showDebug);
	}

	public void GameOver()
	{
		_gameOver = true;
		_menuTitle.text = "Game Over";
		gameOverPopup.SetActive(true);
		Time.timeScale = 0;
	}

	private void PauseGame()
	{
		_isPaused = !_isPaused;
		_menuTitle.text = "Game Paused";
		gameOverPopup.SetActive(_isPaused);
		if (_isPaused)
		{
			Debug.Log($"[{CoreHelper.TimeNow()}];[{GetType().Name}];[game paused]");
			SetTimeTo(0f);
		}
		else
		{
			Debug.Log($"[{CoreHelper.TimeNow()}];[{GetType().Name}];[game unpaused]");
			SetTimeTo(1f);
		}
	}

	public void Restart()
	{
		Debug.Log($"[{CoreHelper.TimeNow()}];[{GetType().Name}];[restarting game]");
		_gameOver = false;
		_isPaused = false;
		gameOverPopup.SetActive(false);
		villageWarning.SetActive(false);
		SetTimeTo(1f);
		castle.Revive();
		castle.gameObject.SetActive(true);
		
		foreach (var enemy in enemyPool.GetComponentsInChildren<EnemyStateMachine>())
		{
			enemy.ReturnToPool();
		}
		GameRestarted?.Invoke();
	}

	public void QuitGame()
	{
		Debug.Log($"[{CoreHelper.TimeNow()}];[{GetType().Name}];[quit game]");
		#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
		#endif
		Application.Quit();
	}

	public void SetTimeTo(float value)
	{
		Debug.Log($"[{CoreHelper.TimeNow()}];[{GetType().Name}];[set time to {value}x]");
		gameSpeed.text = $"{value.ToString("0.0")}x";
		Time.timeScale = value;
	}

	public void ChangeCastleInvulnerability(bool value)
	{
		Debug.Log($"[{CoreHelper.TimeNow()}];[{GetType().Name}];[castle invulnerability set to {value}]");
		castle.Killable = !value;
	}
}
