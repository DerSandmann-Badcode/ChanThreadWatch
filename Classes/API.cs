using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.IO;

namespace JDP {
	public class API {
		public event EventHandler<string> NewThread;
		class Route : Attribute {
			public string Map;
			public Route(string s) {
				Map = s;
			}
		}
		public interface IHttpMethod {
			string Method();
		}

		class HttpGet : Attribute, IHttpMethod {
			string IHttpMethod.Method() {
				return "GET";
			}
		}
		class HttpPost : Attribute, IHttpMethod {
			string IHttpMethod.Method() {
				return "POST";
			}
		}
		class HttpPut : Attribute, IHttpMethod {
			string IHttpMethod.Method() {
				return "PUT";
			}
		}
		public void Listen() {
			HttpListener listener = new HttpListener();
			listener.Prefixes.Add($"http://*:{Settings.WebServicePort}/"); // TODO - Correctly pull this
			listener.Start();
			while (true) {
				HttpListenerContext httpListenerContext = listener.GetContext();
				ThreadPool.QueueUserWorkItem((_) => {
					string methodName = httpListenerContext.Request.Url.Segments[1].Replace("/", "");
					string httpMethod = httpListenerContext.Request.HttpMethod;
					string httpContent = string.Empty;
					using (StreamReader reader = new StreamReader(httpListenerContext.Request.InputStream)) {
						httpContent = reader.ReadToEnd();
					}
					string[] strParams = { httpContent }; // TODO - Json\XML Serialisation
					var matchingRoutes = GetType().GetMethods().Where(mi => mi.GetCustomAttributes(true).Any(attr => (attr is Route) && ((Route)attr).Map == methodName));
					if (!matchingRoutes) {

					}
					var matchingHttpMethod = matchingRoutes.First(mi => mi.GetCustomAttributes(true).Any(attr => (attr is IHttpMethod) && ((IHttpMethod)attr).Method() == httpMethod));
					object[] @params = matchingHttpMethod.GetParameters()
										.Select((p, i) => Convert.ChangeType(strParams[i], p.ParameterType))
										.ToArray();
					object ret = matchingHttpMethod.Invoke(this, @params);
					httpListenerContext.Response.StatusCode = (int)HttpStatusCode.Created; // TODO - Handle routes not found - proper responses
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

		[HttpPost]
		[Route("Threads")]
		public void AddThread(string url) {
			NewThread?.Invoke(this, url);
		}

		[HttpGet]
		[Route("Threads")]
		public void GetThreads() {
			Console.WriteLine("Get Threads");
		}

		[HttpPut]
		[Route("Threads")]
		public void UpdateThread() {
			Console.WriteLine("Put Threads");
		}


	}
}
