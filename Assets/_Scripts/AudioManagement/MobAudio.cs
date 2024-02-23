using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SoundType
{
	Death,
	Hurt,
	Attack
}

[CreateAssetMenu(fileName = "MobAudio", menuName = "Audio/MobAudio", order = 0)]
public class MobAudio : ScriptableObject
{
	[SerializeField] AudioClip[] deathSounds;
	[SerializeField] AudioClip[] hurtSounds;
	[SerializeField] AudioClip[] attackSounds;

	private Dictionary<SoundType, AudioClip[]> _lookup;

	private void BuildLookup()
	{
		if (_lookup != null) return;

		_lookup = new Dictionary<SoundType, AudioClip[]>();
		_lookup[SoundType.Death] = deathSounds;
		_lookup[SoundType.Hurt] = hurtSounds;
		_lookup[SoundType.Attack] = attackSounds;
	}

	public AudioClip GetClip(SoundType type)
	{
		BuildLookup();

		AudioClip[] clips = _lookup[type];

		if (clips.Length == 0)
		{
			Debug.LogError($"[{CoreHelper.TimeNow()}];[{GetType().Name}];[No sounds available for {type.ToString()}]");
			return null;
		}

		return clips[Random.Range(0, clips.Length)];
	}
}
