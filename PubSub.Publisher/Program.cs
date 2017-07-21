using System;
using System.Diagnostics;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Reactive.Linq;
using WampSharp.V2;

namespace net.vieapps.TestLabs.WAMP
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("WAMP Publisher........");

			var ip = "";
			var host = Dns.GetHostEntry(Dns.GetHostName());
			foreach (var address in host.AddressList)
				if (address.AddressFamily == AddressFamily.InterNetwork)
				{
					ip = address.ToString();
					break;
				}

			IWampChannel channel = null;
			IDisposable publisher = null;

			Task.Run(async () =>
			{
				var endpoint = ConfigurationManager.AppSettings["EndPoint"];
				if (string.IsNullOrEmpty(endpoint))
					endpoint = "ws://127.0.0.1:26429/";

				var realm = ConfigurationManager.AppSettings["Realm"];
				if (string.IsNullOrEmpty(realm))
					realm = "VIEAppsRealm";

				Console.WriteLine("Attempt to connect to " + endpoint + realm);

				channel = (new DefaultWampChannelFactory()).CreateJsonChannel(endpoint, realm);
				channel.RealmProxy.Monitor.ConnectionEstablished += (sender, arguments) =>
				{
					Console.WriteLine("Connection is established - Session ID:" + arguments.SessionId.ToString());
					Console.WriteLine("");
					Console.WriteLine("Press RETURN to terminate...");
					Console.WriteLine("");
				};

				await channel.Open();
			})
			.ContinueWith(task =>
			{
				var topicURI = ConfigurationManager.AppSettings["TopicURI"];
				if (string.IsNullOrEmpty(topicURI))
					topicURI = "net.vieapps.testlabs.wamp";

				Console.WriteLine("");
				Console.WriteLine("Start to publish messages to [" + topicURI + "]");
				Console.WriteLine("");

				var subject = channel.RealmProxy.Services.GetSubject<string>(topicURI);
				var counter = 0;
				var timer = Observable.Timer(TimeSpan.FromMilliseconds(0), TimeSpan.FromMilliseconds(200));
				var id = Process.GetCurrentProcess().Id.ToString() + ":" + AppDomain.CurrentDomain.Id.ToString();

				publisher = timer.Subscribe(x =>
				{
					if (counter.Equals(Int32.MaxValue))
						counter = 0;
					counter++;

					Console.WriteLine(counter + " :-> " + topicURI + " [" + ip + " #" + id + "]");
					subject.OnNext("{\"msg\":\"Message from [" + ip + " #" + id + "]: " + topicURI + " #" + counter + " [" + x + "]" + "\"}");
				});
			});

			Console.ReadLine();
			publisher.Dispose();
			channel.Close();
		}
	}
}