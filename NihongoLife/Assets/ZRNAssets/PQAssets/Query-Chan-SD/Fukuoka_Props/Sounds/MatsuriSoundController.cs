using UnityEngine;
using System.Collections;

public class MatsuriSoundController : MonoBehaviour {
	
	private float timeNextRing;

	[SerializeField]
	private AudioClip[] MatsuriSounds;

	// Use this for initialization
	void Start () {
		timeNextRing = Time.time;
	}
	
	// Update is called once per frame
	void Update () {
		if (timeNextRing < Time.time)
		{
			this.GetComponent<AudioSource>().PlayOneShot(MatsuriSounds[Random.Range(0, 4)]);
			timeNextRing = Time.time + Random.Range(3.0f, 10.0f);
		}
	}

}
