using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Runtime.Serialization;
using System.IO;

namespace JDP {
	public class API {
		public event EventHandler<string> NewThread;
		class Method : Attribute {
			public string Map;
			public Method(string s) {
				Map = s;
			}
		}

		public void Listen() {
			HttpListener listener = new HttpListener();
			listener.Prefixes.Add($"http://*:{Settings.WebServicePort}/"); // Correctly pull this
			listener.Start();
			while (true) {
				HttpListenerContext httpListenerContext = listener.GetContext();
				ThreadPool.QueueUserWorkItem((_) => {
					string methodName = httpListenerContext.Request.Url.Segments[1].Replace("/", "");
					string test = string.Empty;
					using (StreamReader reader = new StreamReader(httpListenerContext.Request.InputStream)) {
						test = reader.ReadToEnd();
					}
					string[] strParams = new string[] { test };
					var method = this.GetType().GetMethods().First(mi => mi.GetCustomAttributes(true).Any(attr => attr is Method && ((Method)attr).Map == methodName));
					object[] @params = method.GetParameters()
										.Select((p, i) => Convert.ChangeType(strParams[i], p.ParameterType))
										.ToArray();
					object ret = method.Invoke(this, @params);
					httpListenerContext.Response.StatusCode = 200; //TODO: These should be ENUM Codes
					httpListenerContext.Response.Close();
				});
			};
		}
		public void StartListener() {
			Thread thread = new Thread(() => {
				Listen();
			});
			thread.Start();
		}
		[Method("AddThread")]
		public void AddThread(string url) {
			NewThread?.Invoke(this, url);
		}
	}
}
