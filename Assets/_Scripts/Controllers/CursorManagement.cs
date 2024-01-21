using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BB.Combat;
#if UNITY_EDITOR
    using UnityEditor;
#endif

public class CursorManagement : MonoBehaviour
{
    [SerializeField] private Texture2D closedHandTexture;
    [SerializeField] private Sprite closedHandSprite;
    [SerializeField] GameObject cursorPrefab;
    [SerializeField] bool showDummy = false;

    public EnemyStateMachine selectedObject;
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
        mousePosition = CoreHelper.camera.ScreenToWorldPoint(Input.mousePosition);
        cursorObject.transform.position = mousePosition;
        cursorObject.SetActive(showDummy);
        //CheckColliders();
        InteractWithMonster();
    }

    private void InteractWithMonster()
    {
        if (selectedObject)
        {
            mouseForce = (mousePosition - lastPosition) / Time.deltaTime;
            mouseForce = Vector2.ClampMagnitude(mouseForce, maxSpeed);// don't think that it's needed
            lastPosition = mousePosition;
            selectedObject.IsGrabbed = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            ChangeCursorGrabbed();
            Collider2D[] hits = Physics2D.OverlapPointAll(mousePosition);

            foreach (var hit in hits)
            {
                Grabbable target = hit.transform.GetComponent<Grabbable>();
                if (target == null) return;
                //print("clicked on " + hit.transform.name);

                selectedObject = target.transform.gameObject.GetComponent<EnemyStateMachine>();
                offset = new Vector2(selectedObject.transform.position.x - mousePosition.x, selectedObject.transform.position.y - mousePosition.y);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            ResetCursor();
            if (selectedObject)
            {
                selectedObject.IsGrabbed = false;
                selectedObject.IsFlung = true;
                selectedObject.ThrowForce = mouseForce;
                selectedObject = null;
            }
        }
    }

    private void CheckColliders()
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(mousePosition);
        foreach (var hit in hits)
        {
            Debug.Log("Found " + hit.transform.name);
        }
    }

    void FixedUpdate()
    {
        if (selectedObject)
        {
            selectedObject.SetPosition(mousePosition + offset);
        }
    }

}
