using System.Collections;
using System.Collections.Generic;
using System.Text;
using BB.Resources;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CastleHealthDisplay : MonoBehaviour
{
	[SerializeField] Health castle;
	private Image _healthBar;
	private TextMeshProUGUI _text;
	private StringBuilder strBuilder = new StringBuilder(20);

	void Awake()
	{
		_healthBar = transform.GetChild(1).gameObject
					.transform.GetChild(2).GetComponent<Image>();
		_text = transform.GetChild(1).gameObject
					.transform.GetChild(3).GetComponent<TextMeshProUGUI>();
	}

	void Update()
	{
		ChangeHealthDisplay();
	}

	public void ChangeHealthDisplay()
	{
		_healthBar.fillAmount = castle.GetPercentage();
		strBuilder.Clear();
		_text.text = strBuilder.Append(castle.GetCurrent())
							   .Append(" / ")
							   .Append(castle.maxHealth).ToString();
	}
}
