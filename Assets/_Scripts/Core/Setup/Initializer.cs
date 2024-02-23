using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Initializer : MonoBehaviour
{
	[SerializeField] Transform castleWall;
	[SerializeField] Transform brazier;
	[SerializeField] Transform sea;

	void Awake()
	{
		ConstantTargets.CastleWall = castleWall;
		ConstantTargets.Brazier = brazier;
		ConstantTargets.Sea = sea;

		CoreHelper.SetWall(castleWall.position);
	}

}
