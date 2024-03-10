using System;
using BB.Resources;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

[Serializable]
public struct DayDifficulty
{
	public float minSpawnRate;
	public float maxSpawnRate;
	public int minEnemiesPerSpawn;
	public int maxEnemiesPerSpawn;
}
public class GameStateManager : MonoBehaviour
{
	[SerializeField] Resources resources;
	[SerializeField] GameObject gameOverPopup;
	[SerializeField] GameObject gameTitle;
	[SerializeField] Health castle;
	[SerializeField] GameObject enemyPool;
	[SerializeField] TextMeshProUGUI gameSpeed;
	[SerializeField] GameObject villageWarning;
	[SerializeField] GameObject murlockList;
	[SerializeField] GameObject coordsCanvas;
	[SerializeField] SpawnManager spawnManager;
	[SerializeField] TomatoFieldController tomatoField;
	[SerializeField] GameObject victoryPopup;
	[SerializeField] GameObject victoryEffectPrefab;
	[SerializeField] GameObject battleUI;
	[SerializeField] Light2D globalLight;
	[SerializeField] Color dayColor;
	[SerializeField] DayDifficulty[] difficulties;
	
	private TextMeshProUGUI _menuTitle;
	private bool _isPaused = false;
	private bool _showDebug = false;
	private bool _gameOver = false;
	public static GameStateManager Instance;
	private OnBoarding _onBoardSquence;
	private int _dayCounter = 0;
	private GameObject _victoryEffect;
	private bool _onBoardingFinished;

	public static event Action GameRestarted;
	public static event Action OnBrazierDestroyed;
	private void Awake() 
	{
		_onBoardSquence = transform.GetChild(0).GetComponent<OnBoarding>();
		victoryPopup.SetActive(false);
		battleUI.SetActive(false);
	}

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
		else
			castle.gameObject.SetActive(true);
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

	public void BrazierDestroyed()
	{
		OnBrazierDestroyed?.Invoke();
		GameOver();
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
		_dayCounter = 0;
		tomatoField.SetLevel(_dayCounter);
		_gameOver = false;
		_isPaused = false;
		gameOverPopup.SetActive(false);
		victoryPopup.SetActive(false);
		villageWarning.SetActive(false);
		SetTimeTo(1f);
		castle.Revive();
		castle.gameObject.SetActive(true);
		
		ReturnEnemies();
		SoundManager.Instance.StopBattleMusic();
		SoundManager.Instance.SetAmbientVolume(0.611f);
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

	private void HandleTitle()
	{
		if (!_onBoardingFinished)
		{
			_onBoardingFinished = true;
			_onBoardSquence?.FinishSequence();
		}
		battleUI.SetActive(true);
		gameTitle.transform.DOMove(gameTitle.transform.position + Vector3.down * 5, 5f, false);
		Light2D titleLight1 = gameTitle.transform.GetChild(0).GetComponent<Light2D>();
		Light2D titleLight2 = gameTitle.transform.GetChild(1).GetComponent<Light2D>();
		TextMeshProUGUI titleText3 = gameTitle.transform.GetChild(2).GetChild(0).GetComponent<TextMeshProUGUI>();

		float fadeOutDuration = 0.5f;

		DOTween.To(() => titleLight1.color.a, alpha => {
			Color color = titleLight1.color;
			color.a = alpha;
			titleLight1.color = color;
		}, 0f, fadeOutDuration);
		DOTween.To(() => titleLight2.color.a, alpha => {
			Color color = titleLight2.color;
			color.a = alpha;
			titleLight2.color = color;
		}, 0f, fadeOutDuration);
		titleText3.DOFade(0f, fadeOutDuration);
	}

	private void DayStarts()
	{
		int difficultyLevel = Mathf.Min(_dayCounter, difficulties.Length - 1);
		spawnManager.ChangeSpawnRate(difficulties[difficultyLevel].minSpawnRate, difficulties[difficultyLevel].maxEnemiesPerSpawn, 
									difficulties[difficultyLevel].minEnemiesPerSpawn, difficulties[difficultyLevel].maxEnemiesPerSpawn);
		spawnManager.StartSpawning();
		SoundManager.Instance.PlayBattleMusic();
		SoundManager.Instance.SetAmbientVolume(0.224f);
	}
	
	private void NightStarts()
	{
		SoundManager.Instance.StopBattleMusic();
		SoundManager.Instance.SetAmbientVolume(0.611f);
		tomatoField.SetLevel((int)MathF.Min(++_dayCounter, 5));
		spawnManager.StopSpawning();
		if (_dayCounter == 5)
			Victory();
	}

	private void Victory()
	{
		victoryPopup.SetActive(true);
		_victoryEffect = Instantiate(victoryEffectPrefab);
		globalLight.color = dayColor;

		ReturnEnemies();
	}

	private void ReturnEnemies()
	{
		foreach (var enemy in enemyPool.GetComponentsInChildren<EnemyStateMachine>())
		{
			enemy.ReturnToPool();
		}
	}

	public void VictoryClose()
	{
		victoryPopup.SetActive(false);
		Destroy(_victoryEffect);
	}

	public bool UsePearls(int amount)
	{
		return resources.pearls.Take(amount);
	}

	private void OnEnable() 
	{
		TimeController.DayStarted += HandleTitle;
		TimeController.DayStarted += DayStarts;
		TimeController.DayEnded += NightStarts;
	}

	private void OnDestroy() 
	{
		TimeController.DayStarted -= HandleTitle;
		TimeController.DayStarted -= DayStarts;
		TimeController.DayEnded -= NightStarts;
	}
}
