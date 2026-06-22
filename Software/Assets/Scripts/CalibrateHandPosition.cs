using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalibrateHandPosition : MonoBehaviour
{

    public GameObject modelLeftHand;
    public GameObject modelRightHand;

    public GameObject handRightObject, handLeftObject;

    private GameObject wristR, wristL, palmR, palmL;
    private GameObject wristRF, wristLF, palmRF, palmLF;

    private Material[] originalColor;
    public Material positionMaterial;
    public bool firstTime;

    private Vector3 originalEulerR, originalEulerL;

    public bool startCalibrating, finishCalibrating;

    private int state = 0;

    public IMUReceiver imuReceiver;
    
    // Start is called before the first frame update
    void Start()
    {

     	originalColor = new Material[2];
     	originalColor[0] = modelRightHand.gameObject.GetComponentInChildren<Renderer>().material;
     	originalColor[1] = modelLeftHand.gameObject.GetComponentInChildren<Renderer>().material;

     	originalEulerR = modelRightHand.transform.eulerAngles;
     	originalEulerL = modelLeftHand.transform.eulerAngles;
     	
           
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
		        		modelRightHand.GetComponentInChildren<Renderer>().material = positionMaterial;
	        			//StartCoroutine(WaitAndRotate());
	        			//state = 1;
		        		
		        	}
		        }
		        else
		        {
		    		modelRightHand.GetComponentInChildren<Renderer>().material = originalColor[0];
		        }

		        break;

	        case 1:
	        	if(ComparePositions(wristRF, wristR))
		        {
		        	if(ComparePositions(palmR, palmRF))
		        	{
		        		if(CompareAngles(palmR, palmRF))
		        		{
		        			modelRightHand.GetComponentInChildren<Renderer>().material = positionMaterial;
	        				StartCoroutine(FinishCalibrating());
	        				state = 2;
		        		}
		        		
		        		
		        	}
		        }
		        else
		        {
		    		modelRightHand.GetComponentInChildren<Renderer>().material = originalColor[0];
		        }

		        break;

	        case 2:
	        	break;

    	}
        
    }

    bool ComparePositions(GameObject obj1, GameObject obj2)
    {
        Debug.Log("Distance " + obj1.name + ": " + Vector3.Distance(obj1.transform.position, obj2.transform.position));
    	return (Vector3.Distance(obj1.transform.position, obj2.transform.position) < 0.05f);
    }

    bool CompareAngles(GameObject obj1, GameObject obj2)
    {
        Debug.Log("Angle: " + Vector3.Angle(obj1.transform.up, obj2.transform.up));

        return (Vector3.Angle(obj1.transform.up, obj2.transform.up) < 5f);
    }

    IEnumerator WaitAndRotate()
    {
    	imuReceiver.SendInitWire();
    	yield return new WaitForSeconds(1.0f);
    	imuReceiver.SendCalibrate();
    	startCalibrating = true;
    	yield return new WaitForSeconds(5.0f);
    	modelRightHand.transform.eulerAngles = new Vector3(originalEulerR.x, originalEulerR.y + 180, originalEulerR.z);

    }

    IEnumerator FinishCalibrating()
    {
    	yield return new WaitForSeconds(5.0f);
    	finishCalibrating = true;
    	modelRightHand.gameObject.SetActive(false);
    }

}
