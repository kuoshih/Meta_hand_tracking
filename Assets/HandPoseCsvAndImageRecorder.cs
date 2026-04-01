using UnityEngine;
using System.IO;
using System.Text;
using System;
using System.Collections;

public class HandPoseCsvAndImageRecorder : MonoBehaviour
{
	[Header("Hand Tracking")]
	public Transform leftPalm;
	public Transform rightPalm;

	[Header("Image Capture")]
	public Camera captureCamera;
	public int imageWidth = 640;
	public int imageHeight = 480;
	[Range(1, 100)]
	public int jpgQuality = 90;

	[Header("Logging")]
	public float logInterval = 0.1f;   // 10 Hz
	public int fontSize = 20;

	private bool isRecording = false;
	private bool isBusy = false;
	private string statusText = "已結束存資料";

	private string sessionFolder = "";
	private string imageFolder = "";
	private string csvPath = "";

	private float nextLogTime = 0f;
	private int recordFrame = 0;

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.R))
		{
			StartRecording();
		}

		if (Input.GetKeyDown(KeyCode.T))
		{
			StopRecording();
		}

		if (isRecording && !isBusy && Time.time >= nextLogTime)
		{
			nextLogTime = Time.time + logInterval;
			StartCoroutine(CaptureAndWriteCoroutine());
		}
	}

	public void StartRecording()
	{
		if (isRecording)
		{
			Debug.Log("已經在記錄中。");
			return;
		}

		try
		{
			string rootFolder = Path.Combine(Application.dataPath, "HandPoseLogs");
			string sessionName = "session_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_ffff");

			sessionFolder = Path.Combine(rootFolder, sessionName);
			imageFolder = Path.Combine(sessionFolder, "images");
			string logFolder = Path.Combine(sessionFolder, "logs");

			Directory.CreateDirectory(rootFolder);
			Directory.CreateDirectory(sessionFolder);
			Directory.CreateDirectory(imageFolder);
			Directory.CreateDirectory(logFolder);

			csvPath = Path.Combine(logFolder, "handpose_log.csv");

			StringBuilder sb = new StringBuilder();
			sb.AppendLine(
				"record_frame," +
				"unity_time," +
				"unix_time," +
				"system_time_local," +
				"system_time_utc," +
				"image_filename," +
				"left_x,left_y,left_z,left_roll,left_pitch,left_yaw," +
				"right_x,right_y,right_z,right_roll,right_pitch,right_yaw"
			);

			File.WriteAllText(csvPath, sb.ToString(), Encoding.UTF8);

			recordFrame = 0;
			isRecording = true;
			isBusy = false;
			statusText = "正在存資料";
			nextLogTime = Time.time;

			Debug.Log("開始記錄: " + sessionFolder);
		}
		catch (Exception e)
		{
			statusText = "開始存檔失敗";
			Debug.LogError("StartRecording 失敗: " + e);
		}
	}

	public void StopRecording()
	{
		if (!isRecording)
		{
			statusText = "已結束存資料";
			Debug.Log("目前沒有在記錄。");
			return;
		}

		isRecording = false;
		statusText = "已結束存資料";

		Debug.Log("停止記錄");
		Debug.Log("資料夾位置: " + sessionFolder);
	}

	IEnumerator CaptureAndWriteCoroutine()
	{
		isBusy = true;

		yield return new WaitForEndOfFrame();

		if (leftPalm == null || rightPalm == null)
		{
			statusText = "Palm 未指定";
			Debug.LogWarning("leftPalm 或 rightPalm 尚未指定。");
			isBusy = false;
			yield break;
		}

		if (captureCamera == null)
		{
			statusText = "Camera 未指定";
			Debug.LogWarning("captureCamera 尚未指定。");
			isBusy = false;
			yield break;
		}

		string imageFilename = "frame_" + recordFrame.ToString("D6") + ".jpg";
		string imagePath = Path.Combine(imageFolder, imageFilename);

		try
		{
			SaveCameraJpg(imagePath);

			Vector3 lp = leftPalm.position;
			Vector3 lr = leftPalm.eulerAngles;

			Vector3 rp = rightPalm.position;
			Vector3 rr = rightPalm.eulerAngles;

			double unixTime = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
			string systemTimeLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff");
			string systemTimeUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffff");

			string line =
				recordFrame + "," +
				Time.time.ToString("F6") + "," +
				unixTime.ToString("F6") + "," +
				systemTimeLocal + "," +
				systemTimeUtc + "," +
				imageFilename + "," +
				lp.x.ToString("F4") + "," +
				lp.y.ToString("F4") + "," +
				lp.z.ToString("F4") + "," +
				lr.z.ToString("F2") + "," +
				lr.x.ToString("F2") + "," +
				lr.y.ToString("F2") + "," +
				rp.x.ToString("F4") + "," +
				rp.y.ToString("F4") + "," +
				rp.z.ToString("F4") + "," +
				rr.z.ToString("F2") + "," +
				rr.x.ToString("F2") + "," +
				rr.y.ToString("F2");

			File.AppendAllText(csvPath, line + "\n", Encoding.UTF8);

			recordFrame++;
		}
		catch (Exception e)
		{
			Debug.LogError("CaptureAndWrite 失敗: " + e);
			statusText = "寫入失敗";
		}

		isBusy = false;
	}

	void SaveCameraJpg(string imagePath)
	{
		RenderTexture rt = new RenderTexture(imageWidth, imageHeight, 24);
		Texture2D tex = new Texture2D(imageWidth, imageHeight, TextureFormat.RGB24, false);

		RenderTexture oldActive = RenderTexture.active;
		RenderTexture oldTarget = captureCamera.targetTexture;

		captureCamera.targetTexture = rt;
		captureCamera.Render();

		RenderTexture.active = rt;
		tex.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
		tex.Apply();

		captureCamera.targetTexture = oldTarget;
		RenderTexture.active = oldActive;

		byte[] jpgBytes = tex.EncodeToJPG(jpgQuality);
		File.WriteAllBytes(imagePath, jpgBytes);

		Destroy(rt);
		Destroy(tex);
	}

	void OnGUI()
	{
		GUIStyle style = new GUIStyle(GUI.skin.label);
		style.fontSize = fontSize;
		style.normal.textColor = Color.white;
		style.alignment = TextAnchor.MiddleLeft;

		GUI.Label(new Rect(20, 10, 500, 30), "Recorder v2026-03-19 IMG", style);

		GUI.Box(new Rect(20, 40, 320, 60), "");
		GUI.Label(new Rect(30, 50, 300, 40), statusText, style);

		string info =
			"Left Palm: " + (leftPalm != null ? "OK" : "None") + "\n" +
			"Right Palm: " + (rightPalm != null ? "OK" : "None") + "\n" +
			"Capture Camera: " + (captureCamera != null ? "OK" : "None");

		GUI.Box(new Rect(20, 110, 320, 90), "");
		GUI.Label(new Rect(30, 120, 300, 80), info, style);

		if (GUI.Button(new Rect(20, 210, 140, 50), "Start"))
		{
			StartRecording();
		}

		if (GUI.Button(new Rect(180, 210, 140, 50), "Stop"))
		{
			StopRecording();
		}

		GUI.Box(new Rect(20, 270, 420, 90), "");
		GUI.Label(new Rect(30, 280, 400, 70),
			"R：開始\nT：停止\n存檔：Assets/HandPoseLogs/session_xxx/",
			style);
	}
}