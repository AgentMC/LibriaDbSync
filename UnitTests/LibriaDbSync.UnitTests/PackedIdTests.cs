using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LibriaDbSync.UnitTests
{
    [TestClass]
    public class PackedIdTests
    {
        [DataTestMethod]
        [DataRow(0, 0, 0)]
        [DataRow(65535, 0, 65535)]
        [DataRow(0b0111_1111_1111_1111_0000_0000_0000_0000, 32767, 0)]
        [DataRow(int.MaxValue, 32767, 65535)]
        [DataRow(459136, 7, 384)]
        [DataRow(1573252, 24, 388)]
        [DataRow(65929, 1, 393)]
        [DataRow(532314, 8, 8026)]
        public void UnpackV0Test(int packed, int episode, int release)
        {
            var unpacked = PackedId.Unpack(packed);
            Assert.AreEqual(0, unpacked.Version, "Version is wrong");
            Assert.AreEqual(episode, unpacked.EpisodeId, "Episode is wrong");
            Assert.AreEqual(release, unpacked.ReleaseId, "Release is wrong");
        }

        [DataTestMethod]
        [DataRow(int.MinValue, 0, 0)]
        [DataRow(unchecked((int)0b1000_0000_0000_1111_1111_1111_1111_1111), 0, 1048575)]
        [DataRow(unchecked((int)0b1111_1111_1111_0000_0000_0000_0000_0000), 2047, 0)]
        [DataRow(-1, 2047, 1048575)]
        [DataRow(-2140143232, 7, 384)]
        [DataRow(-2122317436, 24, 388)]
        [DataRow(-2146434679, 1, 393)]
        [DataRow(-2139087014, 8, 8026)]
        public void UnpackV1Test(int packed, int episode, int release)
        {
            var unpacked = PackedId.Unpack(packed);
            Assert.AreEqual(1, unpacked.Version, "Version is wrong");
            Assert.AreEqual(episode, unpacked.EpisodeId, "Episode is wrong");
            Assert.AreEqual(release, unpacked.ReleaseId, "Release is wrong");
        }

        [DataTestMethod]
        [DataRow(int.MinValue, 0, 0)]
        [DataRow(unchecked((int)0b1000_0000_0000_1111_1111_1111_1111_1111), 0, 1048575)]
        [DataRow(unchecked((int)0b1111_1111_1111_0000_0000_0000_0000_0000), 2047, 0)]
        [DataRow(-1, 2047, 1048575)]
        [DataRow(-2140143232, 7, 384)]
        [DataRow(-2122317436, 24, 388)]
        [DataRow(-2146434679, 1, 393)]
        [DataRow(-2139087014, 8, 8026)]
        public void PackV1Test(int packed, int episode, int release)
        {
            var actual = new PackedId(release, episode).Pack();
            Assert.AreEqual(packed, actual, "Wrongly packed");
        }

        [DataTestMethod]
        [DataRow(0, 0, 0)]
        [DataRow(65535, 0, 65535)]
        [DataRow(0b0111_1111_1111_1111_0000_0000_0000_0000, 32767, 0)]
        [DataRow(int.MaxValue, 32767, 65535)]
        [DataRow(459136, 7, 384)]
        [DataRow(1573252, 24, 388)]
        [DataRow(65929, 1, 393)]
        [DataRow(532314, 8, 8026)]
        public void PackV0Test(int packed, int episode, int release)
        {
            var packer = new PackedId(release, episode);
            typeof(PackedId).GetField(nameof(packer.Version), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).SetValueDirect(__makeref(packer), 0);
            var actual = packer.Pack();
            Assert.AreEqual(packed, actual, "Wrongly packed");
        }

        [TestMethod]
        public void ConstructorTest()
        {
            var rnd = new Random();
            var inputs = new[] { rnd.Next(1048575), rnd.Next(2047) };
            var packer = new PackedId(inputs[0], inputs[1]);
            Assert.AreEqual(1, packer.Version, "Version is wrong");
            Assert.AreEqual(inputs[1], packer.EpisodeId, "Episode is wrong");
            Assert.AreEqual(inputs[0], packer.ReleaseId, "Release is wrong");
        }

        [TestMethod]
        public void RoundRobinTest()
        {
            var rnd = new Random();
            for (int i = 0; i < 100; i++)
            {
                var source = rnd.Next(int.MinValue, int.MaxValue);
                var p1 = PackedId.Unpack(source);
                var p2 = new PackedId(p1.ReleaseId, p1.EpisodeId);
                if(p1.Version == 0)
                {
                    typeof(PackedId).GetField(nameof(p2.Version), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).SetValueDirect(__makeref(p2), 0);
                }
                Assert.AreEqual(source, p1.Pack(), "Round-robin 1st iteration didn't work for " + source);
                Assert.AreEqual(p1.Version, p2.Version, "Version wrong");
                Assert.AreEqual(p1.EpisodeId, p2.EpisodeId, "EpisodeId wrong");
                Assert.AreEqual(p1.ReleaseId, p2.ReleaseId, "ReleaseId wrong");
                Assert.AreEqual(source, p2.Pack(), "Round-robin 2rd iteration didn't work for " + source);
            }
        }
    }
}
