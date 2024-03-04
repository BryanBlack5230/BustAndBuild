using System.Collections;
using System.Collections.Generic;
using BB.Resources;
using UnityEngine;

public class Castle : MonoBehaviour
{
	private Health _health;
	private void Awake() 
	{
		_health = GetComponent<Health>();
	}

	public void Repair(int repairAmount)
	{
		if (GameStateManager.Instance.UsePearls(repairAmount * 5))
			_health.Heal(repairAmount);
	}
}
