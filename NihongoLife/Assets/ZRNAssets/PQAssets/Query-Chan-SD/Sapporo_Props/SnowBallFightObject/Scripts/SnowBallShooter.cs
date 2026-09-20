using UnityEngine;
using System.Collections;

public class SnowBallShooter : MonoBehaviour {

	[SerializeField]
	GameObject SnowBallObject;
	
	GameObject targetSnowBallFightObjects;


	// Use this for initialization
	void Start () {
		targetSnowBallFightObjects = GameObject.Find("SnowBallFightObjects");
	}
	
	// Update is called once per frame
	void Update () {

	}


	public void ShootSnowBall () {

		GameObject targetObj = Instantiate (SnowBallObject,
		                                    this.transform.localPosition,
		                                    Quaternion.Euler(this.transform.localEulerAngles.x, this.transform.localEulerAngles.y, this.transform.localEulerAngles.z)) as GameObject;
		targetObj.name = "SnowBall";
		targetObj.transform.parent = targetSnowBallFightObjects.transform;

		if (this.gameObject.tag == "Player")
		{
			targetObj.transform.localPosition = new Vector3 (this.transform.localPosition.x + 0.3f, this.transform.localPosition.y + 0.4f, this.transform.localPosition.z - 0.3f);
			targetObj.transform.localEulerAngles = this.transform.localEulerAngles;
		}
		else if (this.gameObject.tag == "Enemy")
		{
			targetObj.transform.localPosition = new Vector3 (this.transform.localPosition.x - 0.3f, this.transform.localPosition.y + 0.4f, this.transform.localPosition.z - 0.3f);

			int randomdir = Random.Range(0, 5);
			if (randomdir ==  0 || randomdir ==  1)
			{
				// Shoot to player direction
				GameObject targetPlayerObject = GameObject.Find("Player");
				targetObj.transform.LookAt(targetPlayerObject.transform.position);
			}
			else
			{
				targetObj.transform.localEulerAngles = this.transform.localEulerAngles;
			}
		}
	}

}
