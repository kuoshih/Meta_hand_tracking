using UnityEngine;

public class PalmPoseDebug : MonoBehaviour
{
	public Transform leftPalm;
	public Transform rightPalm;

	private string leftText = "Left palm not assigned.";
	private string rightText = "Right palm not assigned.";

	void Update()
	{
		leftText = BuildHandText("Left Hand", leftPalm);
		rightText = BuildHandText("Right Hand", rightPalm);
	}

	string BuildHandText(string handName, Transform palm)
	{
		if (palm == null)
			return handName + "\nNot assigned.";

		Vector3 pos = palm.position;
		Vector3 rot = palm.eulerAngles;

		float roll = rot.z;
		float pitch = rot.x;
		float yaw = rot.y;

		return handName + "\n" +
			"X: " + pos.x.ToString("F3") + "\n" +
			"Y: " + pos.y.ToString("F3") + "\n" +
			"Z: " + pos.z.ToString("F3") + "\n" +
			"Roll: " + roll.ToString("F1") + "\n" +
			"Pitch: " + pitch.ToString("F1") + "\n" +
			"Yaw: " + yaw.ToString("F1");
	}

	void OnGUI()
	{
		GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
		labelStyle.fontSize = 20;
		labelStyle.normal.textColor = Color.white;
		labelStyle.alignment = TextAnchor.UpperLeft;

		GUI.Box(new Rect(10, 10, 300, 210), "");
		GUI.Label(new Rect(20, 20, 280, 190), leftText, labelStyle);

		GUI.Box(new Rect(320, 10, 300, 210), "");
		GUI.Label(new Rect(330, 20, 280, 190), rightText, labelStyle);
	}
}