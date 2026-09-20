using UnityEngine;
using System.Collections;

public class GameSceneController : MonoBehaviour {

	[SerializeField]
	GameObject[] GameStages;

	[SerializeField]
	GameObject SnowBallBattleStage;

	[SerializeField]
	GameObject SnowObjects;
	[SerializeField]
	GameObject SkyDoom;
	[SerializeField]
	GameObject NormalCamera;

	[SerializeField]
	Material[] SkyDoomMaterials;

	[SerializeField]
	GameObject SnowBallSetPrefab;

	int playStageNum;
	GameStatus NowGameStatus;

	enum GameStatus {

		NORMAL_MODE = 0,
		GAME_MODE = 1

	}


	// Use this for initialization
	void Start () {
		NowGameStatus = GameStatus.GAME_MODE;
		ChangeSceneMode();
		playStageNum = 0;
	}
	
	// Update is called once per frame
	void Update () {
	
	}


	void OnGUI () {

		string buttonLabel = "";
		switch (NowGameStatus)
		{
		case GameStatus.NORMAL_MODE:
			buttonLabel = "Start Snowball Fight Game";
			break;
		case GameStatus.GAME_MODE:
			buttonLabel = "Quit Game";
			showHowToPlayGUI();
			break;
		}
		if (GUI.Button(new Rect(10, 10, 200, 50), buttonLabel))
		{
			ChangeSceneMode();
		}
		
	}

	void showHowToPlayGUI()
	{
		GUI.Box(new Rect(10, 70 ,200 ,60), "Interaction");
		GUI.Label(new Rect(30, 90, 140, 40),
		          "Arrow LR : Move LR\n" +
		          "Space : Shoot");
		
	}
	

	public void ChangeSceneMode () {

		switch (NowGameStatus)
		{
		case GameStatus.GAME_MODE:			// to normal mode
			SnowObjects.SetActive(false);
			SnowBallBattleStage.SetActive(false);
			SkyDoom.GetComponent<Renderer>().material = SkyDoomMaterials[0];
			NormalCamera.SetActive(true);
			this.GetComponent<AudioSource>().Stop();
			NowGameStatus = GameStatus.NORMAL_MODE;
			playStageNum = 0;
			break;

		case GameStatus.NORMAL_MODE:	// to gameplay mode
			SnowObjects.SetActive(true);
			SnowBallBattleStage.SetActive(true);
			SkyDoom.GetComponent<Renderer>().material = SkyDoomMaterials[1];
			NormalCamera.SetActive(false);
			this.GetComponent<AudioSource>().Play();
			NowGameStatus = GameStatus.GAME_MODE;
			ChangeStage ();
			//InitGame();
			break;
		}

	}


	public void ChangeStage () {

		SnowBallBattleStage.transform.localPosition = GameStages[playStageNum].transform.localPosition;
		SnowBallBattleStage.transform.localEulerAngles = GameStages[playStageNum].transform.localEulerAngles;
		playStageNum++;
		if (playStageNum >= GameStages.Length)
		{
			playStageNum = 0;
		}
		InitGame();

	}

	void InitGame () {

		// Destroy All game item
		GameObject targetSnowBallSet = GameObject.Find("SnowBallSet");
		if (targetSnowBallSet != null)
		{
			Destroy(targetSnowBallSet.gameObject);
		}

		GameObject[] targetSnowBalls;
		targetSnowBalls = GameObject.FindGameObjectsWithTag("SnowBall");
		foreach (GameObject targetSnowBall in targetSnowBalls)
		{
			Destroy(targetSnowBall.gameObject);
		}

		if (NowGameStatus == GameStatus.GAME_MODE)
		{
			// Instantiate game item
			GameObject gameItem = Instantiate(SnowBallSetPrefab) as GameObject;
			gameItem.name = "SnowBallSet";
			gameItem.transform.parent = SnowBallBattleStage.transform;
			gameItem.transform.localPosition = new Vector3(0, 0, 0);
			gameItem.transform.localEulerAngles = new Vector3(0, 0, 0);
		}
	}


}
