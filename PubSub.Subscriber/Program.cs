using System;
using System.Configuration;
using System.Threading.Tasks;
using WampSharp.V2;

namespace net.vieapps.TestLabs.WAMP
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("WAMP Subscriber........");

			IWampChannel channel = null;
			IDisposable subscriber = null;

			Task.Run(async () =>
			{
				var endpoint = ConfigurationManager.AppSettings["EndPoint"];
				if (string.IsNullOrEmpty(endpoint))
					endpoint = "ws://127.0.0.1:26429/";

				var realm = ConfigurationManager.AppSettings["Realm"];
				if (string.IsNullOrEmpty(realm))
					realm = "VIEAppsRealm";

				Console.WriteLine("Attempt to connect to " + endpoint + realm);

				channel = (new DefaultWampChannelFactory()).CreateMsgpackChannel(endpoint, realm);
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
				Console.WriteLine("Start to fetch messages of [" + topicURI + "]");
				Console.WriteLine("");

				subscriber = channel.RealmProxy.Services.GetSubject<string>(topicURI)
					.Subscribe(msg =>
					{
						Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " >> " + msg + "\r\n");
					});
			});

			Console.ReadLine();
			subscriber.Dispose();
			channel.Close();
		}
	}
}