using System.Threading.Tasks;
using WampSharp.V2.Rpc;

namespace net.vieapps.TestLabs.WAMP
{
	public interface IStaticUriRPC
	{
		[WampProcedure("net.vieapps.testlabs.rpc.static.hello")]
		Task<string> SayHelloAsync();

		[WampProcedure("net.vieapps.testlabs.rpc.static.goodbye")]
		Task<string> SayGoodbyeAsync();
	}

	public interface IDynamicUriRPC
	{
		[WampProcedure("net.vieapps.testlabs.rpc.dynamic.{0}")]
		Task<string> DoSomethingAsync();
	}

	public interface IEmptyMicroService
	{
		[WampProcedure("net.vieapps.testlabs.services.empty")]
		Task<string> DoSomethingAsync();
	}

	public interface IErrorMicroService
	{
		[WampProcedure("net.vieapps.testlabs.services.error")]
		Task<string> DoSomethingAsync();
	}
}