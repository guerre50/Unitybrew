using UnityEngine;
using System.Collections;
using Spacebrew;
using LitJson;

public class TestClient : MonoBehaviour {
	// Output Values
	int rangeValue = 2;
	int prevRangeValue;
	string stringValue = "WRITE TEXT";
	string prevStringValue;
	public Texture2D colorPicker;

	// Input Values
	public TextMesh textMesh;
	public GameObject[] cubes;
	public GameObject[] lights;
	private bool jump = false;
	private int jumpForce = 2;
	private string text;
	private Color color;

	// Spacebrew data
	SpacebrewClient sb;
	public string spacebrewURI = "ws://insbits.com:9000";
	public string spacebrewName = "unitySpacebrew";
	public string spacebrewDescription = "A test client for Spacebrew and Unity";

	void Start () {
		sb = new SpacebrewClient(spacebrewURI, spacebrewName, spacebrewDescription);
		
		sb.AddPublish("string", "string");
		sb.AddPublish("range", "range");
		sb.AddPublish("boolean", "boolean");
		sb.AddPublish("color", "custom");

		sb.AddSubscribe("string", "string");
		sb.AddSubscribe("range", "range");
		sb.AddSubscribe("boolean", "boolean");
		sb.AddSubscribe("color", "custom");

		sb.debug = true;
		sb.reconnect = true;
		sb.OnRangeMessage += OnRangeMessage;
		sb.OnStringMessage += OnStringMessage;
		sb.OnCustomMessage += OnCustomMessage;
		sb.OnBooleanMessage += OnBooleanMessage;
		sb.OnOpen += OnOpen;
		sb.OnClose += OnClose;
		sb.Connect();

		prevStringValue = stringValue;
		prevRangeValue = rangeValue;

		lights = GameObject.FindGameObjectsWithTag("Lights");
		cubes = GameObject.FindGameObjectsWithTag("Cubes");
	}
	
	void Update () {
		if (prevRangeValue != rangeValue) {
			sb.Send("range", "range", rangeValue + "");
		}
		prevRangeValue = rangeValue;

		if (prevStringValue != stringValue) {
			sb.Send("string", "string", stringValue);
		}
		prevStringValue = stringValue;

		if (text != textMesh.text) {
			textMesh.text = text;
		}

		if (jump) {
			jump = false;
			foreach (GameObject cube in cubes) {
				cube.rigidbody.velocity = new Vector3(Random.Range(-jumpForce, jumpForce), Random.Range(0.0f, jumpForce*2), Random.Range(-jumpForce, jumpForce));
			}
		}

		if (color.a != 0) {
			foreach (GameObject cube in cubes) {
				cube.renderer.material.color = color;
			}
			color.a = 0;
		}
	}

	void OnOpen() {
		Debug.Log("open");
	}

	void OnStringMessage(string name, string value) {
		text = value;
	}

	void OnRangeMessage(string name, int value) {
		jumpForce = value;
	}

	void OnBooleanMessage(string name, bool value) {
		jump = value;
	}

	void OnCustomMessage(string name, string value, string type) {
		JsonData data = JsonMapper.ToObject(value);
		color.a = 0;

		try {
			if (data["h"] != null) {
				float h = float.Parse((string)data["h"]);
				float s = float.Parse((string)data["s"]);
				float b = float.Parse((string)data["b"]);
				color = HSBColor.ToColor(new HSBColor(h, s, b, 1.0f));
			} else if (data["r"] != null) {
				color = new Color((int)data["r"], (int)data["g"], (int)data["b"]);
			}
		} catch (System.Exception ex) {
			Debug.Log("Received wrong color format" + ex);
		}
	}

	void OnClose() {
		Debug.Log("close");
	}

	void OnDestroy()  {
		sb.Close();
	}

	void OnGUI() {
		GUI.Label(new Rect(10, 10, 300, 200), "Range:");
		rangeValue = (int) GUI.HorizontalSlider(new Rect(10, 30, 200, 50), rangeValue, -30, 60);
		GUI.Label(new Rect(10, 70, 300, 200), "String:");
		stringValue = GUI.TextField(new Rect(10, 90, 200, 30), stringValue);

		GUI.Label(new Rect(10, 130, 300, 200), "Bool:");
		if (GUI.Button(new Rect(10, 150, 200, 30), "Press me")) {
			sb.Send("boolean", "boolean", true+"");
		}
		GUI.Label(new Rect(10, 190, 300, 200), "Custom:");
		if (GUI.RepeatButton(new Rect(10, 220, colorPicker.width, colorPicker.height), colorPicker)) {
            Vector2 position = Event.current.mousePosition;
            int x = (int)position.x;
            int y = (int)position.y;
            Color color = colorPicker.GetPixel(x, y - 220);
            sb.Send("color", "custom", ColorToHSBJSON(color));
        }
	}

	private string ColorToHSBJSON(Color color) {
		HSBColor hsb = HSBColor.FromColor(color);

		return "{\"h\":" + hsb.h + ",\"s\":" + hsb.s + ",\"b\":" + hsb.b + "}";
	}
}
