using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {
	
	enum PlayerBehaviorStatus {

		STOP = 0,
		WALK_LEFT = 1,
		WALK_RIGHT = 2,
		THROW_BALL = 3

	}

	PlayerBehaviorStatus targetPlayerBehaviorStatus;
	PlayerBehaviorStatus lastTargetPlayerBehaviorStatus;

	[SerializeField]
	GameObject queryChanSapporoObject;

	[SerializeField]
	GameObject turnCenterObject;

	bool disableInputKey;

	bool timerEnable;
	float cancelTimertime;
	float intervalShoot = 0.5f;

	float PcMoveAreaL;
	float PcMoveAreaR;

	bool isDie;


	// Use this for initialization
	void Start () {

		disableInputKey = false; 
		timerEnable = false;
		isDie = false;
		lastTargetPlayerBehaviorStatus = PlayerBehaviorStatus.STOP;
		targetPlayerBehaviorStatus = PlayerBehaviorStatus.STOP;

		GameObject areaInvader = GameObject.Find("AreaInvader");
		PcMoveAreaL = areaInvader.transform.localPosition.z + areaInvader.GetComponent<Renderer>().bounds.size.z / 2;
		PcMoveAreaR = areaInvader.transform.localPosition.z - areaInvader.GetComponent<Renderer>().bounds.size.z / 2;
		//Debug.Log ("PcMoveAreaL = " + PcMoveAreaL + " / PcMoveAreaR = " + PcMoveAreaR);
	}
	
	// Update is called once per frame
	void Update () {
	
		if (disableInputKey == false)
		{

			// animation
			if (Input.GetKeyDown(KeyCode.LeftArrow))
			{
				//Debug.Log ("left key");
				targetPlayerBehaviorStatus = PlayerBehaviorStatus.WALK_LEFT;
			}
			else if (Input.GetKeyDown(KeyCode.RightArrow))
			{
				//Debug.Log ("right key");
				targetPlayerBehaviorStatus = PlayerBehaviorStatus.WALK_RIGHT;
			}
			else if (Input.GetKeyDown(KeyCode.Space))
			{
				//Debug.Log ("space key");
				targetPlayerBehaviorStatus = PlayerBehaviorStatus.THROW_BALL;
				disableInputKey = true;
			}
			else
			{
				if (!Input.anyKey)
				{
					//Debug.Log ("key up");
					targetPlayerBehaviorStatus = PlayerBehaviorStatus.STOP;
					disableInputKey = false;
				}
			}

			// position
			if (targetPlayerBehaviorStatus != lastTargetPlayerBehaviorStatus)
			{
				//Debug.Log("change animation " + disableInputKey);
				StartCoroutine(QueryChanSDBehavior (disableInputKey));
				lastTargetPlayerBehaviorStatus = targetPlayerBehaviorStatus;
			}

			switch (targetPlayerBehaviorStatus)
			{
				case PlayerBehaviorStatus.WALK_LEFT:
				if (this.transform.localPosition.z <= PcMoveAreaL)
				{
					this.transform.localPosition += Vector3.forward * Time.deltaTime;
				}
				break;

				case PlayerBehaviorStatus.WALK_RIGHT:
				if (this.transform.localPosition.z >= PcMoveAreaR)
				{
					this.transform.localPosition -= Vector3.forward * Time.deltaTime;
				}
					break;
			}
		}

		// shoot timer
		if (isDie == false)
		{
			if (timerEnable == false && disableInputKey == true)
			{
				//Debug.Log ("start timer");
				timerEnable = true;
				cancelTimertime = Time.time + intervalShoot;
			}

			if (timerEnable == true && Time.time >= cancelTimertime)
			{
				//Debug.Log ("stop timer");
				disableInputKey = false;
				timerEnable = false;
			}
		}
	}



	IEnumerator QueryChanSDBehavior (bool shoot) {

		yield return new WaitForSeconds(0.0f);

		if (shoot == true)
		{
			this.transform.localEulerAngles = new Vector3(0.0f, 90.0f, 0.0f);
			queryChanSapporoObject.GetComponent<QueryChanSapporoMecanimController>().ChangeAnimation(
				QueryChanSapporoMecanimController.QueryChanSapporoAnimationType.SAPPORO_SNOWBALLING);
			yield return new WaitForSeconds(0.4f);

			this.GetComponent<SnowBallShooter>().ShootSnowBall();
			yield return new WaitForSeconds(0.1f);
			
			this.transform.localEulerAngles = new Vector3(0.0f, 90.0f, 0.0f);
			queryChanSapporoObject.GetComponent<QueryChanSapporoMecanimController>().ChangeAnimation(
				QueryChanSapporoMecanimController.QueryChanSapporoAnimationType.SAPPORO_IDLE);
			targetPlayerBehaviorStatus = PlayerBehaviorStatus.THROW_BALL;
		}
		else
		{
			switch (targetPlayerBehaviorStatus)
			{
				case PlayerBehaviorStatus.STOP:
					queryChanSapporoObject.GetComponent<QueryChanSapporoMecanimController>().ChangeAnimation(
						QueryChanSapporoMecanimController.QueryChanSapporoAnimationType.SAPPORO_IDLE);
					this.transform.localEulerAngles = new Vector3(0.0f, 90.0f, 0.0f);
					break;

				case PlayerBehaviorStatus.WALK_LEFT:
					queryChanSapporoObject.GetComponent<QueryChanSapporoMecanimController>().ChangeAnimation(
						QueryChanSapporoMecanimController.QueryChanSapporoAnimationType.SAPPORO_WALK);
					this.transform.localEulerAngles = new Vector3(0.0f, 0.0f, 0.0f);
					break;

				case PlayerBehaviorStatus.WALK_RIGHT:
					queryChanSapporoObject.GetComponent<QueryChanSapporoMecanimController>().ChangeAnimation(
						QueryChanSapporoMecanimController.QueryChanSapporoAnimationType.SAPPORO_WALK);
					this.transform.localEulerAngles = new Vector3(0.0f, 180.0f, 0.0f);
					break;

				default:
					queryChanSapporoObject.GetComponent<QueryChanSapporoMecanimController>().ChangeAnimation(
						QueryChanSapporoMecanimController.QueryChanSapporoAnimationType.SAPPORO_IDLE);
					this.transform.localEulerAngles = new Vector3(0.0f, 90.0f, 0.0f);
					break;
			}
		}
	}


	void OnTriggerEnter (Collider otherObject) {
		
		//Debug.Log ("hit : " + otherObject.name);
		if (otherObject.tag == "SnowBall")
		{
			StartCoroutine (QueryChanDie());
		}
		
	}

	IEnumerator QueryChanDie () {

		isDie = true;
		disableInputKey = true;

		queryChanSapporoObject.GetComponent<QueryChanSapporoMecanimController>().ChangeAnimation(
			QueryChanSapporoMecanimController.QueryChanSapporoAnimationType.SAPPORO_STAND);
		this.transform.localEulerAngles = new Vector3(0.0f, 0.0f, 0.0f);
		turnCenterObject.transform.localEulerAngles = new Vector3(0.0f, -90.0f, 0.0f);
		turnCenterObject.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
		
		queryChanSapporoObject.GetComponent<QueryChanSapporoMecanimController>().ChangeAnimation(
			QueryChanSapporoMecanimController.QueryChanSapporoAnimationType.SAPPORO_POSE_BEAMBITIOUS);
		yield return new WaitForSeconds(1.0f);

		queryChanSapporoObject.GetComponent<QueryChanSapporoMecanimController>().ChangeAnimation(
			QueryChanSapporoMecanimController.QueryChanSapporoAnimationType.SAPPORO_CLIONE);
		StartCoroutine(MoveUpQuery());
		yield return new WaitForSeconds(3.0f);

		GameObject.Find("GameController").GetComponent<GameSceneController>().ChangeSceneMode();

	}



	IEnumerator MoveUpQuery () {

		while (true)
		{
			this.transform.localPosition += Vector3.up * Time.deltaTime * 0.5f;
			yield return new WaitForSeconds(0.05f);
		}
		
	}
	
}
