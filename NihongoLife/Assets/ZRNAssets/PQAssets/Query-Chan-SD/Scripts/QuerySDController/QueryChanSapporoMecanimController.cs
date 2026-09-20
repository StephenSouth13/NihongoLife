using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class QueryChanSapporoMecanimController : MonoBehaviour {

	[SerializeField]
	GameObject queryBodyParts;
	
	public enum QueryChanSapporoAnimationType
	{

		// Sapporo Motion
		SAPPORO_STAND = 401,
		SAPPORO_WALK = 402,
		SAPPORO_RUN = 403,
		SAPPORO_IDLE = 404,
		SAPPORO_SNOWBALLING = 405,
		SAPPORO_CLIONE = 406,
		SAPPORO_IKADANCE = 407,
		
		// SAPPORO Pose
		SAPPORO_POSE_COLD = 450,
		SAPPORO_POSE_BEAMBITIOUS = 451,
		SAPPORO_POSE_BEAR = 452

	}

	public enum QueryChanSDHandType
	{

		NORMAL = 0,
		STONE = 1,
		PAPER = 2

	}

	void Update()
	{
		queryBodyParts.transform.localPosition = Vector3.zero;
		queryBodyParts.transform.localRotation = Quaternion.identity;

	}


	public void ChangeAnimation (QueryChanSapporoAnimationType animNumber, bool isChangeMechanimState=true)
	{

		if (isChangeMechanimState) {
			queryBodyParts.GetComponent<Animator>().SetInteger("AnimIndex", (int)animNumber);
		}

	}

}
