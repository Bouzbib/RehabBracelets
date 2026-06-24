using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalibrateHandPosition : MonoBehaviour
{

    //public HapticUDPController.ArmID firstToCalibrate;
    public GameObject objectTrigger;
    public GameObject modelLeftHand;
    public GameObject modelRightHand;



    private Vector3[] windowData0, windowData1;
    private int windowSize = 45;

    public GameObject handRightObject, handLeftObject;

    [HideInInspector]
    public GameObject wristR, wristL, palmR, palmL;
    private GameObject wristRF, wristLF, palmRF, palmLF;

    private Color[] originalColor;
    public bool firstTime = false;


    public bool startCalibrating, finishCalibrating;

    public int state = 0;

    public IMUReceiver[] imuReceivers;
    private bool record;
    private int frame;

    private bool reversed;
    private bool bottomOrLeft, rightOrTop, rightOrBottom, leftOrTop;

    public int imuPos = -1, espPos = -1, ulnPos = -1, batteryPos = -1;
    
    // Start is called before the first frame update
    void Start()
    {

     	originalColor = new Color[2];
        originalColor[0] = handRightObject.GetComponentInChildren<Renderer>().material.color;
        originalColor[1] = handLeftObject.GetComponentInChildren<Renderer>().material.color;

        modelRightHand.gameObject.GetComponentInChildren<Renderer>().material.color = originalColor[0];
        modelLeftHand.gameObject.GetComponentInChildren<Renderer>().material.color = originalColor[1];

        imuReceivers = this.GetComponents<IMUReceiver>();

        windowData0 = new Vector3[windowSize];
        windowData1 = new Vector3[windowSize];

        StartCoroutine(WaitToStartWire());
    }

    // Update is called once per frame
    void Update()
    {
        if(handRightObject.transform.childCount != 0)
        {
            wristR = handRightObject.transform.Find("Bones/XRHand_Wrist").gameObject;
            palmR = handRightObject.transform.Find("Bones/XRHand_Wrist/XRHand_Palm").gameObject;

        }

        if (handLeftObject.transform.childCount != 0)
        {
            wristL = handLeftObject.transform.Find("Bones/XRHand_Wrist").gameObject;
            palmL = handLeftObject.transform.Find("Bones/XRHand_Wrist/XRHand_Palm").gameObject;

        }

        if(Input.GetKeyDown(KeyCode.A))
        {
            if(state == 0)
            {
                StartCoroutine(WaitAndRotate(-178, modelRightHand));
            }
            if (state == 1)
            {
                StartCoroutine(FinishCalibrating(modelRightHand));
            }
            if (state == 2)
            {
                StartCoroutine(WaitAndRotate(178, modelLeftHand));
            }
            if (state == 3)
            {
                StartCoroutine(FinishCalibrating(modelLeftHand));
            }
        }

        wristRF = modelRightHand.transform.Find("b_r_wrist").gameObject;     	
     	palmRF = modelRightHand.transform.Find("b_r_wrist/r_palm_center_marker").gameObject;
     	wristLF = modelLeftHand.transform.Find("b_l_wrist").gameObject;
     	palmLF = modelLeftHand.transform.Find("b_l_wrist/l_palm_center_marker").gameObject;

        frame = (frame + 1) % windowSize;
        for(int i = 0; i < imuReceivers.Length; i++)
        {
            windowData0[frame] = new Vector3(imuReceivers[0]._ax , imuReceivers[0]._ay , imuReceivers[0]._az);
            windowData1[frame] = new Vector3(imuReceivers[1]._ax, imuReceivers[1]._ay, imuReceivers[1]._az);
        }
        //Debug.Log("Data 0: " + StandardDeviation(windowData0) + "; " + AngularDistance(imuReceivers[0]._gy));
        //Debug.Log("Data 1: " + StandardDeviation(windowData1) + "; " + AngularDistance(imuReceivers[1]._gy));


        switch (state)
    	{
    		case 0:
                if (handRightObject.transform.childCount != 0)
                { 
                    if (ComparePositions(wristRF, wristR))
		            {
                        
		        	    if(ComparePositions(palmR, palmRF))
		        	    {
                            if (CompareAngles(palmR, palmRF, 1))
                            {
                                modelRightHand.GetComponentInChildren<Renderer>().material.color = Color.blue;
                                objectTrigger.GetComponent<Renderer>().material.color = Color.green;
                                if (!firstTime)
                                {
                                    StartCoroutine(WaitAndRotate(-178, modelRightHand));
                                    StartCoroutine(VerifyXandZ());
                                    CheckOrientation();
                                    firstTime = true;
                                }
                            
                            
                            }
		        		
		        	    }
                    }
                }
		        else
		        {
		    		modelRightHand.GetComponentInChildren<Renderer>().material.color = originalColor[0];
                    objectTrigger.GetComponent<Renderer>().material.color = Color.red;
                }


                


                break;

	        case 1:
                
	        	if(ComparePositions(wristRF, wristR))
		        {
		        	if(ComparePositions(palmR, palmRF))
		        	{
		        		if(CompareAngles(palmR, palmRF, 1))
		        		{
		        			modelRightHand.GetComponentInChildren<Renderer>().material.color = Color.blue;
                            objectTrigger.GetComponent<Renderer>().material.color = Color.blue;
                            if (!firstTime)
                            {
                                StartCoroutine(FinishCalibrating(modelRightHand));
                                firstTime = true;

                            }
		        		}
		        		
		        		
		        	}
		        }
		        else
		        {
		    		modelRightHand.GetComponentInChildren<Renderer>().material.color = originalColor[0];
                    objectTrigger.GetComponent<Renderer>().material.color = Color.red;

                }
                
                if (bottomOrLeft)
                {
                    if (rightOrBottom)
                    { // Bottom
                        imuPos = 3;
                    }
                    if (leftOrTop)
                    {
                        // Left
                        imuPos = 2;
                    }
                }

                if (rightOrTop)
                {
                    if (rightOrBottom)
                    {
                        // Right
                        imuPos = 0;
                    }
                    if (leftOrTop)
                    {
                        // Top
                        imuPos = 1;
                    }
                }

                if(imuPos != -1)
                {
                    if (!reversed)
                    {
                        espPos = (imuPos + 1) % 4;
                        ulnPos = (imuPos + 2) % 4;
                        batteryPos = (imuPos + 3) % 4;
                    }
                    else
                    {
                        espPos = ((imuPos - 1) + 4) % 4;
                        ulnPos = ((imuPos - 2) + 4) % 4;
                        batteryPos = ((imuPos - 3) + 4) % 4;
                    }

                    for (int i = 0; i < imuReceivers.Length; i++)
                    {
                        if (imuReceivers[i].realArmID == HapticUDPController.ArmID.Right)
                        {
                            imuReceivers[i].motorOrder = new int[] { imuPos, espPos, ulnPos, batteryPos };
                        }
                    }
                }
                

                break;

	        case 2:

                rightOrBottom = false;
                leftOrTop = false;
                bottomOrLeft = false;
                rightOrTop = false;

                imuPos = -1;
                espPos = -1;
                ulnPos = -1;
                batteryPos = -1;

                if (ComparePositions(wristLF, wristL))
                {
                    if (ComparePositions(palmL, palmLF))
                    {
                        if (CompareAngles(palmL, palmLF, -1))
                        {
                            modelLeftHand.GetComponentInChildren<Renderer>().material.color = Color.blue;
                            objectTrigger.GetComponent<Renderer>().material.color = Color.green;
                            if (!firstTime)
                            {
                                StartCoroutine(WaitAndRotate(178, modelLeftHand));
                                StartCoroutine(VerifyXandZ());
                                firstTime = true;
                            }


                        }

                    }
                }
                else
                {
                    modelLeftHand.GetComponentInChildren<Renderer>().material.color = originalColor[1];
                    objectTrigger.GetComponent<Renderer>().material.color = Color.red;
                }

                break;

            case 3:
                if (ComparePositions(wristLF, wristL))
                {
                    if (ComparePositions(palmL, palmLF))
                    {
                        if (CompareAngles(palmL, palmLF, -1))
                        {
                            modelLeftHand.GetComponentInChildren<Renderer>().material.color = Color.blue;
                            objectTrigger.GetComponent<Renderer>().material.color = Color.blue;
                            if (!firstTime)
                            {
                                StartCoroutine(FinishCalibrating(modelLeftHand));
                                firstTime = true;

                            }
                        }


                    }
                }
                else
                {
                    modelLeftHand.GetComponentInChildren<Renderer>().material.color = originalColor[1];
                    objectTrigger.GetComponent<Renderer>().material.color = Color.red;

                }

                break;

        }

    }
    void DetermineOrientationR(IMUReceiver imuOfInterest)
    {
        if (Mathf.Sign(AngularDistance(imuOfInterest._gy)) < 0)
        {
            reversed = true;
            if (Mathf.Sign(AngularDistance(imuOfInterest._gy)) == Mathf.Sign(AngularDistance(imuOfInterest._gz)))
            {
                rightOrTop = false;
                bottomOrLeft = true;
                // IMU is top or right

            }
            else
            {
                bottomOrLeft = false;
                rightOrTop = true;
                // IMU is bottom of left

            }
        }
        else
        {
            reversed = false;
            if (Mathf.Sign(AngularDistance(imuOfInterest._gy)) == Mathf.Sign(AngularDistance(imuOfInterest._gz)))
            {
                // IMU is top or right
                rightOrTop = true;
                bottomOrLeft = false;

            }
            else
            {
                // IMU is bottom of left
                bottomOrLeft = true;
                rightOrTop = false;
            }
        }
    }

    void DetermineOrientationL(IMUReceiver imuOfInterest)
    {
        if (Mathf.Sign(AngularDistance(imuOfInterest._gy)) < 0)
        {
            reversed = true;
            if (Mathf.Sign(AngularDistance(imuOfInterest._gy)) == Mathf.Sign(AngularDistance(imuOfInterest._gz)))
            {
                rightOrTop = false;
                bottomOrLeft = true;
                // IMU is top or right

            }
            else
            {
                bottomOrLeft = false;
                rightOrTop = true;
                // IMU is bottom of left

            }
        }
        else
        {
            reversed = false;
            if (Mathf.Sign(AngularDistance(imuOfInterest._gy)) == Mathf.Sign(AngularDistance(imuOfInterest._gz)))
            {
                // IMU is top or right
                rightOrTop = true;
                bottomOrLeft = false;

            }
            else
            {
                // IMU is bottom of left
                bottomOrLeft = true;
                rightOrTop = false;
            }
        }
    }
    void CheckOrientation()
    {
        IMUReceiver imuOfInterest;
        if (state < 2)
        {
            
            if ((Mathf.Abs(AngularDistance(imuReceivers[0]._gy)) > 40f) && (StandardDeviation(windowData0).z > 5f))
            {
                imuReceivers[0].realArmID = HapticUDPController.ArmID.Right;
                imuReceivers[1].realArmID = HapticUDPController.ArmID.Left;
                imuOfInterest = imuReceivers[0];
                DetermineOrientationR(imuOfInterest);
            }


            if ((Mathf.Abs(AngularDistance(imuReceivers[1]._gy)) > 40f) && (StandardDeviation(windowData1).z > 5f))
            {
                imuReceivers[1].realArmID = HapticUDPController.ArmID.Right;
                imuReceivers[0].realArmID = HapticUDPController.ArmID.Left;
                imuOfInterest = imuReceivers[1];
                DetermineOrientationR(imuOfInterest);

            }
        }
        else
        {
            for (int i = 0; i < imuReceivers.Length; i++)
            {
                if (imuReceivers[i].realArmID == HapticUDPController.ArmID.Left)
                {
                    imuOfInterest = imuReceivers[i];
                }
            }
            DetermineOrientationL(imuOfInterest);
        }
        
    }

    bool ComparePositions(GameObject obj1, GameObject obj2)
    {
        //Debug.Log("Distance " + obj1.name + ": " + Vector3.Distance(obj1.transform.position, obj2.transform.position));
    	return (Vector3.Distance(obj1.transform.position, obj2.transform.position) < 0.05f);
    }

    bool CompareAngles(GameObject obj1, GameObject obj2, int dir)
    {
        //Debug.Log("Angle: " + Vector3.Angle(obj1.transform.up, obj2.transform.up));
        Vector3 direction = Vector3.one * dir;
        return (Vector3.Angle(obj1.transform.up, Vector3.Scale(obj2.transform.up,direction)) < 5f);
    }

    IEnumerator WaitAndRotate(int angle, GameObject objToRotate)
    {    	
    	yield return new WaitForSeconds(1.0f);
    	
    	startCalibrating = true;

        Quaternion targetRotation = Quaternion.Euler(angle, objToRotate.transform.eulerAngles.y, objToRotate.transform.eulerAngles.z);
        while(Quaternion.Angle(objToRotate.transform.rotation, targetRotation) > 0.5f)
        {
            objToRotate.transform.rotation = Quaternion.RotateTowards(objToRotate.transform.rotation, targetRotation, 45 * Time.deltaTime);
            yield return null;
            CheckOrientation();

        }

        if (state == 0)
        {
            state = 1;

        }
        if (state == 2)
        {
            state = 3;
        }
        firstTime = false;


    }

    IEnumerator VerifyXandZ()
    {
        float time0 = Time.time;
        float countdown = 5;
        while (Time.time - time0 < countdown)
        {
            for (int i = 0; i < imuReceivers.Length; i++)
            {
               
                if (Mathf.Sign(windowData1[frame].x - windowData1[((frame - 10)+windowSize) % windowSize].x) != Mathf.Sign(windowData1[frame].z - windowData1[((frame - 10) + windowSize) % windowSize].z))
                {
                    Debug.Log("DIFFERENT SLOPES");
                    rightOrBottom = true;
                    leftOrTop = false;
                }
                else
                {
                    Debug.Log("SAME SLOPES");
                    leftOrTop = true;
                    rightOrBottom = false;
                }
            }
            yield return null;
        }
    }


    IEnumerator FinishCalibrating(GameObject objectToDisappear)
    {
    	yield return new WaitForSeconds(0.5f);
        firstTime = false;
        if (state == 1)
        {
            state = 2;
        }
        if (state == 3)
        {
            finishCalibrating = true;
            this.GetComponent<InstantiateBalls>().enabled = true;
        }
        objectToDisappear.gameObject.SetActive(false);

    }

    IEnumerator WaitToStartWire()
    {
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < imuReceivers.Length; i++)
        {
            imuReceivers[i].SendInitWire();
            imuReceivers[i].SendCalibrate();
        }
        record = true;
    }

    public static Vector3 StandardDeviation(Vector3[] values)
    {
        int n = values.Length;
        if (n == 0)
            return new Vector3(0,0,0);
        float sumX = 0f;
        float sumY = 0f;
        float sumZ = 0f;
        for (int i = 0; i < n; i++)
        {
            sumX += values[i].x;
            sumY += values[i].y;
            sumZ += values[i].z;
        }
           
        float meanX = sumX / n;
        float meanY = sumY / n;
        float meanZ = sumZ / n;
        float varianceX = 0f;
        float varianceY = 0f;
        float varianceZ = 0f;

        for (int i = 0; i < n; i++)
        {
            float diff = values[i].x - meanX;
            varianceX += diff * diff;

            diff = values[i].y - meanY;
            varianceY += diff * diff;

            diff = values[i].z - meanZ;
            varianceZ += diff * diff;
        }
        varianceX /= n; // use (n - 1) here for sample standard deviation
        varianceY /= n;
        varianceZ /= n;
        return new Vector3(Mathf.Sqrt(varianceX)*100, Mathf.Sqrt(varianceY)*100, Mathf.Sqrt(varianceZ)*100);
    }

    public float AngularDistance(float eulerAngle)
    {
        return ((eulerAngle + 180) % 360) - 180;
    }

}
