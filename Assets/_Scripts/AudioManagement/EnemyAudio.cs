using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAudio : MonoBehaviour
{
	[SerializeField] AudioClip attackClip;
	[SerializeField] AudioClip deathClip;
	[SerializeField] AudioClip deathClipMidfly;
	private AudioSource _source;
	public bool isPlaying {get {return _source.isPlaying;}}

	void Start()
	{
		_source = GetComponent<AudioSource>();
	}

	public void Play(string clipName)
	{
		switch (clipName)
		{
			case "attack":
				_source.clip = attackClip;
				_source.Play();
				break;
			case "death":
				_source.clip = deathClip;
				_source.Play();
				break;
			case "deathMidFlight":
				_source.clip = deathClip;
				_source.Play();
				_source.clip = deathClipMidfly;
				_source.PlayDelayed(0.5f);
				break;
			default:
				Debug.LogError("Clip with name " + clipName + " is not found");
				break;
		}
	}
}
