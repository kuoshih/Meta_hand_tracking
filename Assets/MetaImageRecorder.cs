using UnityEngine;
using System.IO;
using System.Text;
using System;

public class MetaImageRecorder : MonoBehaviour
{
	[Header("Hand Tracking")]
	public Transform leftPalm;
	public Transform rightPalm;

	[Header("Raw Meta Webcam")]
	public string deviceName = "Meta2 Webcam";
	public int requestedWidth = 640;
	public int requestedHeight = 480;
	public int requestedFPS = 30;

	[Header("AR Capture Camera")]
	public Camera captureCamera;
	public int arImageWidth = 640;
	public int arImageHeight = 480;

	[Header("Logging")]
	public float logInterval = 0.1f;   // 10 Hz
	public int fontSize = 20;
	[Range(1, 100)]
	public int jpgQuality = 90;

	private WebCamTexture webcamTexture;

	private bool isRecording = false;
	private string statusText = "已結束存資料";

	private string sessionFolder = "";
	private string imageFolder = "";
	private string arImageFolder = "";
	private string logFolder = "";
	private string csvPath = "";

	private float nextLogTime = 0f;
	private int recordFrame = 0;

	void Start()
	{
		InitializeWebcam();
	}

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

		if (isRecording && Time.time >= nextLogTime)
		{
			nextLogTime = Time.time + logInterval;
			CaptureAndWrite();
		}
	}

	void InitializeWebcam()
	{
		WebCamDevice[] devices = WebCamTexture.devices;
		bool found = false;

		foreach (var d in devices)
		{
			Debug.Log("Found webcam: " + d.name);
			if (d.name == deviceName)
			{
				found = true;
			}
		}

		if (!found)
		{
			Debug.LogError("找不到指定 webcam: " + deviceName);
			statusText = "找不到相機";
			return;
		}

		webcamTexture = new WebCamTexture(deviceName, requestedWidth, requestedHeight, requestedFPS);
		webcamTexture.Play();

		Debug.Log("Webcam started: " + deviceName);
	}

	public void StartRecording()
	{
		if (isRecording)
		{
			Debug.Log("已經在記錄中。");
			return;
		}

		if (webcamTexture == null)
		{
			Debug.LogError("webcamTexture 尚未初始化。");
			statusText = "相機未初始化";
			return;
		}

		if (captureCamera == null)
		{
			Debug.LogError("captureCamera 尚未指定。");
			statusText = "CaptureCamera 未指定";
			return;
		}

		try
		{
			string rootFolder = Path.Combine(Application.dataPath, "HandPoseLogs");
			string sessionName = "session_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_ffff");

			sessionFolder = Path.Combine(rootFolder, sessionName);
			imageFolder = Path.Combine(sessionFolder, "images");
			arImageFolder = Path.Combine(sessionFolder, "ARimages");
			logFolder = Path.Combine(sessionFolder, "logs");

			Directory.CreateDirectory(rootFolder);
			Directory.CreateDirectory(sessionFolder);
			Directory.CreateDirectory(imageFolder);
			Directory.CreateDirectory(arImageFolder);
			Directory.CreateDirectory(logFolder);

			csvPath = Path.Combine(logFolder, "handpose_log.csv");

			StringBuilder sb = new StringBuilder();
			sb.AppendLine(
				"record_frame," +
				"unity_time," +
				"unix_time," +
				"system_time_local," +
				"system_time_utc," +
				"raw_image_filename," +
				"ar_image_filename," +
				"left_x,left_y,left_z,left_roll,left_pitch,left_yaw," +
				"right_x,right_y,right_z,right_roll,right_pitch,right_yaw"
			);

			File.WriteAllText(csvPath, sb.ToString(), Encoding.UTF8);

			recordFrame = 0;
			isRecording = true;
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
			Debug.Log("目前沒有在記錄。");
			statusText = "已結束存資料";
			return;
		}

		isRecording = false;
		statusText = "已結束存資料";

		Debug.Log("停止記錄");
		Debug.Log("資料夾位置: " + sessionFolder);
	}

	void CaptureAndWrite()
	{
		if (leftPalm == null || rightPalm == null)
		{
			statusText = "Palm 未指定";
			Debug.LogWarning("leftPalm 或 rightPalm 尚未指定。");
			return;
		}

		if (webcamTexture == null || !webcamTexture.isPlaying)
		{
			statusText = "相機未啟動";
			Debug.LogWarning("webcamTexture 尚未啟動。");
			return;
		}

		if (webcamTexture.width <= 16 || webcamTexture.height <= 16)
		{
			Debug.LogWarning("webcamTexture 尚未準備好，略過此幀。");
			return;
		}

		try
		{
			string rawImageFilename = "frame_" + recordFrame.ToString("D6") + ".jpg";
			string rawImagePath = Path.Combine(imageFolder, rawImageFilename);

			string arImageFilename = "ARframe_" + recordFrame.ToString("D6") + ".jpg";
			string arImagePath = Path.Combine(arImageFolder, arImageFilename);

			// 1. 純相機影像
			SaveWebcamFrame(rawImagePath);

			// 2. 相機 + AR 藍球
			SaveCameraRenderFrame(arImagePath);

			// 3. 雙手資訊
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
				rawImageFilename + "," +
				arImageFilename + "," +
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
			statusText = "寫入失敗";
			Debug.LogError("CaptureAndWrite 失敗: " + e);
		}
	}

	void SaveWebcamFrame(string imagePath)
	{
		Texture2D tex = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGB24, false);
		tex.SetPixels(webcamTexture.GetPixels());
		tex.Apply();

		byte[] bytes = tex.EncodeToJPG(jpgQuality);
		File.WriteAllBytes(imagePath, bytes);

		Destroy(tex);
	}

	void SaveCameraRenderFrame(string imagePath)
	{
		RenderTexture rt = new RenderTexture(arImageWidth, arImageHeight, 24);
		Texture2D tex = new Texture2D(arImageWidth, arImageHeight, TextureFormat.RGB24, false);

		RenderTexture oldActive = RenderTexture.active;
		RenderTexture oldTarget = captureCamera.targetTexture;

		captureCamera.targetTexture = rt;
		captureCamera.Render();

		RenderTexture.active = rt;
		tex.ReadPixels(new Rect(0, 0, arImageWidth, arImageHeight), 0, 0);
		tex.Apply();

		captureCamera.targetTexture = oldTarget;
		RenderTexture.active = oldActive;

		byte[] bytes = tex.EncodeToJPG(jpgQuality);
		File.WriteAllBytes(imagePath, bytes);

		Destroy(rt);
		Destroy(tex);
	}

	void OnGUI()
	{
		GUIStyle style = new GUIStyle(GUI.skin.label);
		style.fontSize = fontSize;
		style.normal.textColor = Color.white;
		style.alignment = TextAnchor.MiddleLeft;

		GUI.Label(new Rect(20, 10, 500, 30), "MetaImageRecorder v2026-03-31 AR", style);

		GUI.Box(new Rect(20, 40, 360, 60), "");
		GUI.Label(new Rect(30, 50, 340, 40), statusText, style);

		string info =
			"Left Palm: " + (leftPalm != null ? "OK" : "None") + "\n" +
			"Right Palm: " + (rightPalm != null ? "OK" : "None") + "\n" +
			"Webcam: " + ((webcamTexture != null && webcamTexture.isPlaying) ? "OK" : "None") + "\n" +
			"CaptureCamera: " + (captureCamera != null ? "OK" : "None");

		GUI.Box(new Rect(20, 110, 360, 110), "");
		GUI.Label(new Rect(30, 120, 340, 100), info, style);

		if (GUI.Button(new Rect(20, 230, 140, 50), "Start"))
		{
			StartRecording();
		}

		if (GUI.Button(new Rect(180, 230, 140, 50), "Stop"))
		{
			StopRecording();
		}

		GUI.Box(new Rect(20, 290, 520, 120), "");
		GUI.Label(
			new Rect(30, 300, 500, 110),
			"R：開始\nT：停止\n" +
			"images = 純相機影像\n" +
			"ARimages = 相機 + AR 藍球\n" +
			"logs = 雙手資訊 CSV",
			style
		);
	}

	void OnDestroy()
	{
		if (webcamTexture != null && webcamTexture.isPlaying)
		{
			webcamTexture.Stop();
		}
	}
}