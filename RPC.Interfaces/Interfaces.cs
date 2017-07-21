using System.Threading.Tasks;
using WampSharp.V2.Rpc;

namespace net.vieapps.TestLabs.WAMP
{
	public interface StaticUriRPC
	{
		[WampProcedure("net.vieapps.testlabs.rpc.static.hello")]
		Task<string> SayHelloAsync();

		[WampProcedure("net.vieapps.testlabs.rpc.static.goodbye")]
		Task<string> SayGoodbyeAsync();
	}

	public interface DynamicUriRPC
	{
		[WampProcedure("net.vieapps.testlabs.rpc.dynamic.{0}")]
		Task<string> DoSomethingAsync();
	}
}