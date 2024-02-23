using UnityEngine;

public class SoundManager : MonoBehaviour
{
	public static SoundManager Instance;
	[SerializeField] private AudioSource _musicSource, _effectsSource;

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
}
