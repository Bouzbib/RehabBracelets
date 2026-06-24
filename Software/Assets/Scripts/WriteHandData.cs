using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using System.IO;


public class WriteHandData : MonoBehaviour
{
    [Header("Recording Controls")]
    [Tooltip("Key used to start/stop recording.")]
    public KeyCode toggleRecordingKey = KeyCode.Space;

    [Tooltip("If true, recording starts automatically when the scene plays.")]
    public bool startRecordingOnPlay = false;

    [Header("Output")]
    [Tooltip("Optional custom file name (without extension). Leave empty to auto-generate a timestamped name.")]
    public string customFileName = "";


	private StreamWriter writer;
	private string path;

	public int state = 2;

    private static float time0;
	private string time1;

	public CalibrateHandPosition calibratingPosition;
	public IMUReceiver[] imuReceiver;

	public GameObject[] trackedObjects;
	private int frame = 0;

    private void Start()
    {

        time0 = Time.time;
		time1 = System.DateTime.Now.ToString("ddMMyyyy-HHmm");
		path = "Assets/Resources/DataCollection/HandCalib-" + customFileName + "-" + time1 + ".csv";
		calibratingPosition = this.GetComponent<CalibrateHandPosition>();

		imuReceiver = new IMUReceiver[2];
		for(int i = 0; i < GetComponents<IMUReceiver>().Length; i++)
        {
			if(GetComponents<IMUReceiver>()[i].armID == HapticUDPController.ArmID.Left)
            {
				imuReceiver[0] = GetComponents<IMUReceiver>()[i];
			}
			if (GetComponents<IMUReceiver>()[i].armID == HapticUDPController.ArmID.Right)
			{
				imuReceiver[1] = GetComponents<IMUReceiver>()[i];
			}

		}

		trackedObjects = new GameObject[4];
		trackedObjects[2] = calibratingPosition.wristR;
		trackedObjects[3] = calibratingPosition.palmR;
		trackedObjects[0] = calibratingPosition.wristL;
		trackedObjects[1] = calibratingPosition.palmL;


	}

	private void Update()
    {

		trackedObjects[2] = calibratingPosition.wristR;
		trackedObjects[3] = calibratingPosition.palmR;
		trackedObjects[0] = calibratingPosition.wristL;
		trackedObjects[1] = calibratingPosition.palmL;


		switch (state)
		{
			case 0:
				path = "Assets/Resources/DataCollection/HandCalib-" + customFileName + "-" + time1 + ".csv";
				writer = new StreamWriter(path, true);

				writer.Write("Frame;Time;Acc1X;Acc1Y;Acc1Z;Gy1X;Gy1Y;Gy1Z;");
				writer.Write("Acc2X;Acc2Y;Acc2Z;Gy2X;Gy2Y;Gy2Z;");
				writer.Write("WristLX;WristLY;WristLZ;WristLRotX;WristLRotY;WristLRotZ;");
				writer.Write("PalmLX;PalmLY;PalmLZ;PalmLRotX;PalmLRotY;PalmLRotZ;");
				writer.Write("WristRX;WristRY;WristRZ;WristRRotX;WristRRotY;WristRRotZ;");
				writer.Write("PalmRX;PalmRY;PalmRZ;PalmRRotX;PalmRRotY;PalmRRotZ");

				writer.Close();

				state = 1;
				break;

			case 1:
				frame = frame + 1;
				path = "Assets/Resources/DataCollection/HandCalib-" + customFileName + "-" + time1 + ".csv";
				writer = new StreamWriter(path, true);
				writer.WriteLine();
				writer.Write(frame + ";" + (Time.unscaledTime - time0));
				for(int i = 0; i < imuReceiver.Length; i++)
                {
					writer.Write(";" + imuReceiver[i]._ax + ";" + imuReceiver[i]._ay + ";" + imuReceiver[i]._az + ";" + imuReceiver[i]._gx + ";" + imuReceiver[i]._gy + ";" + imuReceiver[i]._gz);
				}
				
		    	for(int i = 0; i < trackedObjects.Length; i++)
		    	{
					writer.Write(";" + trackedObjects[i].transform.position.x.ToString("F4") + ";" + trackedObjects[i].transform.position.y.ToString("F4") + ";" + trackedObjects[i].transform.position.z.ToString("F4") + ";" + calibratingPosition.AngularDistance(trackedObjects[i].transform.eulerAngles.x).ToString("F4") + ";" + calibratingPosition.AngularDistance(trackedObjects[i].transform.eulerAngles.y).ToString("F4") + ";" + calibratingPosition.AngularDistance(trackedObjects[i].transform.eulerAngles.z).ToString("F4"));
				}

		    	writer.Close();

		    	if(calibratingPosition.finishCalibrating)
		    	{
		    		state = 3;
		    	}

				break;

			case 2:
				if(calibratingPosition.startCalibrating || Input.GetKeyDown(toggleRecordingKey))
				{
					time0 = Time.time;
					state = 0;
					calibratingPosition.startCalibrating = false;
				}

				break;

			case 3:
				Debug.Log("Finished recording");
				break;
		}
    }

	

}