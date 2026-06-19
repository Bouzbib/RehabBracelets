using System.Collections;
using UnityEngine;
using UnityEditor; 

[CustomEditor(typeof(UDPTest))]
public class MotorTestEditor : Editor
{
   public override void OnInspectorGUI()
   {
   		DrawDefaultInspector();
   		UDPTest myMotorTest = (UDPTest) target;
   		if(GUILayout.Button("Test Motor"))
   		{
   			myMotorTest.LaunchVibration();
   		}

   }
}
