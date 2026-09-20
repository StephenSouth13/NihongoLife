using UnityEngine;
using System.Collections;

public class EnemyPcController : MonoBehaviour {

	[SerializeField]
	GameObject queryChanSapporoObject;

	[SerializeField]
	GameObject turnCenterObject;

	float counterTime;
	bool isDie;

	// Use this for initialization
	void Start () {
		isDie = false;
		queryChanSapporoObject.GetComponent<QueryChanSapporoMecanimController>().ChangeAnimation(
							QueryChanSapporoMecanimController.QueryChanSapporoAnimationType.SAPPORO_IKADANCE);

		// for temporary
		turnCenterObject.transform.localEulerAngles = new Vector3(0.0f, 0.0f, 0.0f);
	}
	
	// Update is called once per frame
	void Update () {

		if (Time.time > counterTime && isDie == false)
		{
			StartCoroutine ("QueryChanSDShoot");
			counterTime = Time.time + Random.Range(0.5f, 5.0f);
		}

	}

	IEnumerator QueryChanSDShoot () {

		turnCenterObject.transform.localEulerAngles = new Vector3(0.0f, 0.0f, 0.0f);

		queryChanSapporoObject.GetComponent<QueryChanSapporoMecanimController>().ChangeAnimation(
							QueryChanSapporoMecanimController.QueryChanSapporoAnimationType.SAPPORO_SNOWBALLING);
		yield return new WaitForSeconds(0.7f);
		this.GetComponent<SnowBallShooter>().ShootSnowBall();
		yield return new WaitForSeconds(0.5f);

		turnCenterObject.transform.localEulerAngles = new Vector3(0.0f, 90.0f, 0.0f);
		queryChanSapporoObject.GetComponent<QueryChanSapporoMecanimController>().ChangeAnimation(
							QueryChanSapporoMecanimController.QueryChanSapporoAnimationType.SAPPORO_IKADANCE);
	}


	void OnTriggerEnter (Collider otherObject) {
		
		//Debug.Log ("hit : " + otherObject.name);
		if (otherObject.tag == "SnowBall")
		{
			StartCoroutine (QueryChanDie());
			Destroy(otherObject.gameObject);
		}
		
	}


	IEnumerator QueryChanDie () {

		//Debug.Log ("NPC die");
		StopCoroutine("QueryChanSDShoot");
		isDie = true;
		queryChanSapporoObject.GetComponent<QueryChanSapporoMecanimController>().ChangeAnimation(
			QueryChanSapporoMecanimController.QueryChanSapporoAnimationType.SAPPORO_STAND);
		turnCenterObject.transform.localEulerAngles = new Vector3(0.0f, 0.0f, 0.0f);
		turnCenterObject.transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);

		queryChanSapporoObject.GetComponent<QueryChanSapporoMecanimController>().ChangeAnimation(
			QueryChanSapporoMecanimController.QueryChanSapporoAnimationType.SAPPORO_POSE_COLD);
		yield return new WaitForSeconds(1.0f);

		//turnCenterObject.transform.localEulerAngles = new Vector3(0.0f, 0.0f, 0.0f);
		queryChanSapporoObject.GetComponent<QueryChanSapporoMecanimController>().ChangeAnimation(
			QueryChanSapporoMecanimController.QueryChanSapporoAnimationType.SAPPORO_CLIONE);
		turnCenterObject.transform.localEulerAngles = new Vector3(0.0f, 0.0f, 0.0f);
		StartCoroutine(MoveUpQuery());
		yield return new WaitForSeconds(3.0f);

		GameObject[] EnemyQueryChan = GameObject.FindGameObjectsWithTag("Enemy");
		if (EnemyQueryChan.Length == 1)
		{
			//Debug.Log("Game Clear");
			GameObject.Find("GameController").GetComponent<GameSceneController>().ChangeStage();
		}
		Destroy(this.gameObject);
	}


	IEnumerator MoveUpQuery () {

		while (true)
		{
			this.transform.localPosition += Vector3.up * Time.deltaTime * 0.5f;
			yield return new WaitForSeconds(0.05f);
		}

	}

}
