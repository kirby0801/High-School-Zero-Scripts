using UnityEngine;
using System.Collections;

public class GameManagerNew : MonoBehaviour 
{

	private static GameManagerNew _instance = null;
	
	public static GameManagerNew instance 
	{
		get { return _instance; }
	}

	public SpriteRenderer black;
	public bool fadingIn = true;
	public bool fadingOut = false;

	public bool hasLost = false;
	public GameObject gameOverScreen;

	public Event introEvent;
	public Event tutorialEvent;

	public GameObject arrow;

	public Vector3 [] mapLocations;
	public GirlIcon [] girlIcons;

	public LocationButton [] locationButtons;

	public int numFailed;

	public int testChance = 66;
	public int[] baseTestChance;

	public bool playerWin = false;
	public bool isLoadingScene = false;

	public TimeAndPlaceManager timeAndPlace;

	void Awake ()
	{

		if (_instance) 
		{
			Destroy(gameObject);
		}
		
		else 
		{
			_instance = this;
		}

		DontDestroyOnLoad (this);
	}
	
	void Start () 
	{
//		StartCoroutine("fadeIn");
		UpdateTimeOfDay ();
		
		updateGirls ();
		updateCalendar ();
	}

	void OnLevelWasLoaded()
	{
		StartCoroutine ("fadeIn");
		isLoadingScene = false;
		UpdateTimeOfDay ();
		updateCalendar ();
		updateGirls ();
		updateLocationButtons ();
	}

	void updateGirls()
	{
		foreach (GirlIcon g in girlIcons)
		{
			g.deactivate ();
		}

		if (Application.loadedLevelName == "Map")
		{
			// Show active girls on map
			foreach (Girl g in Data.instance.girls)
			{
				foreach (GirlIcon gi in girlIcons)
				{
					if (g.getName () == gi.getName ()) // Matching Names
					{
						foreach (string s in g.availability)
						{
							if (s == timeOfDay) // Matching Time
							{
								gi.activate();
							}
						}
					}
				}
			}
		}
	}

	void updateLocationButtons()
	{
		if (Application.loadedLevelName == "Map")
		{
			foreach (LocationButton b in locationButtons)
			{
				b.gameObject.SetActive(true);
				b.show();
				b.deactivate();
			}

			if (PlayerPrefs.GetInt ("Player Has Started", 0) == 0)
			{
				locationButtons[1].activate ();
			}
			else if ((currTime  - 1) % 2 == 0)
			{
				locationButtons[0].activate ();
			}
			else
			{
				foreach (LocationButton b in locationButtons)
				{
					b.activate();
				}
			}
		}
		else
		{
			foreach (LocationButton b in locationButtons)
			{
				b.hide();
				b.gameObject.SetActive(false);
			}
		}
	}

	public void spendTime()
	{
		currTime++;
		UpdateTimeOfDay();
	}

	void UpdateTimeOfDay()
	{
		timeAndPlace.UpdateTimeAndPlace ();
	}

	public void receiveLocation (string location)
	{
		unhighlightLocation(location);
		currentLocation = location;
		loadScene ("location");
	}

	public void receiveCommand(string command)
	{
		switch (command)
		{
			case "play again":
			startGame ();
			break;

			case "main menu":
			loadScene ("mainmenu");
			break;

			case "quit":
			Application.Quit ();
			break;
		}
	}
	
	public void loadScene(string type)
	{
		if (!isLoadingScene)
		{
			StartCoroutine (actualLoadScene (type));
			isLoadingScene = true;
		}
	}

	public IEnumerator actualLoadScene(string type)
	{
		yield return StartCoroutine("fadeOut");

		switch (type)
		{
		case "location":
			if (PlayerPrefs.GetInt ("Player Has Started", 0) == 0)
			{
				Data.instance.setEvent (tutorialEvent);
				Application.LoadLevel ("Event");
			}
			else if (Data.instance.getGirlByLocationAndTime(currentLocation, timeOfDay)) Application.LoadLevel ("Girl");
			else if (hasClass())
			{
				float randomizer = Random.Range (1, 100);
				
				if (randomizer < testChance)
				{
					testChance = baseTestChance[day];
					Application.LoadLevel ("Battle");
				}
				else
				{
					testChance += testChance;
					Application.LoadLevel ("RandomEvent");
				}
			}
			else Application.LoadLevel ("RandomEvent");
			break;	
			
		case "battle":
			Application.LoadLevel ("Battle");
			break;
			
		case "event":
			Application.LoadLevel ("Event");	
			break;
			
		case "random":
			Application.LoadLevel ("RandomEvent");	
			break;
			
		case "map":
			Application.LoadLevel ("Map");
			break;

			case "naming":
			Application.LoadLevel ("Name Your Character");
			break;

			case "intro":
			Data.instance.setEvent (introEvent);
			Application.LoadLevel ("Event");
			break;

			case "mainmenu":
			Application.LoadLevel ("Main Menu");
			break;

			case "ending":
			Application.LoadLevel ("Ending");
			break;
		}
	}

	public void endEvent()
	{
		if (!hasLost)
		{
			if (playerWin)
			{
				loadScene ("ending");
			}
			else if (numFailed >= 3)
			{
				receiveLocation("failed");
				
				UpdateTimeOfDay();
			}
			else
			{
				if (timeOfDay == "Before School")
				{
					receiveLocation("on way to school");
					
					UpdateTimeOfDay();
				}
				else if (timeOfDay == "Late Afternoon")
				{
					receiveLocation("on way home");
					
					UpdateTimeOfDay();
				}
				else if (timeOfDay == "Evening")
				{
					receiveLocation("home");
					
					UpdateTimeOfDay();
				}
				else
				{
					if (currTime >= 8)
					{
						day++;
						if (day > 22) receiveLocation ("passed");
						else
						{
							currTime = -1;
							updateCalendar();
							receiveLocation("on way to school");
						}

						
						UpdateTimeOfDay();
						testChance = baseTestChance[day];

					}
					else
					{
					loadScene ("map");
					currentLocation = "map";
					
					UpdateTimeOfDay ();
					}
				}
			}
		}
	}

	public void updateCalendar()
	{
		calendarDays.text = day.ToString ();
	}

	public void gameOver ()
	{
		gameOverScreen.SetActive (true);
		hasLost = true;
	}

	public bool isGameOver()
	{
		return hasLost;
	}

	public void youWin ()
	{

	}

	public bool hasClass()
	{
		if ((currTime == 1) || (currTime == 3) || (currTime == 5)) return true;
		return false;
	}

	public IEnumerator fade()
	{
		Color alpha = new Color (0, 0, 0, 0);
		while (black.color.a != 1)
		{
			yield return new WaitForSeconds(0.01f);
			alpha.a += 0.1f;
			black.color = alpha;
		}

		alpha = new Color (0, 0, 0, 1);
		while (black.color.a != 0)
		{
			yield return new WaitForSeconds(0.01f);
			alpha.a -= 0.1f;
			black.color = alpha;
		}
	}

	public IEnumerator fadeOut()
	{
		black.gameObject.SetActive (true);

		Color alpha = new Color (0, 0, 0, 0);
		while (black.color.a < 1)
		{
			yield return new WaitForSeconds(0.01f);
			alpha.a += 0.2f;
//			black.color.a += 0.1f;
			black.color = alpha;
		}

		yield return 0;
	}

	public IEnumerator fadeIn()
	{
		Color alpha = new Color (0, 0, 0, 1);
		while (black.color.a > 0)
		{
			yield return new WaitForSeconds(0.01f);
			alpha.a -= 0.2f;
			black.color = alpha;
		}

		black.gameObject.SetActive (false);


		yield return 0;
	}

	public void startGame()
	{
		Random.seed = (int) System.DateTime.Now.Ticks;

		if (PlayerPrefs.GetInt ("Player Has Started", 0) == 1)
		{
			numFailed = 0;
			gameOverScreen.SetActive (false);
			day = 1;

			Player.instance.setStatsToDefault();
			Data.instance.resetGirlPoints ();
			playerWin = false;

			currTime = -1;
			receiveLocation("on way to school");
			hasLost = false;
		}
		else
		{
			loadScene ("naming");
		}
	}

	public void highlightLocation(string location)
	{
		arrow.SetActive (true);

		switch(location)
		{
			case "classroom":
			arrow.transform.position = mapLocations[0];
			break;
			
			case "library":
			arrow.transform.position = mapLocations[1];
			break;
			
			case "gym":
			arrow.transform.position = mapLocations[2];
			break;
			
			case "bathroom":
			arrow.transform.position = mapLocations[3];
			break;
			
			case "hallway":
			arrow.transform.position = mapLocations[4];
			break;
		}

		foreach (GirlIcon g in girlIcons)
		{
			if (g.location == location) g.highlight ();
		}

	}

	public void unhighlightLocation(string location	)
	{
		arrow.SetActive (false);
		foreach (GirlIcon g in girlIcons)
		{
			if (g.location == location) g.unhighlight ();
		}
	}

	void OnDrawGizmosSelected()
	{
		foreach (Vector3 v in mapLocations)
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawSphere(v,0.3f);
		}
	}

	public void failedTest()
	{
		numFailed++;
	}

	public void playerWon()
	{
		playerWin = true;
	}

}
