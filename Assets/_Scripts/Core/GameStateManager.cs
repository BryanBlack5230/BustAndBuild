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
	private TextMeshProUGUI _menuTitle;
	private bool _isPaused = false;

	public static event Action GameRestarted;
	void Start()
	{
		gameOverPopup.SetActive(false);
		_menuTitle = gameOverPopup.transform.GetChild(0)
									.transform.GetChild(0)
									.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
	}

	void Update()
	{
		if (castle.IsDead())
			GameOver();
		else if (Input.GetKeyUp(KeyCode.Escape))
			PauseGame();
	}

	private void GameOver()
	{
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
			SetTimeTo(0f);
		else
			SetTimeTo(1f);
	}

	public void Restart()
	{
		_isPaused = false;
		gameOverPopup.SetActive(false);
		SetTimeTo(1f);
		castle.Revive();
		
		foreach (var enemy in enemyPool.GetComponentsInChildren<EnemyStateMachine>())
		{
			enemy.ReturnToPool();
		}
		GameRestarted?.Invoke();
	}

	public void QuitGame()
	{
		#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
		#endif
		Application.Quit();
	}

	public void SetTimeTo(float value)
	{
		gameSpeed.text = $"{value.ToString("0.0")}x";
		Time.timeScale = value;
	}

	public void ChangeCastleInvulnerability(bool value)
	{
		castle.Killable = !value;
	}
}
