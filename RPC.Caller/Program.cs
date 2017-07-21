using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Reflection;

using WampSharp.V2;
using WampSharp.V2.Core.Contracts;

namespace net.vieapps.TestLabs.WAMP
{
	class Program
	{
		static IWampChannel Channel = null;

		static void Main(string[] args)
		{
			Console.WriteLine("WAMP Caller........");

			Task.Run(async () =>
			{
				await OpenChannelAsync();
			}).ConfigureAwait(false);

			Console.CancelKeyPress += delegate
			{
				Channel.Close();
			};

			var running = true;
			while (running)
			{
				var key = Console.ReadKey();
				var theKey = (int)key.KeyChar;
				Console.WriteLine(" " + theKey.ToString() + " +> ");
				if (theKey == 27)
					Task.Run(async () =>
					{
						Console.WriteLine("");
						Console.WriteLine("Press ESC one more time to quit....");
						await SayGoodbyeAsync().ContinueWith(t =>
						{
							running = false;
						});
					}).ConfigureAwait(false);

				else if (theKey == 13)
					Task.Run(async () =>
					{
						await DoSomethingAsync();
					}).ConfigureAwait(false);

				else if (theKey >= 48 && theKey <= 53)
					Task.Run(async () =>
					{
						await DoSomethingAsync(1);
					}).ConfigureAwait(false);

				else if (theKey >= 54 && theKey <= 59)
					Task.Run(async () =>
					{
						await DoSomethingAsync(2);
					}).ConfigureAwait(false);

				else
					Task.Run(async () =>
					{
						await SayHelloAsync();
					}).ConfigureAwait(false);
			}
		}

		static async Task OpenChannelAsync()
		{
			var endpoint = ConfigurationManager.AppSettings["EndPoint"];
			if (string.IsNullOrEmpty(endpoint))
				endpoint = "ws://127.0.0.1:26429/";

			var realm = ConfigurationManager.AppSettings["Realm"];
			if (string.IsNullOrEmpty(realm))
				realm = "VIEAppsRealm";

			Console.WriteLine("Attempt to connect to " + endpoint + realm);

			Channel = (new DefaultWampChannelFactory()).CreateJsonChannel(endpoint, realm);
			Channel.RealmProxy.Monitor.ConnectionEstablished += (sender, args) => {
				Console.WriteLine("Connection is established - Session ID:" + args.SessionId.ToString());
				Console.WriteLine("");
				Console.WriteLine("Press any key to say hello, RETURN to call dynamic, ESC to say goodbye, Ctrl + C to terminate...");
				Console.WriteLine("");
			};
			await Program.Channel.Open();
		}

		static StaticUriRPC Static = null;

		static async Task SayHelloAsync()
		{
			if (Static == null)
				Static = Channel.RealmProxy.Services.GetCalleeProxy<StaticUriRPC>();

			var hello = await Static.SayHelloAsync();
			Console.WriteLine(" -> " + hello);
		}

		static async Task SayGoodbyeAsync()
		{
			if (Static == null)
				Static = Channel.RealmProxy.Services.GetCalleeProxy<StaticUriRPC>();

			var hello = await Static.SayGoodbyeAsync();
			Console.WriteLine(" -> " + hello);
			await Task.Delay(567);
		}

		static DynamicUriRPC Dynamic1 = null, Dynamic2 = null;
		static Random Rnd = new Random();

		static async Task DoSomethingAsync(int number = 0)
		{
			number = number == 0
				? Rnd.Next() % 2
				: number;

			if (number == 1)
			{
				if (Dynamic1 == null)
					Dynamic1 = Channel.RealmProxy.Services.GetCalleeProxy<DynamicUriRPC>(new DynamicCalleeProxyInterceptor("1"));
				var smt = await Dynamic1.DoSomethingAsync();
				Console.WriteLine("-> " + smt);
			}
			else
			{
				if (Dynamic2 == null)
					Dynamic2 = Channel.RealmProxy.Services.GetCalleeProxy<DynamicUriRPC>(new DynamicCalleeProxyInterceptor("2"));
				var smt = await Dynamic2.DoSomethingAsync();
				Console.WriteLine("-> " + smt);
			}
		}

		public class DynamicCalleeProxyInterceptor : CalleeProxyInterceptor
		{
			string name;

			public DynamicCalleeProxyInterceptor(string name) : base(new CallOptions())
			{
				this.name = name;
			}

			public override string GetProcedureUri(MethodInfo method)
			{
				return string.Format(base.GetProcedureUri(method), name);
			}
		}

	}
}