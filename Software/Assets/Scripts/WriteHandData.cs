using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using System.IO;


public class WriteHandData : MonoBehaviour
{
    [Header("Hand Skeletons")]
    [Tooltip("OVRSkeleton component for the left hand (OVRSkeleton.SkeletonType = HandLeft)")]
    public OVRSkeleton handLeft;

    [Tooltip("OVRSkeleton component for the right hand (OVRSkeleton.SkeletonType = HandRight)")]
    public OVRSkeleton handRight;

    [Header("Recording Controls")]
    [Tooltip("Key used to start/stop recording.")]
    public KeyCode toggleRecordingKey = KeyCode.Space;

    [Tooltip("If true, recording starts automatically when the scene plays.")]
    public bool startRecordingOnPlay = false;

    [Header("Output")]
    [Tooltip("Optional custom file name (without extension). Leave empty to auto-generate a timestamped name.")]
    public string customFileName = "";


	private StreamWriter writerL, writerR;
	private string pathR, pathL;

	public int state = 2;

    private static float time0;
	private string time1;

	public CalibrateHandPosition calibratingPosition;
	public IMUReceiver imuReceiver;

    private void Start()
    {

        time0 = Time.time;
		time1 = System.DateTime.Now.ToString("ddMMyyyy-HHmm");
		pathL = "Assets/Resources/DataCollection/Left-" + time1 + ".csv";
		pathR = "Assets/Resources/DataCollection/Right-" + time1 + ".csv";
		calibratingPosition = this.GetComponent<CalibrateHandPosition>();

		imuReceiver = GetComponent<IMUReceiver>();

    }

    private void Update()
    {


        switch(state)
		{
			case 0:
				pathL = "Assets/Resources/DataCollection/Left-" + time1 + ".csv";
				pathR = "Assets/Resources/DataCollection/Right-" + time1 + ".csv";
		        writerL = new StreamWriter(pathL, true);
		        writerR = new StreamWriter(pathR, true);
				writerL.WriteLine("Time;AccX;AccY;AccZ;GyX;GyY;GyZ");
				for(int i = 0; i < handLeft.GetComponent<OVRSkeleton>().Bones.Count; i++)
		    	{
					writerL.Write(";" + handLeft.GetComponent<OVRSkeleton>().Bones[i].Id.ToString() + ";PosX;PosY;PosZ;RotX;RotY;RotZ");
		    	}
				writerL.Close();

				writerR.WriteLine("Time;AccX;AccY;AccZ;GyX;GyY;GyZ");
				for(int i = 0; i < handRight.GetComponent<OVRSkeleton>().Bones.Count; i++)
		    	{
					writerR.Write(";" + handRight.GetComponent<OVRSkeleton>().Bones[i].Id.ToString() + ";PosX;PosY;PosZ;RotX;RotY;RotZ");
		    	}
				writerR.Close();
				state = 1;
				break;

			case 1:
				pathL = "Assets/Resources/DataCollection/Left-" + time1 + ".csv";
				pathR = "Assets/Resources/DataCollection/Right-" + time1 + ".csv";
		        writerL = new StreamWriter(pathL, true);
		        writerR = new StreamWriter(pathR, true);
				writerL.WriteLine();
				writerL.Write((Time.unscaledTime - time0));
				writerL.Write(imuReceiver._ax + ";" + imuReceiver._ay + ";" + imuReceiver._az + ";" + imuReceiver._gx + ";" + imuReceiver._gy + ";" + imuReceiver._gz);
		    	for(int i = 0; i < handLeft.GetComponent<OVRSkeleton>().Bones.Count; i++)
		    	{
					writerL.Write(";" + i.ToString() +";"+ handLeft.GetComponent<OVRSkeleton>().Bones[i].Transform.position.x.ToString("F4") +";"+ handLeft.GetComponent<OVRSkeleton>().Bones[i].Transform.position.y.ToString("F4") +";"+ handLeft.GetComponent<OVRSkeleton>().Bones[i].Transform.position.z.ToString("F4") +";"+ handLeft.GetComponent<OVRSkeleton>().Bones[i].Transform.eulerAngles.x.ToString("F4") +";"+ handLeft.GetComponent<OVRSkeleton>().Bones[i].Transform.eulerAngles.y.ToString("F4") +";"+ handLeft.GetComponent<OVRSkeleton>().Bones[i].Transform.eulerAngles.z.ToString("F4")+";"+ handRight.GetComponent<OVRSkeleton>().Bones[i].Transform.position.x.ToString("F4") +";"+ handRight.GetComponent<OVRSkeleton>().Bones[i].Transform.position.y.ToString("F4") +";"+ handRight.GetComponent<OVRSkeleton>().Bones[i].Transform.position.z.ToString("F4") +";"+ handRight.GetComponent<OVRSkeleton>().Bones[i].Transform.eulerAngles.x.ToString("F4") +";"+ handRight.GetComponent<OVRSkeleton>().Bones[i].Transform.eulerAngles.y.ToString("F4") +";"+ handRight.GetComponent<OVRSkeleton>().Bones[i].Transform.eulerAngles.z.ToString("F4"));
		    	}
		    	writerL.Close();

		    	if(calibratingPosition.finishCalibrating)
		    	{
		    		state = 2;
		    	}

		    	writerR.WriteLine();
				writerR.Write((Time.unscaledTime - time0));
				writerR.Write(imuReceiver._ax + ";" + imuReceiver._ay + ";" + imuReceiver._az + ";" + imuReceiver._gx + ";" + imuReceiver._gy + ";" + imuReceiver._gz);

		    	for(int i = 0; i < handRight.GetComponent<OVRSkeleton>().Bones.Count; i++)
		    	{
					writerR.Write(";" + i.ToString() +";"+ handRight.GetComponent<OVRSkeleton>().Bones[i].Transform.position.x.ToString("F4") +";"+ handLeft.GetComponent<OVRSkeleton>().Bones[i].Transform.position.y.ToString("F4") +";"+ handLeft.GetComponent<OVRSkeleton>().Bones[i].Transform.position.z.ToString("F4") +";"+ handLeft.GetComponent<OVRSkeleton>().Bones[i].Transform.eulerAngles.x.ToString("F4") +";"+ handLeft.GetComponent<OVRSkeleton>().Bones[i].Transform.eulerAngles.y.ToString("F4") +";"+ handLeft.GetComponent<OVRSkeleton>().Bones[i].Transform.eulerAngles.z.ToString("F4")+";"+ handRight.GetComponent<OVRSkeleton>().Bones[i].Transform.position.x.ToString("F4") +";"+ handRight.GetComponent<OVRSkeleton>().Bones[i].Transform.position.y.ToString("F4") +";"+ handRight.GetComponent<OVRSkeleton>().Bones[i].Transform.position.z.ToString("F4") +";"+ handRight.GetComponent<OVRSkeleton>().Bones[i].Transform.eulerAngles.x.ToString("F4") +";"+ handRight.GetComponent<OVRSkeleton>().Bones[i].Transform.eulerAngles.y.ToString("F4") +";"+ handRight.GetComponent<OVRSkeleton>().Bones[i].Transform.eulerAngles.z.ToString("F4"));
		    	}
		    	writerR.Close();
				break;

			case 2:
				if(calibratingPosition.startCalibrating || Input.GetKeyDown(toggleRecordingKey))
				{
					state = 0;
					calibratingPosition.startCalibrating = false;
				}

				break;
		}
    }

}