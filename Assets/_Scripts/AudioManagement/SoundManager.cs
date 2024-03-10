using DG.Tweening;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
	public static SoundManager Instance;
	[SerializeField] private AudioSource _musicSource, _effectsSource, _ambientSource;
	private Tween _battleTween;
	private void Awake() {
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else
			Destroy(gameObject);
	}

	public void PlaySound(AudioClip clip)
	{
		_effectsSource.PlayOneShot(clip);
	}

	public void PlayBattleMusic()
	{
		_battleTween?.Kill();
		_musicSource.Play();
		_battleTween = _musicSource.DOFade(0.667f, 1f);
	}

	public void StopBattleMusic()
	{
		_battleTween?.Kill();
		_battleTween = _musicSource.DOFade(0f, 1.5f).OnComplete(() => _musicSource.Stop());
	}

	public void SetAmbientVolume(float volume)
	{
		_ambientSource.DOFade(volume, 1f);
	}
}
