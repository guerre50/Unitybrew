using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using WebSocketSharp;
using LitJson;
using System;

namespace Spacebrew {
	// http://lbv.github.io/litjson/docs/quickstart.html

	public class IO {
		public string name { get; set; }
		public string type { get; set; }
		public IO(string name, string type) {
			this.name = name;
			this.type = type;
		}
	}

	public class IOSubscriber {
		public List<IO> messages { get; private set; }
		public IOSubscriber() {
			messages = new List<IO>();
		}
	}

	public class ClientConfig {
		public string name { get; set; }
		public string description { get; set; }
		public IOSubscriber publish { get; private set; }
		public IOSubscriber subscribe { get; private set; }
		public ClientConfig(string name, string description) {
			this.name = name;
			this.description = description;
			publish = new IOSubscriber();
			subscribe = new IOSubscriber();
		}
	}

	public class SpacebrewMessage {
		public string clientName { get; set; }
		public string name { get; set; }
		public string type { get; set; }
		public string value { get; set; }
		public SpacebrewMessage(string clientName, string name, string type, string value) {
			this.clientName = clientName;
			this.name = name;
			this.type = type;
			this.value = value;
		}
	}

	public class SpacebrewClient {
		private string serverURI;
		private WebSocket ws = null;
		public bool reconnect;
		public bool debug;
		private Thread reconnectTimeout = null;
		private static int RECONNECT_TIME = 5000;
		public bool isConnected { 
			get;
			private set;
		}

		private ClientConfig config;

		public string name {
			get {
				return config.name;
			}
			set {
				if (isConnected) {
					Log("setName:Spacebrew", "consider setting name BEFORE connecting to Spacebrew");
				} else {
					config.name = value;
				}
			}
		}

		public string description {
			get {
				return config.description;
			}
			set {
				if (isConnected) {
					Log("setName:Spacebrew", "consider setting description BEFORE connecting to Spacebrew");
				} else {
					config.description = value;
				}
			}
		}

		public delegate void OnMessageHandler(JsonData message);
		public event OnMessageHandler OnMessage;
		public delegate void OnCloseHandler();
		public event OnCloseHandler OnClose;
		public delegate void OnErrorHandler(JsonData message);
		public event OnErrorHandler OnError;
		public delegate void OnOpenHandler();
		public event OnOpenHandler OnOpen;

		public delegate void OnRangeMessageHandler(string name, int value);
		public event OnRangeMessageHandler OnRangeMessage;
		public delegate void OnBooleanMessageHandler(string name, bool value);
		public event OnBooleanMessageHandler OnBooleanMessage;
		public delegate void OnStringMessageHandler(string name, string value);
		public event OnStringMessageHandler OnStringMessage;
		public delegate void OnCustomMessageHandler(string name, string value, string type);
		public event OnCustomMessageHandler OnCustomMessage;
	 
		public SpacebrewClient(string server, string name, string description) {
			isConnected = false;
			debug = false;
			serverURI = server;
			config = new ClientConfig(name, description);
		}

		public void Connect() {
			try {
				ws = new WebSocket (serverURI);
		        ws.OnMessage += (sender, e) =>
		        	_OnMessage(sender, e.Data);

		        ws.OnOpen += (sender, e) =>
		        	_OnOpen(sender);

		        ws.OnClose += (sender, e) =>
		        	_OnClose(sender, e);

		        ws.OnError += (sender, e) =>
		        	_OnError(sender, e);

		        ws.Connect ();
		    } catch {
		    	isConnected = false;
		    	Log("connect:Spacebrew", "connection attempt failed");
		    }
		}

		public void Close() {
			try {
				if (isConnected) {
					ws.Close();
				}
			} catch {
				Log("close:Spacebrew", "close error");
			}
			isConnected = false;
		}

		public void AddPublish(string name, string type) {
			config.publish.messages.Add(new IO(name, type));
			UpdatePubSub();
		}


		public void AddSubscribe(string name, string type) {
			config.subscribe.messages.Add(new IO(name, type));
			UpdatePubSub();
		}

		public void Send(string name, string type, string value) {
			if (isConnected) {
				SpacebrewMessage message = new SpacebrewMessage(config.name, name, type, value);
				ws.Send("{\"message\":" + JsonMapper.ToJson(message).ToString() + "}");
			}
		}

		void _OnMessage(object sender, string m) {
			JsonData data = new JsonData();
			try {
				data = JsonMapper.ToObject(m);
			} catch (Exception ex) {
				Log("_onMessage:Spacebrew", "Error decoding json: " + ex + "\n" + m);
			}

			string name, type;
			JsonData message = data["message"];

			if (message != null) {
				if (message["name"] != null) {
					name = (string) message["name"];
					type = (string) message["type"];
					Debug.Log(message["value"]);
					switch (type) {
						case "boolean":
							OnBooleanMessage(name, (bool) message["value"]);
							break;
						case "string":
							OnStringMessage(name, (string) message["value"]);
							break;
						case "range":
							OnRangeMessage(name, (int) message["value"]);
							break;
						default:
							OnCustomMessage(name, (string) message["value"], type);
						break;
					}
				}
			}
		}

		void _OnOpen(object sender) {
			Log("_onOpen:Spacebrew", "Spacebrew connection opened, client name is: " + config.name);
			isConnected = true;

			if (reconnectTimeout != null) {
				Log("_onOpen:Spacebrew", "tearing down reconnect timer");
				reconnectTimeout = clearTimeout(reconnectTimeout);
			}


			UpdatePubSub();
			OnOpen();
		}

		void _OnClose(object sender, CloseEventArgs message) {
			isConnected = false;

			Log("_onClose:Spacebrew", "Spacebrew connection closed");

			if (reconnect && reconnectTimeout == null) {
				Log("_onClose:Spacebrew", "setting up reconnect timer");
				reconnectTimeout = setTimeout(() => {
					Log("reconnect:Spacebrew", "attempting to reconnect to spacebrew");
					Connect();
				}, RECONNECT_TIME);
			}

			OnClose();
		}

		void _OnError(object sender, ErrorEventArgs message) {
			//OnError();
		}

		void UpdatePubSub() {
			if (isConnected) {
				ws.Send("{\"config\":" + JsonMapper.ToJson(config).ToString() + "}");
			}
		}

		void Log(string tag, string message) {
			if (debug) Debug.Log("[" + tag + "]:" + message);
		}

		private delegate void Action();
		Thread setTimeout(Action action, int timeout) {
	        Thread t = new Thread(() => {
                Thread.Sleep(timeout);
                action();
            });
	        t.Start();

	        return t;
    	}

    	Thread clearTimeout(Thread t) {
    		t.Abort();

    		return null;
    	}
	}
}