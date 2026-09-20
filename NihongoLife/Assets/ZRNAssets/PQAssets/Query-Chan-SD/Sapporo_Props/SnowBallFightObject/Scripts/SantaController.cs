using UnityEngine;
using System.Collections;

public class SantaController : MonoBehaviour {

	[SerializeField]
	GameObject startObject;

	[SerializeField]
	GameObject goalObject;

	float goalPosZ;

	[SerializeField]
	float moveSpeed;

	// Use this for initialization
	void Start () {
		goalPosZ = goalObject.transform.localPosition.z;
	}
	
	// Update is called once per frame
	void Update () {

		if (this.transform.localPosition.z > goalPosZ)
		{
			this.transform.localPosition = startObject.transform.localPosition;
		}
		else
		{
			this.transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.Self);
		}
	
	}

	void OnTriggerEnter (Collider otherObject) {

		//Debug.Log ("hit : " + otherObject.name);
		if (otherObject.tag == "SnowBall")
		{
			Destroy(otherObject.gameObject);
		}

	}

}
