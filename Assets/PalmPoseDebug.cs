using UnityEngine;

public class PalmPoseDebug : MonoBehaviour
{
	public Transform leftPalm;
	public Transform rightPalm;
	public Transform leftTop;
	public Transform rightTop;

	private string leftText = "Left hand not assigned.";
	private string rightText = "Right hand not assigned.";

	void Update()
	{
		leftText = BuildHandText("Left Hand", leftPalm, leftTop);
		rightText = BuildHandText("Right Hand", rightPalm, rightTop);
	}

	string BuildHandText(string handName, Transform palm, Transform top)
	{
		if (palm == null)
			return handName + "\nPalm not assigned.";

		if (top == null)
			return handName + "\nTop not assigned.";

		Vector3 palmPos = palm.position;
		Vector3 topPos = top.position;

		Vector3 dir = topPos - palmPos;
		if (dir.sqrMagnitude > 0.000001f)
		{
			dir.Normalize();
		}
		else
		{
			dir = Vector3.zero;
		}

		return handName + "\n" +
			"Palm X: " + palmPos.x.ToString("F3") + "\n" +
			"Palm Y: " + palmPos.y.ToString("F3") + "\n" +
			"Palm Z: " + palmPos.z.ToString("F3") + "\n" +
			"Top X:  " + topPos.x.ToString("F3") + "\n" +
			"Top Y:  " + topPos.y.ToString("F3") + "\n" +
			"Top Z:  " + topPos.z.ToString("F3") + "\n" +
			"Dir X:  " + dir.x.ToString("F3") + "\n" +
			"Dir Y:  " + dir.y.ToString("F3") + "\n" +
			"Dir Z:  " + dir.z.ToString("F3");
	}

	void OnGUI()
	{
		GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
		labelStyle.fontSize = 18;
		labelStyle.normal.textColor = Color.white;
		labelStyle.alignment = TextAnchor.UpperLeft;

		GUI.Box(new Rect(10, 10, 320, 250), "");
		GUI.Label(new Rect(20, 20, 300, 230), leftText, labelStyle);

		GUI.Box(new Rect(340, 10, 320, 250), "");
		GUI.Label(new Rect(350, 20, 300, 230), rightText, labelStyle);
	}
}