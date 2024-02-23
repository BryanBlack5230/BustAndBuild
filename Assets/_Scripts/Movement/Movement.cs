using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BB.Resources;
using BB.Core;

namespace BB.Movement
{
	public class Movement : MonoBehaviour, IAction
	{
		[SerializeField] float maxSpeed = 6f;
		private bool _canMove = true;
		private ActionScheduler _actionScheduller;

		public float MaxSpeed {get {return maxSpeed;}}

		public void StartMoveAction(Vector3 destination, float speedFraction)
		{
			_canMove = true;
			if (_actionScheduller == null ) 
				_actionScheduller = GetComponent<ActionScheduler>();
			_actionScheduller.StartAction(this);
			MoveTo(destination, speedFraction);
		}

		public void MoveTo(Vector3 target, float speedFraction)
		{
			if (!_canMove) return;

			var step = Mathf.Clamp01(speedFraction) * maxSpeed * Time.deltaTime;
			transform.position = Vector3.MoveTowards(transform.position, target, step);
		}

		public void Cancel()
		{
			_canMove = false;
		}
	}
}
