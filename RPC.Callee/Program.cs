﻿using System;
using System.Diagnostics;
using System.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

using Newtonsoft.Json.Linq;

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

				channel = (new DefaultWampChannelFactory()).CreateMsgpackChannel(endpoint, realm);
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

				await channel.RealmProxy.Services.RegisterCallee(new ErrorService(), options);

				await channel.RealmProxy.Services.RegisterCallee(new DynamicUri1(), options);

				await channel.RealmProxy.Services.RegisterCallee(new DynamicUri2(), options);

				Console.WriteLine("RPC methods are registered...");
				Console.WriteLine("");
				Console.WriteLine("Wait for the calls.........");

			}).ConfigureAwait(false);

			Console.CancelKeyPress += delegate
			{
				channel.Close();
			};
			while (true) { }
		}

		static string ip = null;
		static string pid = null;

		static string GetInfo()
		{
			if (string.IsNullOrWhiteSpace(ip))
			{
				var host = Dns.GetHostEntry(Dns.GetHostName());
				foreach (var address in host.AddressList)
					if (address.AddressFamily == AddressFamily.InterNetwork)
					{
						ip = address.ToString();
						break;
					}
			}

			if (string.IsNullOrWhiteSpace(pid))
				pid = Process.GetCurrentProcess().Id.ToString();

			return "(PID: " + pid + " - IP: " + ip + " - " + DateTime.Now.ToString("HH:mm:ss") + ")";
		}

		public class StaticUri : IStaticUriRPC
		{
			public Task<string> SayHelloAsync()
			{
				var say = "HELLO from callee --> " + GetInfo();
				Console.WriteLine("Got one call: " + say);
				return Task.FromResult(say);
			}

			public Task<string> SayGoodbyeAsync()
			{
				var say = "GOODBYE from callee --> " + GetInfo();
				Console.WriteLine("Got one call: " + say);
				return Task.FromResult(say);
			}
		}

		public abstract class DynamicUri : IDynamicUriRPC
		{
			[WampProcedure("net.vieapps.testlabs.rpc.dynamic.{0}")]
			public abstract Task<string> DoSomethingAsync();
		}

		public class DynamicUri1 : IDynamicUriRPC
		{
			[WampProcedure("net.vieapps.testlabs.rpc.dynamic.1")]
			public Task<string> DoSomethingAsync()
			{
				var say = "DYNAMIC (1) callee --> " + GetInfo();
				Console.WriteLine("Got one call: " + say);
				return Task.FromResult(say);
			}
		}

		public class DynamicUri2 : IDynamicUriRPC
		{
			[WampProcedure("net.vieapps.testlabs.rpc.dynamic.2")]
			public Task<string> DoSomethingAsync()
			{
				var say = "DYNAMIC [2] callee --> " + GetInfo();
				Console.WriteLine("Got one call: " + say);
				return Task.FromResult(say);
			}
		}

		public class ErrorService: IErrorMicroService
		{
			[WampProcedure("net.vieapps.testlabs.services.error")]
			public Task<string> DoSomethingAsync()
			{
				Console.WriteLine("Got one call of ErrSvc --> " + GetInfo());
				try
				{
					throw new Exception("Got an error", new ApplicationException("Nothing to lose!!!!!"));
				}
				catch (Exception ex)
				{
					var message = ex.Message;

					var details = new Dictionary<string, object>()
					{
						{ "0", ex.StackTrace }
					};

					var counter = 0;
					var inner = ex.InnerException;
					while (inner != null)
					{
						counter++;
						details.Add(counter.ToString(), inner.StackTrace);
						inner = inner.InnerException;
					}

					var info = new JObject()
					{
						{ "ID", "ID" },
						{ "Time", DateTime.Now }
					};

					return Task.FromException<string>(
							new WampRpcRuntimeException(details, new Dictionary<string, object>(), new Dictionary<string, object>() { { "info", info } }, message, ex)
						);
				}
			}
		}

	}
}