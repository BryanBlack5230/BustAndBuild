using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BB.Combat;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CursorManagement : MonoBehaviour
{
	[SerializeField] private Texture2D closedHandTexture;
	[SerializeField] private Sprite closedHandSprite;
	[SerializeField] GameObject cursorPrefab;
	[SerializeField] bool showDummy = false;

	public EnemyStateMachine selectedEnemy;
	public Grabbable selectedObject;
	Vector2 offset;
	Vector2 mousePosition;
	public float maxSpeed = 10;
	Vector2 mouseForce;
	Vector2 lastPosition;

	private Vector2 cursorHotspot;
	GameObject cursorObject;
	private Sprite defaultHandSprite;
	
	void ChangeCursorGrabbed()
	{
		cursorHotspot = new Vector2(closedHandTexture.width / 2, closedHandTexture.height / 2);
		Cursor.SetCursor(closedHandTexture, cursorHotspot, CursorMode.ForceSoftware);
		cursorObject.GetComponent<SpriteRenderer>().sprite = closedHandSprite;
	}

	void ResetCursor()
	{
		Cursor.SetCursor(null, Vector2.zero, CursorMode.ForceSoftware);
		cursorObject.GetComponent<SpriteRenderer>().sprite = defaultHandSprite;
	}

	void Start()
	{
		#if UNITY_EDITOR
		Cursor.SetCursor(PlayerSettings.defaultCursor, Vector2.zero, CursorMode.ForceSoftware);
		#endif
		cursorObject = Instantiate(cursorPrefab, Vector3.zero, Quaternion.identity);
		defaultHandSprite = cursorObject.GetComponent<SpriteRenderer>().sprite;
	}

	void Update()
	{
		mousePosition = CoreHelper.CursorPos();
		cursorObject.transform.position = mousePosition;
		cursorObject.SetActive(showDummy);
		// CheckColliders();
		if (selectedObject)
			GetForceAndPos();
		if (Input.GetMouseButtonDown(0))
			MouseClicked();
		if (Input.GetMouseButtonUp(0))
			ReleaseObject();
	}

	void FixedUpdate()
	{
		if (selectedObject)
		{
			MoveSelected();
		}
	}

	private void MoveSelected()
	{
		switch (selectedObject.type)
		{
			case ThrowableType.Object:
				selectedObject.SetPosition(mousePosition + offset);
				break;
			case ThrowableType.Enemy:
				selectedEnemy.SetPosition(mousePosition + offset);
				break;
			default:
				break;
		}
	}

	private void GetForceAndPos()
	{
		mouseForce = (mousePosition - lastPosition) / Time.deltaTime;
		mouseForce = Vector2.ClampMagnitude(mouseForce, maxSpeed);// don't think that it's needed
		lastPosition = mousePosition;
		selectedObject.IsGrabbed = true;
	}

	private void MouseClicked()
	{
		ChangeCursorGrabbed();
			Collider2D[] hits = Physics2D.OverlapPointAll(mousePosition);

			foreach (var hit in hits)
			{
				if (hit.TryGetComponent<Grabbable>(out Grabbable target))
				{
					// print("clicked on " + hit.transform.name);
					selectedObject = target;
					selectedObject.GrabbedPosY = selectedObject.transform.position.y;
					selectedObject.OnReleaseObject += OnReleaseObjectCallback;
					if (selectedObject.type == ThrowableType.Enemy)
					{
						var objectParent = target.transform.parent.parent;
						selectedEnemy = objectParent.GetComponent<EnemyStateMachine>();
					}
					offset = new Vector2(selectedObject.transform.position.x - mousePosition.x, selectedObject.transform.position.y - mousePosition.y);
				}
			}
	}

	private void OnReleaseObjectCallback()
	{
		ResetCursor();
		Drop();
	}

	private void ReleaseObject()
	{
		ResetCursor();
		if (selectedObject)
		{
			selectedObject.ThrowForce = mouseForce;
			Drop();
		}
	}

	private void Drop()
	{
		selectedObject.IsGrabbed = false;
		selectedObject.IsFlung = true;
		selectedObject.OnReleaseObject -= OnReleaseObjectCallback;
		selectedObject = null;
		if (selectedEnemy)
				selectedEnemy = null;
	}

	private void CheckColliders()
	{
		Collider2D[] hits = Physics2D.OverlapPointAll(mousePosition);
		foreach (var hit in hits)
		{
			Debug.Log("Found " + hit.transform.name);
		}
	}


}
