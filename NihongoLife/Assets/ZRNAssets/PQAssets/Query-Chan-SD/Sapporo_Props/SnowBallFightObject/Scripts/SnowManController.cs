using UnityEngine;
using System.Collections;

public class SnowManController : MonoBehaviour {

	[SerializeField]
	float decreaseScale;

	// Use this for initialization
	void Start () {

		float randomPosZ = Random.Range(-1.5f, 1.5f);
		//Debug.Log ("randomPosZ = " + randomPosZ);
		this.transform.localPosition = new Vector3 (this.transform.localPosition.x, this.transform.localPosition.y, randomPosZ);
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnTriggerEnter (Collider otherObject) {
		
		//Debug.Log ("hit : " + otherObject.name);
		if (otherObject.tag == "SnowBall")
		{
			Destroy(otherObject.gameObject);
			this.transform.localScale -= new Vector3(decreaseScale, decreaseScale/2, decreaseScale);
			if (this.transform.localScale.x <= 0.0f)
			{
				Destroy (this.gameObject);
			}
		}
		
	}

}
