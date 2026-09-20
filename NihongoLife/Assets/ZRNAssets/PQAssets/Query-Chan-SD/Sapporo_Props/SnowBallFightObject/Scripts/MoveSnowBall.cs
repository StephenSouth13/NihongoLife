using UnityEngine;
using System.Collections;

public class MoveSnowBall : MonoBehaviour {
	
	float moveSpeed; 

	float destroyPosZ;
	float destroyPosZpc;

	// Use this for initialization
	void Start () {
		GameObject areaInvader = GameObject.Find("AreaInvader");
		destroyPosZ = areaInvader.transform.localPosition.z - areaInvader.GetComponent<Renderer>().bounds.size.z;
		destroyPosZpc = areaInvader.transform.localPosition.z + areaInvader.GetComponent<Renderer>().bounds.size.z;
		//Debug.Log ("areaInvader edge z = " + destroyPosZ);

		moveSpeed = Random.Range(1.0f, 3.0f);
	}
	
	// Update is called once per frame
	void Update () {
		this.transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.Self);
		if (this.transform.localPosition.x < destroyPosZ || this.transform.localPosition.x > destroyPosZpc)
		{
			Destroy (this.gameObject);
		}
	}
}
