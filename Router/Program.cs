using System;
using System.Configuration;

using WampSharp.V2;

namespace net.vieapps.TestLabs.WAMP
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("WAMP Router..........");

			var endpoint = ConfigurationManager.AppSettings["EndPoint"];
			if (string.IsNullOrEmpty(endpoint))
				endpoint = "ws://127.0.0.1:26429/";

			var realm = ConfigurationManager.AppSettings["Realm"];
			if (string.IsNullOrEmpty(realm))
				realm = "VIEAppsRealm";

			IWampHost host = new DefaultWampHost(endpoint);
			var counters = 0;
			var hostedRealm = host.RealmContainer.GetRealmByName(realm);

			hostedRealm.SessionCreated += (sender, arguments) =>
			{
				counters++;
				Console.WriteLine("\r\n" + "A session is opened..." + "\r\n" + "- Session ID: " + arguments.SessionId.ToString() + "\r\n" + "- Total of opened sessions: " + counters.ToString());
			};
			hostedRealm.SessionClosed += (sender, arguments) =>
			{
				counters--;
				Console.WriteLine("\r\n" + "A session is closed..." + "\r\n" + "- Session ID: " + arguments.SessionId.ToString() + "\r\n" + "- Total of opened sessions: " + counters.ToString());
			};

			host.Open();

			Console.WriteLine("");
			Console.WriteLine("Serving now [" + endpoint + realm + "].... (press RETURN to terminate)");
			Console.WriteLine("");

			Console.ReadLine();
			host.Dispose();
		}

	}
}