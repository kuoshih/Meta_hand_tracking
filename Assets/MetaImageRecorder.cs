using UnityEngine;
using System.IO;
using System.Text;
using System;

public class MetaImageRecorder : MonoBehaviour
{
	[Header("Head Tracking")]
	public Transform headTransform;

	[Header("Hand Tracking")]
	public Transform leftPalm;
	public Transform rightPalm;
	public Transform leftTop;
	public Transform rightTop;

	[Header("Raw Meta Webcam")]
	public string deviceName = "Meta2 Webcam";
	public int requestedWidth = 640;
	public int requestedHeight = 480;
	public int requestedFPS = 30;

	[Header("AR Overlay Camera")]
	public Camera arOverlayCamera;
	public int outputWidth = 640;
	public int outputHeight = 480;

	[Header("Logging")]
	public float logInterval = 0.1f;
	public int fontSize = 18;
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

	private Vector3 leftDir = Vector3.zero;
	private Vector3 rightDir = Vector3.zero;

	void Start()
	{
		InitializeWebcam();
	}

	void Update()
	{
		UpdateDirections();

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

	void UpdateDirections()
	{
		if (leftPalm != null && leftTop != null)
		{
			leftDir = leftTop.position - leftPalm.position;
			if (leftDir.sqrMagnitude > 0.000001f)
			{
				leftDir.Normalize();
			}
			else
			{
				leftDir = Vector3.zero;
			}
		}
		else
		{
			leftDir = Vector3.zero;
		}

		if (rightPalm != null && rightTop != null)
		{
			rightDir = rightTop.position - rightPalm.position;
			if (rightDir.sqrMagnitude > 0.000001f)
			{
				rightDir.Normalize();
			}
			else
			{
				rightDir = Vector3.zero;
			}
		}
		else
		{
			rightDir = Vector3.zero;
		}
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
			statusText = "相機未初始化";
			Debug.LogError("webcamTexture 尚未初始化。");
			return;
		}

		if (arOverlayCamera == null)
		{
			statusText = "AR camera 未指定";
			Debug.LogError("arOverlayCamera 尚未指定。");
			return;
		}

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
			"head_x,head_y,head_z,head_roll,head_pitch,head_yaw," +
			"left_palm_x,left_palm_y,left_palm_z," +
			"left_top_x,left_top_y,left_top_z," +
			"left_dir_x,left_dir_y,left_dir_z," +
			"right_palm_x,right_palm_y,right_palm_z," +
			"right_top_x,right_top_y,right_top_z," +
			"right_dir_x,right_dir_y,right_dir_z"
		);

		File.WriteAllText(csvPath, sb.ToString(), Encoding.UTF8);

		recordFrame = 0;
		isRecording = true;
		statusText = "正在存資料";
		nextLogTime = Time.time;

		Debug.Log("開始記錄: " + sessionFolder);
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
		if (leftPalm == null || rightPalm == null || leftTop == null || rightTop == null)
		{
			statusText = "Palm/Top 未指定";
			Debug.LogWarning("leftPalm/rightPalm/leftTop/rightTop 尚未指定。");
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

		string rawImageFilename = "frame_" + recordFrame.ToString("D6") + ".jpg";
		string rawImagePath = Path.Combine(imageFolder, rawImageFilename);

		string arImageFilename = "ARframe_" + recordFrame.ToString("D6") + ".jpg";
		string arImagePath = Path.Combine(arImageFolder, arImageFilename);

		SaveRawWebcamFrame(rawImagePath);
		SaveCompositedARFrame(arImagePath);

		Vector3 headPos = Vector3.zero;
		Vector3 headRot = Vector3.zero;

		if (headTransform != null)
		{
			headPos = headTransform.position;
			headRot = headTransform.eulerAngles;
		}

		Vector3 lp = leftPalm.position;
		Vector3 rp = rightPalm.position;
		Vector3 lt = leftTop.position;
		Vector3 rt = rightTop.position;

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

			headPos.x.ToString("F4") + "," +
			headPos.y.ToString("F4") + "," +
			headPos.z.ToString("F4") + "," +
			headRot.z.ToString("F2") + "," +
			headRot.x.ToString("F2") + "," +
			headRot.y.ToString("F2") + "," +

			lp.x.ToString("F4") + "," +
			lp.y.ToString("F4") + "," +
			lp.z.ToString("F4") + "," +

			lt.x.ToString("F4") + "," +
			lt.y.ToString("F4") + "," +
			lt.z.ToString("F4") + "," +

			leftDir.x.ToString("F4") + "," +
			leftDir.y.ToString("F4") + "," +
			leftDir.z.ToString("F4") + "," +

			rp.x.ToString("F4") + "," +
			rp.y.ToString("F4") + "," +
			rp.z.ToString("F4") + "," +

			rt.x.ToString("F4") + "," +
			rt.y.ToString("F4") + "," +
			rt.z.ToString("F4") + "," +

			rightDir.x.ToString("F4") + "," +
			rightDir.y.ToString("F4") + "," +
			rightDir.z.ToString("F4");

		File.AppendAllText(csvPath, line + "\n", Encoding.UTF8);

		recordFrame++;
	}

	void SaveRawWebcamFrame(string imagePath)
	{
		Texture2D tex = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGB24, false);
		tex.SetPixels(webcamTexture.GetPixels());
		tex.Apply();

		byte[] bytes = tex.EncodeToJPG(jpgQuality);
		File.WriteAllBytes(imagePath, bytes);

		Destroy(tex);
	}

	void SaveCompositedARFrame(string imagePath)
	{
		int w = outputWidth;
		int h = outputHeight;

		Texture2D webcamTex = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGB24, false);
		webcamTex.SetPixels(webcamTexture.GetPixels());
		webcamTex.Apply();

		Color[] basePixels = webcamTex.GetPixels();

		RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
		Texture2D overlayTex = new Texture2D(w, h, TextureFormat.ARGB32, false);

		RenderTexture oldActive = RenderTexture.active;
		RenderTexture oldTarget = arOverlayCamera.targetTexture;

		arOverlayCamera.targetTexture = rt;
		arOverlayCamera.Render();

		RenderTexture.active = rt;
		overlayTex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
		overlayTex.Apply();

		arOverlayCamera.targetTexture = oldTarget;
		RenderTexture.active = oldActive;

		Color[] overlayPixels = overlayTex.GetPixels();

		int pixelCount = Mathf.Min(basePixels.Length, overlayPixels.Length);
		Color[] outPixels = new Color[pixelCount];

		for (int i = 0; i < pixelCount; i++)
		{
			Color bg = basePixels[i];
			Color fg = overlayPixels[i];
			float a = fg.a;

			outPixels[i] = new Color(
				bg.r * (1f - a) + fg.r * a,
				bg.g * (1f - a) + fg.g * a,
				bg.b * (1f - a) + fg.b * a,
				1f
			);
		}

		Texture2D outTex = new Texture2D(w, h, TextureFormat.RGB24, false);
		outTex.SetPixels(outPixels);
		outTex.Apply();

		byte[] bytes = outTex.EncodeToJPG(jpgQuality);
		File.WriteAllBytes(imagePath, bytes);

		Destroy(webcamTex);
		Destroy(overlayTex);
		Destroy(outTex);
		Destroy(rt);
	}

	void OnGUI()
	{
		GUIStyle style = new GUIStyle(GUI.skin.label);
		style.fontSize = fontSize;
		style.normal.textColor = Color.white;
		style.alignment = TextAnchor.MiddleLeft;

		GUI.Label(new Rect(20, 10, 620, 30), "MetaImageRecorder stable v2026-04-06 DIR only", style);

		GUI.Box(new Rect(20, 40, 420, 60), "");
		GUI.Label(new Rect(30, 50, 400, 40), statusText, style);

		string info =
			"Head: " + (headTransform != null ? "OK" : "None") + "\n" +
			"Left Palm: " + (leftPalm != null ? "OK" : "None") + "\n" +
			"Right Palm: " + (rightPalm != null ? "OK" : "None") + "\n" +
			"Left Top: " + (leftTop != null ? "OK" : "None") + "\n" +
			"Right Top: " + (rightTop != null ? "OK" : "None") + "\n" +
			"Webcam: " + ((webcamTexture != null && webcamTexture.isPlaying) ? "OK" : "None") + "\n" +
			"AR Camera: " + (arOverlayCamera != null ? "OK" : "None");

		GUI.Box(new Rect(20, 110, 420, 170), "");
		GUI.Label(new Rect(30, 120, 400, 160), info, style);

		if (GUI.Button(new Rect(20, 290, 140, 50), "Start"))
		{
			StartRecording();
		}

		if (GUI.Button(new Rect(180, 290, 140, 50), "Stop"))
		{
			StopRecording();
		}

		GUI.Box(new Rect(20, 350, 560, 110), "");
		GUI.Label(
			new Rect(30, 360, 540, 100),
			"Left dir_x,y,z = (" +
			leftDir.x.ToString("F3") + ", " +
			leftDir.y.ToString("F3") + ", " +
			leftDir.z.ToString("F3") + ")\n" +
			"Right dir_x,y,z = (" +
			rightDir.x.ToString("F3") + ", " +
			rightDir.y.ToString("F3") + ", " +
			rightDir.z.ToString("F3") + ")",
			style
		);

		GUI.Box(new Rect(20, 470, 660, 130), "");
		GUI.Label(
			new Rect(30, 480, 640, 120),
			"R：開始\nT：停止\n" +
			"handpose_log.csv：時間 + head + left/right palm + top + dir\n" +
			"images = 純相機影像\n" +
			"ARimages = 相機 + 藍球合成\n" +
			"若畫面仍顯示 roll/pitch/yaw，代表 Scene 裡還有舊版 GUI script 在跑",
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