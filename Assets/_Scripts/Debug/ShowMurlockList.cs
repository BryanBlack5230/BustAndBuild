using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class ShowMurlockList : MonoBehaviour
{
    [SerializeField] GameObject enemyPool;
    private TextMeshProUGUI _textField;
    private StringBuilder _sb;

    void Start()
    {
        _textField = GetComponent<TextMeshProUGUI>();
        _sb = new StringBuilder();
    }

    void Update()
    {
        _sb.Clear();

        foreach (var enemy in enemyPool.GetComponentsInChildren<EnemyStateMachine>())
		{
            string coords = $"[x:{enemy.transform.position.x.ToString("0.00")},y:{enemy.transform.position.y.ToString("0.00")}]";
            _sb.AppendLine($"[{enemy.ID}][{enemy.RootState}][{enemy.SubState}]{coords}");
		}
        _textField.text = _sb.ToString();
    }
}
