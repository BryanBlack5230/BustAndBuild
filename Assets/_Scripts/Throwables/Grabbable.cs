using System;
using UnityEngine;

namespace BB.Combat
{
	public class Grabbable : MonoBehaviour
	{
		[SerializeField] public ThrowableType type;
		public bool IsFlung {get; set;}
		public bool IsGrabbed {get; set;}
		public Vector2 ThrowForce {get; set;}
		public float GrabbedPosY {get; set;}

		public event Action<Vector2> OnSetPosition;
		public event Action OnReleaseObject;
		public void SetPosition(Vector2 newPos)
		{
			OnSetPosition?.Invoke(newPos);
		}

		public void ReleaseObject()
		{
			OnReleaseObject?.Invoke();
		}
	}
}


