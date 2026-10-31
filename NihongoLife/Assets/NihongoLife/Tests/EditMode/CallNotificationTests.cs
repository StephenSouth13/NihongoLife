using NihongoLife.Learning;
using NUnit.Framework;
using UnityEngine;

namespace NihongoLife.Tests.EditMode
{
    public class CallNotificationTests
    {
        [Test]
        public void Ringtone_IsAudibleAndClipped()
        {
            var clip = CallRingtone.CreateClip();
            Assert.Greater(clip.length, 1f);
            var data = new float[clip.samples];
            clip.GetData(data, 0);
            float peak = 0f; foreach (var v in data) peak = Mathf.Max(peak, Mathf.Abs(v));
            Assert.Greater(peak, 0.2f); Assert.LessOrEqual(peak, 1f);
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void Ringtone_IsThrottled()
        {
            Assert.IsTrue(CallRingtone.ShouldRing(10f, -99f));
            Assert.IsFalse(CallRingtone.ShouldRing(11f, 10f));
            Assert.IsTrue(CallRingtone.ShouldRing(14.5f, 10f));
        }

        [Test]
        public void Decline_UsesEdgeFunctionAction()
        {
            Assert.AreEqual("{\"action\":\"decline\",\"requestId\":\"a\\\"b\"}", AgoraClassroomService.DeclineBody("a\"b"));
            StringAssert.EndsWith("/rest/v1/rpc/reject_speaking_request", AgoraClassroomService.RejectRpcUrl("https://x.supabase.co/"));
        }
    }
}
