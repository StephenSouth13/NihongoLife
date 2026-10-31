using NUnit.Framework;
using NihongoLife.Learning;

namespace NihongoLife.Tests
{
    /// <summary>Client side of the teacher "decline" (database RPC reject_speaking_request); the RPC itself was run live.</summary>
    public class OnlinePlaytestTests
    {
        [Test]
        public void RejectRpc_UrlAndBody()
        {
            Assert.AreEqual("https://x.supabase.co/rest/v1/rpc/reject_speaking_request", AgoraClassroomService.RejectRpcUrl("https://x.supabase.co/"));
            Assert.AreEqual("{\"request_id\":\"abc\"}", AgoraClassroomService.RejectRpcBody("abc"));
            Assert.AreEqual("{\"request_id\":\"a\\\"b\"}", AgoraClassroomService.RejectRpcBody("a\"b"), "ids are escaped");
            Assert.AreEqual("{\"request_id\":\"\"}", AgoraClassroomService.RejectRpcBody(null));
        }
    }
}
