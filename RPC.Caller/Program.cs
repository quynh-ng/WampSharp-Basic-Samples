using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;

using Newtonsoft.Json.Linq;

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

				else if (theKey >= 49 && theKey <= 53)
					Task.Run(async () =>
					{
						await DoSomethingAsync(1);
					}).ConfigureAwait(false);

				else if (theKey >= 54 && theKey <= 59)
					Task.Run(async () =>
					{
						await DoSomethingAsync(2);
					}).ConfigureAwait(false);

				else if (theKey == 96)
					Task.Run(async () =>
					{
						await DoEmptyServiceActionAsync();
					}).ConfigureAwait(false);

				else if (theKey == 48)
					Task.Run(async () =>
					{
						await DoErrorServiceActionAsync();
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

			Channel = (new DefaultWampChannelFactory()).CreateMsgpackChannel(endpoint, realm);
			Channel.RealmProxy.Monitor.ConnectionEstablished += (sender, args) => {
				Console.WriteLine("Connection is established - Session ID:" + args.SessionId.ToString());
				Console.WriteLine("");
				Console.WriteLine("Press any key to say hello, RETURN to call dynamic, ESC to say goodbye, Ctrl + C to terminate...");
				Console.WriteLine("");
			};
			await Program.Channel.Open();
		}

		static IEmptyMicroService EmptyService = null;

		static async Task DoEmptyServiceActionAsync()
		{
			try
			{
				if (EmptyService == null)
					EmptyService = Channel.RealmProxy.Services.GetCalleeProxy<IEmptyMicroService>();

				var val = await EmptyService.DoSomethingAsync();
				Console.WriteLine(" Microservice: -> " + val);
			}
			catch (Exception ex)
			{
				ShowError(ex);
			}
		}

		static IErrorMicroService ErrorService = null;

		static async Task DoErrorServiceActionAsync()
		{
			try
			{
				if (ErrorService == null)
					ErrorService = Channel.RealmProxy.Services.GetCalleeProxy<IErrorMicroService>();

				var val = await ErrorService.DoSomethingAsync();
				Console.WriteLine(" Error Microservice: -> " + val);
			}
			catch (Exception ex)
			{
				ShowError(ex);
			}
		}

		static IStaticUriRPC Static = null;

		static async Task SayHelloAsync()
		{
			try
			{
				if (Static == null)
					Static = Channel.RealmProxy.Services.GetCalleeProxy<IStaticUriRPC>();

				var hello = await Static.SayHelloAsync();
				Console.WriteLine(" -> " + hello);
			}
			catch (Exception ex)
			{
				ShowError(ex);
			}
		}

		static async Task SayGoodbyeAsync()
		{
			try
			{
				if (Static == null)
					Static = Channel.RealmProxy.Services.GetCalleeProxy<IStaticUriRPC>();

				var hello = await Static.SayGoodbyeAsync();
				Console.WriteLine(" -> " + hello);
				await Task.Delay(567);
			}
			catch (Exception ex)
			{
				ShowError(ex);
			}
		}

		static IDynamicUriRPC Dynamic1 = null, Dynamic2 = null;
		static Random Rnd = new Random();

		static async Task DoSomethingAsync(int number = 0)
		{
			number = number == 0
				? Rnd.Next() % 2
				: number;

			if (number == 1)
				try
				{
					if (Dynamic1 == null)
						Dynamic1 = Channel.RealmProxy.Services.GetCalleeProxy<IDynamicUriRPC>(new DynamicCalleeProxyInterceptor("1"));
					var smt = await Dynamic1.DoSomethingAsync();
					Console.WriteLine("-> " + smt);
				}
				catch (Exception ex)
				{
					ShowError(ex);
				}
			else
				try
				{
					if (Dynamic2 == null)
						Dynamic2 = Channel.RealmProxy.Services.GetCalleeProxy<IDynamicUriRPC>(new DynamicCalleeProxyInterceptor("2"));
					var smt = await Dynamic2.DoSomethingAsync();
					Console.WriteLine("-> " + smt);
				}
				catch (Exception ex)
				{
					ShowError(ex);
				}
		}

		static void ShowError(Exception exception)
		{
			if (exception is WampException)
			{
				var ex = exception as WampException;
				var type = ex.GetType();
				var msg = ex.Message;
				var json = "";

				if (ex.Arguments != null)
					foreach (var info in ex.Arguments)
						json += info is JObject && (info as JObject).Count > 0
							? (info as JObject).ToString(Newtonsoft.Json.Formatting.Indented) + "\r\n"
							: info is JValue && (info as JValue).Value != null
								? (info as JValue).Value.ToString() + "\r\n"
								: "";

				if (ex.Details != null)
					foreach (var info in ex.Details)
						json += info.Value != null && info.Value is JObject && (info.Value as JObject).Count > 0
							? (info.Value as JObject).ToString(Newtonsoft.Json.Formatting.Indented) + "\r\n"
							: info.Value != null && info.Value is JValue && (info.Value as JValue).Value != null
								? (info.Value as JValue).Value.ToString() + "\r\n"
								: "";

				Console.WriteLine("ERROR of wampsharp: " + ex.Message + " [" + ex.GetType().ToString() + "]");
				Console.WriteLine("------\r\n" + exception.StackTrace);
				Console.WriteLine("------\r\n" + json);
			}
			else
			{
				Console.WriteLine("ERROR: " + exception.Message + " [" + exception.GetType().ToString() + "]");
				var bex = exception.GetBaseException();
				if (bex != null)
					Console.WriteLine("- Base: " + bex.Message + " [" + bex.GetType().ToString() + "]");
				Console.WriteLine("------\r\n" + exception.StackTrace);
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
				var uri = base.GetProcedureUri(method);
				return string.Format(uri, name);
			}
		}

	}
}