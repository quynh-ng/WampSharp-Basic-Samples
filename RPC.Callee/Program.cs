using System;
using System.Diagnostics;
using System.Configuration;
using System.Threading.Tasks;

using WampSharp.V2;
using WampSharp.V2.Rpc;
using WampSharp.V2.Core.Contracts;

namespace net.vieapps.TestLabs.WAMP
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("WAMP Callee........");

			IWampChannel channel = null;

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
				channel.RealmProxy.Monitor.ConnectionEstablished += (sender, arguments) => {
					Console.WriteLine("Connection is established - Session ID:" + arguments.SessionId.ToString());
					Console.WriteLine("");
					Console.WriteLine("Ctrl + C to terminate");
					Console.WriteLine("");
				};
				await channel.Open();

				var options = new CalleeRegistrationInterceptor(new RegisterOptions()
				{
					Invoke = WampInvokePolicy.Roundrobin
				});

				await channel.RealmProxy.Services.RegisterCallee(new StaticUri(), options);

				await channel.RealmProxy.Services.RegisterCallee(new DynamicUri1(), options);

				await channel.RealmProxy.Services.RegisterCallee(new DynamicUri2(), options);

				Console.WriteLine("RPC method is registered...");
				Console.WriteLine("");
				Console.WriteLine("Wait for the calls.........");

			}).ConfigureAwait(false);

			Console.CancelKeyPress += delegate
			{
				channel.Close();
			};
			while (true) { }
		}

		public class StaticUri : StaticUriRPC
		{
			public Task<string> SayHelloAsync()
			{
				var say = "HELLO from callee --> PID: " + Process.GetCurrentProcess().Id.ToString() + " [" + DateTime.Now.ToString("HH:mm:ss") + "]";
				Console.WriteLine("Got one call: " + say);
				return Task.FromResult(say);
			}

			public Task<string> SayGoodbyeAsync()
			{
				var say = "GOODBYE from callee --> PID: " + Process.GetCurrentProcess().Id.ToString() + " [" + DateTime.Now.ToString("HH:mm:ss") + "]";
				Console.WriteLine("Got one call: " + say);
				return Task.FromResult(say);
			}
		}

		public class DynamicUri1 : DynamicUriRPC
		{
			[WampProcedure("net.vieapps.testlabs.rpc.dynamic.1")]
			public Task<string> DoSomethingAsync()
			{
				var say = "DYNAMIC (1) callee --> PID: " + Process.GetCurrentProcess().Id.ToString() + " [" + DateTime.Now.ToString("HH:mm:ss") + "]";
				Console.WriteLine("Got one call: " + say);
				return Task.FromResult(say);
			}
		}

		public class DynamicUri2 : DynamicUriRPC
		{
			[WampProcedure("net.vieapps.testlabs.rpc.dynamic.2")]
			public Task<string> DoSomethingAsync()
			{
				var say = "DYNAMIC [2] callee --> PID: " + Process.GetCurrentProcess().Id.ToString() + " [" + DateTime.Now.ToString("HH:mm:ss") + "]";
				Console.WriteLine("Got one call: " + say);
				return Task.FromResult(say);
			}
		}

	}
}