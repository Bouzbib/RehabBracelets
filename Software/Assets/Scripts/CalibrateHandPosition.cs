using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalibrateHandPosition : MonoBehaviour
{
    public GameObject objectTrigger;
    public GameObject modelLeftHand;
    public GameObject modelRightHand;

    public GameObject handRightObject, handLeftObject;

    private GameObject wristR, wristL, palmR, palmL;
    private GameObject wristRF, wristLF, palmRF, palmLF;

    private Color[] originalColor;
    public bool firstTime = false;


    public bool startCalibrating, finishCalibrating;

    public int state = 0;

    public IMUReceiver[] imuReceivers;
    
    // Start is called before the first frame update
    void Start()
    {

     	originalColor = new Color[2];
        originalColor[0] = handRightObject.GetComponentInChildren<Renderer>().material.color;
        originalColor[1] = handLeftObject.GetComponentInChildren<Renderer>().material.color;

        modelRightHand.gameObject.GetComponentInChildren<Renderer>().material.color = originalColor[0];
        modelLeftHand.gameObject.GetComponentInChildren<Renderer>().material.color = originalColor[1];

        imuReceivers = this.GetComponents<IMUReceiver>();


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
    	switch(state)
    	{
    		case 0:
    			if(ComparePositions(wristRF, wristR))
		        {
		        	if(ComparePositions(palmR, palmRF))
		        	{
                        if (CompareAngles(palmR, palmRF, 1))
                        {
                            modelRightHand.GetComponentInChildren<Renderer>().material.color = Color.blue;
                            objectTrigger.GetComponent<Renderer>().material.color = Color.green;
                            if (!firstTime)
                            {
                                for(int i = 0; i < imuReceivers.Length; i++)
                                {
                                    imuReceivers[i].SendInitWire();
                                    imuReceivers[i].SendCalibrate();
                                }
                                StartCoroutine(WaitAndRotate(-178, modelRightHand));
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

                break;

	        case 2:
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
        }

        if(state == 0)
        {
            state = 1;
        }
        if (state == 2)
        {
            state = 3;
        }
        firstTime = false;

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

}
