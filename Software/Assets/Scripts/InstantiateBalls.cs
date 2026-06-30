using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;
using System.Linq;

public class InstantiateBalls : MonoBehaviour
{
	[Serializable]
	public enum Level {Easy, Medium, Hard};

	[Serializable]
	public enum StimulusMode {Reward, ArmID, DirectionArm, DirectionStatic, DirectionDynamic};


	[Serializable]
	public enum AudioStimulusMode { DirectionStatic, DirectionDynamic };



	[Serializable]
	public enum StimulusType {Haptic, Audio, AudioHaptic, None};

	public AudioClip[] sonidos;


	[Serializable]
	public struct LevelConfig 
	{
		public Level level;
		public int lines;
		public int columns;
	}

	[Tooltip("0: Left hand; 1: Right hand")]
	public GameObject[] handVisual;

	private AudioSource audioManager;

	public HapticUDPController[] handHaptic;

	public Level chosenLevel;
	public StimulusMode chosenStimulusMode;
	public StimulusType chosenStimulusType;
	public AudioStimulusMode chosenAudioMode;
	
	public int pulseDuration = 500, intensity = 255;
	private LevelConfig nivel;

	public int numberTouchMax = 10;
	public float angleVision = 60f;
	public float height = 0.5f;
	public float radius = 0.6f;

	public float sizeSpheres = 0.1f;

	public bool realGame, randomizeConditions;
	public string userName;
	public GameObject primitiveToInstantiate;
	public int nbBlocMax,trialNumber;
	private int config;

	public float stopWatch;
	private float startStopWatchTime;

	public List<(int, float)> results;

	private string time0;
	[HideInInspector]
	public int state;
	private GameObject panelStart, interactiveObject;

	[Tooltip("List of Past Configurations")]
	public List<int> configException;

	private GameObject objectToLoad;

	private string path;
	private StreamWriter writer;
	private int nbBloc;
	private bool startOver;

	private int[] leftOrRight;

	private bool firstTimeHere, audiofirstTimeHere;
	private int motorUp, motorLeft, motorRight, motorDown;

	private GameObject panelInstructions;

	// Called in Editor when you change a value in Inspector
	void OnValidate() => LoadConfig();

	// Called at runtime
	void Awake() => LoadConfig();

    void Start()
    {
		// order for Sound -> reward; left; right; leftToRight; RightToLeft
		sonidos = new AudioClip[6];
		sonidos[0] = Resources.Load("correct") as AudioClip;
		sonidos[1] = Resources.Load("vysium_audios_burst_WideBand_Left") as AudioClip;
		sonidos[2] = Resources.Load("vysium_audios_burst_WideBand_Right") as AudioClip;
		sonidos[3] = Resources.Load("vysium_audios_burst_WideBand_LeftToRight") as AudioClip;
		sonidos[4] = Resources.Load("vysium_audios_burst_WideBand_RightToLeft") as AudioClip;
		sonidos[5] = Resources.Load("vysium_audios_burst_WideBand_Center") as AudioClip;

		time0 = System.DateTime.Now.ToString("ddMMyyyy-HHmm");
		state = -3;


		handVisual = new GameObject[2];
		handVisual[0] = this.GetComponent<CalibrateHandPosition>().handLeftObject;
		handVisual[1] = this.GetComponent<CalibrateHandPosition>().handRightObject;

		panelInstructions = GameObject.Find("PanelInstructions");


		// RecordPerformance();
	}

	// Update is called once per frame
	void Update()
    {
        for(int m = 0; m < configException.Count; m++)
		{
			for(int p = 0; p < configException.Count; p++)
			{
				if(p != m)
				{
					if(configException[m] == configException[p])
					{
						configException.Remove(configException[m]);
					}
				}
			}
		}


		switch(state)
    	{
    		case -3:

    			leftOrRight = new int[numberTouchMax];
    			for(int i = 0; i < numberTouchMax; i++)
    			{
    				leftOrRight[i] = i < numberTouchMax/2 ? 0 : 1;
    			}
    			leftOrRight = leftOrRight.OrderBy(_ => UnityEngine.Random.value).ToArray();


    			GameObject parentBall = GameObject.Find("Balls");
    			for(int j = 0; j < nivel.columns; j++)
    			{
    				for(int i = 0; i < nivel.lines; i++)
    				{
    					float theta = (i*angleVision / (float)nivel.lines);
    					Vector3 userOrigin = GameObject.Find("CenterEyeAnchor").transform.position;
    					Vector3 positionBall = new Vector3(radius*Mathf.Cos(theta*Mathf.Deg2Rad), j*height/(float)nivel.columns - height/Mathf.Floor(nivel.columns/2), radius*Mathf.Sin(theta*Mathf.Deg2Rad)) + userOrigin;
    					GameObject interactiveBalls = (GameObject)Instantiate(primitiveToInstantiate, positionBall, Quaternion.identity, parentBall.transform);
    					interactiveBalls.name = "Ball_" + i.ToString() + "_" + j.ToString(); 
    					interactiveBalls.transform.localScale = Vector3.one * sizeSpheres;
    				}
					

    				

    			}
				primitiveToInstantiate.SetActive(false);
				state = -2;
				break;


    		case -2:

				this.GetComponent<CalibrateHandPosition>().enabled = false;
				// Introduction // CALIBRATION

				// HERE WE NEED TO INIT IMU; AND CALIBRATE -> this determines which hapticUDP is haptic left or haptic right
				// This also determines who is motor left, right, bottom, up

				// HERE PRESENTS PANEL START
				// 		for(int i = 0; i < panelStart.transform.childCount; i++)
				// 		{
				// 			if(panelStart.transform.GetChild(i).GetComponent<CollideAndDisappear>().isTouched)
				// 			{
				// 				panelStart.SetActive(false);
				// 				state = -1;
				// 			}
				// 		}

				// 		if(UnityEngine.Input.GetKeyDown(KeyCode.Space))
				// 		{
				// panelStart.SetActive(false);
				// 			state = -1;
				// 		}

				handHaptic = new HapticUDPController[this.GetComponents<HapticUDPController>().Length];
				for(int i = 0; i < this.GetComponents<HapticUDPController>().Length; i++)
                {
					if(this.GetComponents<HapticUDPController>()[i].realArmID == HapticUDPController.ArmID.Left)
                    {
						handHaptic[0] = this.GetComponents<HapticUDPController>()[i];
					}
					if (this.GetComponents<HapticUDPController>()[i].realArmID == HapticUDPController.ArmID.Right)
					{
						handHaptic[1] = this.GetComponents<HapticUDPController>()[i];
					}
				}

				StartCoroutine(Instructions());

				motorLeft = 2;
	    		motorUp = 1;
	    		motorRight = 0;
	    		motorDown = 3;

				state = -1;
    			break;

    		case -1:
    			if(randomizeConditions)
				{
					config = UnityEngine.Random.Range(0, nivel.lines * nivel.columns);
					while(configException.Contains(config))
					{
						config = UnityEngine.Random.Range(0, nivel.lines * nivel.columns);
					}
				}
				else
				{
					config = configException.Count; // + 1;
				}

				state = 0;
				break;

    		case 0:
				configException.Add(config);

				objectToLoad = GameObject.Find("Balls").transform.GetChild(config).gameObject;

				// LOAD CONDITIONS
				objectToLoad.AddComponent<AudioSource>();

				audioManager = new AudioSource();
				audioManager = objectToLoad.GetComponent<AudioSource>();
				audioManager.playOnAwake = false;

				switch(chosenStimulusMode)
                {
					case StimulusMode.ArmID:
						audioManager.clip = leftOrRight[trialNumber] == 0 ? sonidos[1] : sonidos[2];
						break;

					case StimulusMode.Reward:
						audioManager.clip = sonidos[0];
						break;

				}


				objectToLoad.AddComponent<CollideAndDisappear>();
				objectToLoad.GetComponent<CollideAndDisappear>().playSound = chosenStimulusType == StimulusType.Haptic ? false : true;

				interactiveObject = handVisual[leftOrRight[trialNumber]];

				if(interactiveObject.transform.childCount != 0)
				{ 
				
					for(int i = 0; i < interactiveObject.transform.Find("Capsules").transform.childCount; i++)
					{
						if (interactiveObject.transform.Find("Capsules").transform.GetChild(i).transform.GetChild(0).GetComponent<Rigidbody>() == null)
						{
							interactiveObject.transform.Find("Capsules").transform.GetChild(i).transform.GetChild(0).gameObject.AddComponent<Rigidbody>();
						}
					
						interactiveObject.transform.Find("Capsules").transform.GetChild(i).transform.GetChild(0).GetComponent<Rigidbody>().isKinematic = true;
						interactiveObject.transform.Find("Capsules").transform.GetChild(i).transform.GetChild(0).GetComponent<Rigidbody>().useGravity = false;
						interactiveObject.transform.Find("Capsules").transform.GetChild(i).transform.GetChild(0).GetComponent<Collider>().isTrigger = true;
						interactiveObject.transform.Find("Capsules").transform.GetChild(i).transform.GetChild(0).gameObject.tag = "InteractiveObject";

					}
				}
				if(handVisual[(leftOrRight[trialNumber] + 1) % 2].transform.childCount != 0)
                {
					for (int i = 0; i < handVisual[(leftOrRight[trialNumber] + 1) % 2].transform.Find("Capsules").transform.childCount; i++)
					{
						handVisual[(leftOrRight[trialNumber] + 1) % 2].transform.Find("Capsules").transform.GetChild(i).GetChild(0).gameObject.tag = "Untagged";

					}
				}
				

    			startStopWatchTime = Time.time;
    			firstTimeHere = true;
				audiofirstTimeHere = true;
				stopWatch = 0;
				//state = 1;
				state = 8;
				StartCoroutine(WaitBetweenSignals());
    			// update countDown = 0
    			// instantiate new sphere, condition i

    			break;
			case 8:

				break;

    		case 1:
    			stopWatch = Time.time - startStopWatchTime;
				Debug.DrawRay(GameObject.Find("CenterEyeAnchor").transform.position, GameObject.Find("CenterEyeAnchor").transform.forward*radius*1.1f, Color.yellow);


    			// HERE ADD: if firstHere: if stimulus NOTIFICATION -> vibration on correct hand when isTouched
    			// If stimulus direction -> 
    			if(firstTimeHere)
    			{
					if((chosenStimulusType == StimulusType.Haptic) || (chosenStimulusType == StimulusType.AudioHaptic))
					{ 
						if (chosenStimulusMode == StimulusMode.ArmID)
    					{
    					
    						for(int motor = 0; motor < 4; motor++)
		    				{
		    					handHaptic[leftOrRight[trialNumber]].Pulse(motor, pulseDuration, intensity);
		    				}
							firstTimeHere = false;

						}

						// send vib to arm ID
						// StimulusManager.mode, type -> pulse

						if (chosenStimulusMode == StimulusMode.DirectionArm)
						{
							for (int k = 0; k < handHaptic.Length; k++)
							{
								RaycastHit hit;
								if (Physics.Raycast(GameObject.Find("CenterEyeAnchor").transform.position, GameObject.Find("CenterEyeAnchor").transform.forward * radius * 1.1f, out hit))
								{
                                    Debug.DrawRay(GameObject.Find("CenterEyeAnchor").transform.position, GameObject.Find("CenterEyeAnchor").transform.forward * hit.distance, Color.yellow);
                                    Vector3 lookingHere = hit.point;
									if (objectToLoad.transform.position.x - lookingHere.x < -0.2f)
									{
										for (int motor = 0; motor < 4; motor++)
										{
											handHaptic[0].Pulse(motor, pulseDuration, intensity);
										}
									}
									if (objectToLoad.transform.position.x - lookingHere.x > 0.2f)
									{
										for (int motor = 0; motor < 4; motor++)
										{
											handHaptic[1].Pulse(motor, pulseDuration, intensity);
										}
									}
                                    if ((objectToLoad.transform.position.x - lookingHere.x > -0.2f) && (objectToLoad.transform.position.x - lookingHere.x < 0.2f))
                                    {
										for (int motor = 0; motor < 4; motor++)
										{
											handHaptic[leftOrRight[trialNumber]].Pulse(motor, pulseDuration, intensity);
										}
									}
                                    //Debug.Log("Distance: " + (objectToLoad.transform.position.x - lookingHere.x));
                                    firstTimeHere = false;
								}
							}
						}

						if (chosenStimulusMode == StimulusMode.DirectionStatic)
						{
							for (int k = 0; k < handHaptic.Length; k++)
							{
								RaycastHit hit;

								if (Physics.Raycast(GameObject.Find("CenterEyeAnchor").transform.position, GameObject.Find("CenterEyeAnchor").transform.forward * radius * 1.1f, out hit))
								{									
                                    Vector3 lookingHere = hit.point;
									if (objectToLoad.transform.position.x - lookingHere.x < -0.2f)
									{
										handHaptic[k].Pulse(motorLeft, pulseDuration, intensity);
									}
									if (objectToLoad.transform.position.x - lookingHere.x > 0.2f)
									{
										handHaptic[k].Pulse(motorRight, pulseDuration, intensity);
									}
                                    //if ((objectToLoad.transform.position.x - lookingHere.x > -0.2f) && (objectToLoad.transform.position.x - lookingHere.x < 0.2f))
                                    //{
                                    //    handHaptic[k].Pulse(motorLeft, pulseDuration, intensity);
                                    //}
                                    if (objectToLoad.transform.position.y - lookingHere.y < -0.2f)
									{
										handHaptic[k].Pulse(motorDown, pulseDuration, intensity);
									}
									if (objectToLoad.transform.position.y - lookingHere.y > 0.2f)
									{
										handHaptic[k].Pulse(motorUp, pulseDuration, intensity);
									}
									firstTimeHere = false;
								}
							}
						}
						if (chosenStimulusMode == StimulusMode.DirectionDynamic)
						{
							// send vib sweeping three motors one after the other on both arms
							RaycastHit hit;
							if (Physics.Raycast(GameObject.Find("CenterEyeAnchor").transform.position, GameObject.Find("CenterEyeAnchor").transform.forward * radius * 1.1f, out hit))
							{
								Vector3 lookingHere = hit.point;
								if (objectToLoad.transform.position.x - lookingHere.x < -0.2f)
								{
									StartCoroutine(SweepingDirection(0, 2));
								}
								if (objectToLoad.transform.position.x - lookingHere.x > 0.2f)
								{
									StartCoroutine(SweepingDirection(2, 0));
								}

								if (objectToLoad.transform.position.y - lookingHere.y < -0.2f)
								{
									StartCoroutine(SweepingDirection(1, 3));
								}
								if (objectToLoad.transform.position.y - lookingHere.y > 0.2f)
								{
									StartCoroutine(SweepingDirection(3, 1));
								}
								firstTimeHere = false;
							}

						}
					}
				}

				if(audiofirstTimeHere)
				{ 
					if ((chosenStimulusType == StimulusType.Audio) || (chosenStimulusType == StimulusType.AudioHaptic))
					{

						RaycastHit hit;
						if (Physics.Raycast(GameObject.Find("CenterEyeAnchor").transform.position, GameObject.Find("CenterEyeAnchor").transform.forward * radius * 1.1f, out hit))
						{
							Vector3 lookingHere = hit.point;
							if (objectToLoad.transform.position.x - lookingHere.x < -0.2f)
							{
								if(chosenAudioMode == AudioStimulusMode.DirectionStatic)
                                {
									audioManager.clip = sonidos[1];
								}
								if (chosenAudioMode == AudioStimulusMode.DirectionDynamic)
								{
									audioManager.clip = sonidos[3];
								}
								audioManager.Play();
								StartCoroutine(SwitchAudio());
							}
							if (objectToLoad.transform.position.x - lookingHere.x > 0.2f)
							{
								if (chosenAudioMode == AudioStimulusMode.DirectionStatic)
								{
									audioManager.clip = sonidos[2];
								}
								if (chosenAudioMode == AudioStimulusMode.DirectionDynamic)
								{
									audioManager.clip = sonidos[4];
								}
								audioManager.Play();
								StartCoroutine(SwitchAudio());
							}
                            if ((objectToLoad.transform.position.x - lookingHere.x > -0.2f) && (objectToLoad.transform.position.x - lookingHere.x < 0.2f))
                            {
								audioManager.clip = sonidos[5];
								audioManager.Play();
								StartCoroutine(SwitchAudio());
                            }
                            audiofirstTimeHere = false;
						}
					}

				}

				if (objectToLoad.GetComponent<CollideAndDisappear>().isTouched)
    			{
					if((chosenStimulusType == StimulusType.Haptic) || (chosenStimulusType == StimulusType.AudioHaptic))
                    {
						for (int motor = 0; motor < 4; motor++)
						{
							handHaptic[leftOrRight[trialNumber]].Pulse(motor, (int)Mathf.Floor(pulseDuration / 2), intensity);
						}
					}
					
                }

    			if(objectToLoad.GetComponent<CollideAndDisappear>().finishedVisual)
    			{
					state = 2;

                }

    			// StartCoroutine(WaitForCollision());
    			// in coroutine -> record time
    			break;

    		case 2:
		    	// Destroy(objectToLoad);
				
    			Destroy(objectToLoad.GetComponent<CollideAndDisappear>());
				objectToLoad.GetComponent<Renderer>().material.color = Color.white;
		    	//RecordPerformance(nbBloc, trialNumber, config, stopWatch);
		    	trialNumber = trialNumber + 1;
    			if(configException.Count >= numberTouchMax)
				{
					nbBloc = nbBloc + 1;
					if(nbBloc >= nbBlocMax)
					{
						Application.Quit();
						Debug.Break();
					}
					else
					{
						startOver = true;
					}
				}
				else
				{
					
                    state = -1;
                }

                if (startOver)
				{
					configException.Clear();
					configException = new List<int>();
					startOver = false;
					state = -1;
				}

    			// Destroy sphere
    			// if nbBloc == nbBlocMax, trial=trialMax - Finish
    			// else go back to 0
    			break;

    	}

    }

	IEnumerator WaitBetweenSignals()
    {
		yield return new WaitForSeconds(0.5f);
		objectToLoad.GetComponent<Renderer>().material.color = leftOrRight[trialNumber] == 0 ? handVisual[0].GetComponentInChildren<Renderer>().material.color : handVisual[1].GetComponentInChildren<Renderer>().material.color;

		state = 1;
		
	}

	IEnumerator SwitchAudio()
    {
		yield return new WaitForSeconds(audioManager.clip.length);
		audioManager.clip = sonidos[0];
    }
	public void LoadConfig() {
	    nivel.level = chosenLevel;

	    switch (chosenLevel) {
	        case Level.Easy:
	           nivel.lines = 8;
	           nivel.columns = 4;
	           break;
	        case Level.Medium:
	         	nivel.lines = 10;
	         	nivel.columns = 6;
	         	break;
	        case Level.Hard:
	        	nivel.lines = 12;
	        	nivel.columns = 8;
	        	break;
	    }
	}

    void RecordPerformance(int blockID = 0, int trialID = 0, int configID = 0, float clock = 0)
    {

		if(!realGame)
		{
	    	path = "Assets/Resources/DataCollection/" + userName + "-" + time0 + ".csv";
		}
		else
		{
	    	path = Application.dataPath + "/Hand-Touch-Countdown" + time0 + ".csv";
		}

    	
    	
    	if(state == -2)
    	{
    		writer = new StreamWriter(path, true);
			writer.WriteLine("BlockID;TrialID;Config;StopWatch");
			writer.Close();
    	}
    	else
    	{
    		writer = new StreamWriter(path, true);
			writer.WriteLine(blockID + ";" + trialID + ";" + configID + ";" + clock.ToString());
			writer.Close();
    	}
    }

    IEnumerator SweepingDirection(int motorStart, int motorFinish)
    {
    	int sign = (int)Mathf.Sign(motorFinish - motorStart);
    	for(int i = motorStart; i*sign < motorFinish*sign + 1; i = i + sign)
    	{
    		//Debug.Log("Sweeping Motor " + i);
    		for(int k = 0; k < handHaptic.Length; k++)
    		{
    			handHaptic[k].Pulse(i, (int)Mathf.Floor((float)(pulseDuration)), intensity);
    		}
    		yield return new WaitForSeconds((float)1.0f);
    	}
    }

	IEnumerator Instructions()
	{
		string wordStimulus = "";
		string endPhrase = "\n Estimulo " + trialNumber.ToString() + "/" + numberTouchMax.ToString();

		switch(chosenStimulusType)
        {
			case StimulusType.Audio:
				wordStimulus = "auditivo";
				break;


			case StimulusType.Haptic:
				wordStimulus = "de vibración";
				break;

			case StimulusType.AudioHaptic:
				wordStimulus = "de vibración y auditivo";
				break;

			case StimulusType.None:
				panelInstructions.transform.GetChild(0).gameObject.GetComponent<TextMeshPro>().text = "Toca la pelota verde con la mano verde, y azul con la mano azul." + endPhrase;
				break;

		}
		while(trialNumber < numberTouchMax)
        {
			if(chosenStimulusType != StimulusType.None)
            {

            
				switch(chosenStimulusMode)
				{
					case StimulusMode.ArmID:
						panelInstructions.transform.GetChild(0).gameObject.GetComponent<TextMeshPro>().text = "El estimulo " + wordStimulus +  " te indica \n con que mano interactuar." + endPhrase;

						break;

					case StimulusMode.Reward:
						panelInstructions.transform.GetChild(0).gameObject.GetComponent<TextMeshPro>().text = "¡El estimulo " + wordStimulus + " te indica \n que has hecho bien!" + endPhrase;

						break;

					case StimulusMode.DirectionStatic:
						panelInstructions.transform.GetChild(0).gameObject.GetComponent<TextMeshPro>().text = "¡El estimulo " + wordStimulus + " te indica \n donde está la pelota!" + endPhrase;

						break;
					case StimulusMode.DirectionDynamic:
						panelInstructions.transform.GetChild(0).gameObject.GetComponent<TextMeshPro>().text = "¡El estimulo " + wordStimulus + " te indica que dirección \n seguir para encontrar la pelota!" + endPhrase;

						break;
				}
			}

			yield return null;
        }


	}


}
