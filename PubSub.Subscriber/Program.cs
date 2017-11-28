using System;
using System.Configuration;
using System.Threading.Tasks;
using WampSharp.V2;

namespace net.vieapps.TestLabs.WAMP
{
	class Program
	{
		static IWampChannel Channel = null;
		static IDisposable Subscriber = null;

		static void Main(string[] args)
		{
			Console.WriteLine("WAMP Subscriber........");

			Task.Run(async () =>
			{
				await ConnectAsync();
				DoSubscribe();
			}).ConfigureAwait(false);

			Console.ReadLine();
			Subscriber?.Dispose();
			Channel?.Close();
		}

		static async Task ConnectAsync()
		{
			var endpoint = ConfigurationManager.AppSettings["EndPoint"];
			if (string.IsNullOrEmpty(endpoint))
				endpoint = "ws://127.0.0.1:26429/";

			var realm = ConfigurationManager.AppSettings["Realm"];
			if (string.IsNullOrEmpty(realm))
				realm = "VIEAppsRealm";

			Console.WriteLine("Attempt to connect to " + endpoint + realm);

			Channel = (new DefaultWampChannelFactory()).CreateMsgpackChannel(endpoint, realm);
			Channel.RealmProxy.Monitor.ConnectionEstablished += (sender, arguments) =>
			{
				Console.WriteLine("Connection is established - Session ID:" + arguments.SessionId.ToString());
				Console.WriteLine("");
				Console.WriteLine("Press RETURN to terminate...");
				Console.WriteLine("");
			};

			try
			{
				await Channel.Open().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
				Channel = null;
			}
		}

		static void DoSubscribe()
		{
			if (Channel == null)
			{
				Console.WriteLine("No WAMP channel is available");
				return;
			}

			var topicURI = ConfigurationManager.AppSettings["TopicURI"];
			if (string.IsNullOrEmpty(topicURI))
				topicURI = "net.vieapps.testlabs.wamp";

			Console.WriteLine("");
			Console.WriteLine("Start to fetch messages of [" + topicURI + "]");
			Console.WriteLine("");

			Subscriber = Channel.RealmProxy.Services.GetSubject<string>(topicURI)
				.Subscribe(msg =>
				{
					Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " >> " + msg + "\r\n");
				});
		}
	}
}