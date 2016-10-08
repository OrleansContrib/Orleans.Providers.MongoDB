using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.UnitTest.Reminders
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Orleans.TestingHost;

    [TestClass]
    public class Test
    {
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            TestCluster cluster = new TestCluster();
            cluster.Deploy();
        }

        [TestMethod]
        public void TestMethod1()
        {
            var a = "";
        }
    }
}
