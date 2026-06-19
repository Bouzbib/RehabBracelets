using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalibrateHandPosition : MonoBehaviour
{

    public GameObject realHandLeft, modelLeftHand;
    public GameObject realHandRight, modelRightHand;

    public GameObject wristR, wristL, palmR, palmL;
    public GameObject wristRF, wristLF, palmRF, palmLF;

    private Color[] originalColor;
    public bool firstTime;

    private Vector3 originalEulerR, originalEulerL;

    public bool startCalibrating, finishCalibrating;

    private int state = 0;

    public IMUReceiver imuReceiver;
    
    // Start is called before the first frame update
    void Start()
    {
        wristR = realHandRight.transform.Find("b_r_wrist").gameObject;
     	wristRF = modelRightHand.transform.Find("b_r_wrist").gameObject;

     	palmR = realHandRight.transform.Find("r_palm_center_marker").gameObject;
     	wristRF = modelRightHand.transform.Find("r_palm_center_marker").gameObject;

     	wristL = realHandLeft.transform.Find("b_l_wrist").gameObject;
     	wristLF = modelLeftHand.transform.Find("b_l_wrist").gameObject;

     	palmL = realHandLeft.transform.Find("l_palm_center_marker").gameObject;
     	palmLF = modelLeftHand.transform.Find("l_palm_center_marker").gameObject;

     	originalColor = new Color[2];
     	originalColor[0] = modelRightHand.gameObject.GetComponentInChildren<Renderer>().material.color;
     	originalColor[1] = modelLeftHand.gameObject.GetComponentInChildren<Renderer>().material.color;

     	originalEulerR = modelRightHand.transform.eulerAngles;
     	originalEulerL = modelLeftHand.transform.eulerAngles;
     	
           
    }

    // Update is called once per frame
    void Update()
    {

    	wristR = realHandRight.transform.Find("b_r_wrist").gameObject;
     	wristRF = modelRightHand.transform.Find("b_r_wrist").gameObject;

     	palmR = realHandRight.transform.Find("r_palm_center_marker").gameObject;
     	wristRF = modelRightHand.transform.Find("r_palm_center_marker").gameObject;

     	wristL = realHandLeft.transform.Find("b_l_wrist").gameObject;
     	wristLF = modelLeftHand.transform.Find("b_l_wrist").gameObject;

     	palmL = realHandLeft.transform.Find("l_palm_center_marker").gameObject;
     	palmLF = modelLeftHand.transform.Find("l_palm_center_marker").gameObject;
    	switch(state)
    	{
    		case 0:
    			if(ComparePositions(wristRF, wristR))
		        {
		        	if(ComparePositions(palmR, palmRF))
		        	{
		        		modelRightHand.GetComponentInChildren<Renderer>().material.color = Color.green;
	        			StartCoroutine(WaitAndRotate());
	        			state = 1;
		        		
		        	}
		        }
		        else
		        {
		    		modelRightHand.GetComponentInChildren<Renderer>().material.color = originalColor[0];
		        }

		        break;

	        case 1:
	        	if(ComparePositions(wristRF, wristR))
		        {
		        	if(ComparePositions(palmR, palmRF))
		        	{
		        		if(CompareAngles(palmR, palmRF))
		        		{
		        			modelRightHand.GetComponentInChildren<Renderer>().material.color = Color.green;
	        				StartCoroutine(FinishCalibrating());
	        				state = 2;
		        		}
		        		
		        		
		        	}
		        }
		        else
		        {
		    		modelRightHand.GetComponentInChildren<Renderer>().material.color = originalColor[0];
		        }

		        break;

	        case 2:
	        	break;

    	}
        
    }

    bool ComparePositions(GameObject obj1, GameObject obj2)
    {
    	return (Vector3.Distance(obj1.transform.position, obj2.transform.position) < 0.01f);
    }

    bool CompareAngles(GameObject obj1, GameObject obj2)
    {
    	return (Vector3.Angle(obj1.transform.forward, obj2.transform.forward) < 5f);
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
