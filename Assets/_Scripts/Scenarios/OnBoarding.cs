using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class OnBoarding : MonoBehaviour
{
	[SerializeField] private Light2D _globalLight;
	[SerializeField] private Gradient _globalLightGradient;
	[SerializeField] private Sphere _sphere;
	[SerializeField] private Color _sphereColor;
	[SerializeField] private Light2D _brazierLight;
	[SerializeField] private GameObject _brazierIndicators;
	[SerializeField] private Transform _hintText;
	[SerializeField] private float _timeTillHint;
	[SerializeField] private float _maxDistToBrazier;
	[SerializeField] private float _pulseDuration = 2f;

	public float typingSpeed = 0.1f;
	public float bobbingFrequency = 2f;
	public float bobbingAmplitude = 1f;
	private float _timeElapsed;
	private TextMeshProUGUI _firstHint;
	private TextMeshProUGUI _secondHint;
	private TextMeshProUGUI _thirdHint;
	private Tween _spherePulse;
	private Tween _brazierPulse;
	private bool _finished;
	private bool _fHintShown;
	private bool _sHintShown;
	private bool _tHintShown;
	private Vector2 _sphereStartPos;
	private void Awake() 
	{
		_globalLight.color = _globalLightGradient.colorKeys[0].color;
		_sphereStartPos = _sphere.transform.position;

		_firstHint = _hintText.GetChild(0).GetComponent<TextMeshProUGUI>();
		_secondHint = _hintText.GetChild(1).GetComponent<TextMeshProUGUI>();
		_thirdHint = _hintText.GetChild(2).GetComponent<TextMeshProUGUI>();

		_firstHint.gameObject.SetActive(false);
		_secondHint.gameObject.SetActive(false);
		_thirdHint.gameObject.SetActive(false);
		
		_brazierIndicators.SetActive(false);
	}

	private void Start() 
	{
		_spherePulse = StartPulsating(_sphere.transform.GetChild(0).GetComponent<Light2D>(), 0.2f, 0.3f, _pulseDuration);
	}

	private void Update() 
	{
		if (_finished) return;

		_timeElapsed += Time.deltaTime;

		if (_brazierPulse == null && _sphereStartPos != (Vector2)_sphere.transform.position)
			_brazierPulse = StartPulsating(_brazierLight, 0.2f, 0.3f, _pulseDuration);
		
		if(!_fHintShown && _timeElapsed > _timeTillHint)
		{
			_fHintShown = true;
			ShowHint(_firstHint);
		}
		if(!_sHintShown && _timeElapsed > 2 * _timeTillHint)
		{
			_sHintShown = true;
			ShowHint(_secondHint);
		}
		if(!_tHintShown && _timeElapsed > 3 * _timeTillHint)
		{
			_tHintShown = true;
			ShowHint(_thirdHint);
		}
		AdjustGlobalLight();
	}


	private void ShowHint(TextMeshProUGUI hint)
	{
		hint.gameObject.SetActive(true);
		TypeText(hint);
	}

	private void AdjustGlobalLight()
	{
		float dist = Vector2.Distance(_sphere.transform.position, _brazierLight.transform.position);
		if (dist >= _maxDistToBrazier)
			_globalLight.color = _globalLightGradient.colorKeys[0].color;
		else 
		{
			_globalLight.color = _globalLightGradient.Evaluate(1 - dist / _maxDistToBrazier);
		}
	}

	public Tween StartPulsating(Light2D light2D, float minAlpha, float maxAlpha, float pulseDuration)
	{
		Tween pulsatingTween = DOTween.To(() => light2D.color.a, alpha => {
			Color color = light2D.color;
			color.a = alpha;
			light2D.color = color;
		}, maxAlpha, pulseDuration)
		.SetEase(Ease.InOutQuad)
		.SetLoops(-1, LoopType.Yoyo);
		
		return pulsatingTween;
	}

	public void StopPulsating(Tween pulsatingTween)
	{
		pulsatingTween?.Kill();
	}

	// IEnumerator TypeText(TextMeshProUGUI textToAnimate)
	// {
	// 	string fullText = textToAnimate.text;
	// 	for (int i = 0; i < fullText.Length; i++)
	// 	{
	// 		char currentChar = fullText[i];
	// 		if (currentChar == ' ')
	// 		{
	// 			textToAnimate.text += currentChar;
	// 		}
	// 		else
	// 		{
	// 			// Tweening up
	// 			textToAnimate.rectTransform.DOBlendableLocalMoveBy(new Vector3(0, bobbingAmplitude, 0), typingSpeed / 2)
	// 				.SetEase(Ease.OutQuad);

	// 			yield return new WaitForSeconds(typingSpeed / 2);

	// 			// Tweening down
	// 			textToAnimate.rectTransform.DOBlendableLocalMoveBy(new Vector3(0, -bobbingAmplitude, 0), typingSpeed / 2)
	// 				.SetEase(Ease.InQuad);

	// 			yield return new WaitForSeconds(typingSpeed / 2);

	// 			textToAnimate.text += currentChar;
	// 		}
	// 		yield return new WaitForSeconds(typingSpeed);
	// 	}

	// 	// Ensure the text stays centered
	// 	// textToAnimate.rectTransform.localPosition = Vector3.zero;
	// }

	private void TypeText(TextMeshProUGUI textToAnimate)
	{
		string fullText = textToAnimate.text;
		textToAnimate.text = "";

		textToAnimate.DOText(fullText, 3.5f, true).SetEase(Ease.InQuad);
	}


	public void FinishSequence()
	{
		StopPulsating(_spherePulse);
		StopPulsating(_brazierPulse);

		_brazierIndicators.SetActive(true);

		_firstHint.DOFade(0f, 1f);
		_secondHint.DOFade(0f, 1f);
		_thirdHint.DOFade(0f, 1f);

		SoundManager.Instance.PlayBattleMusic();

		_finished = true;

		Destroy(gameObject, 1f);
	}
}
