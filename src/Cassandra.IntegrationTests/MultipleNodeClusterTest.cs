﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace Cassandra.IntegrationTests
{
    /// <summary>
    /// Represents a set of tests that reuse an test cluster of n node
    /// </summary>
    [TestFixture]
    public class MultipleNodesClusterTest
    {
        protected virtual string CcmLocalConfigDir { get; set; }

        protected virtual ISession Session { get; set; }

        protected virtual int NodeLength { get; set; }

        /// <summary>
        /// Determines if the test should use a remote ccm instance
        /// </summary>
        protected virtual bool UseRemoteCcm
        {
            get
            {
                return ConfigurationManager.AppSettings["UseRemote"] == "true";
            }
        }

        /// <summary>
        /// Creates a new instance of MultipleNodeCluster Test
        /// </summary>
        /// <param name="nodeLength">Determines the amount of nodes in the test cluster</param>
        public MultipleNodesClusterTest(int nodeLength)
        {
            this.NodeLength = nodeLength;
        }

        [TestFixtureSetUp]
        public virtual void TestFixtureSetUp()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
            var keyspaceName = "tester";
            if (UseRemoteCcm)
            {
                CCMBridge.ReusableCCMCluster.Setup(NodeLength);
                CCMBridge.ReusableCCMCluster.Build(Cluster.Builder());
                Session = CCMBridge.ReusableCCMCluster.Connect(keyspaceName);
            }
            else
            {
                //Create a local instance
                CcmLocalConfigDir = TestUtils.CreateTempDirectory();
                var output = TestUtils.ExecuteLocalCcmClusterStart(CcmLocalConfigDir, "2.0.6");

                if (output.ExitCode != 0)
                {
                    throw new TestInfrastructureException("Local ccm could not start: " + output.ToString());
                }
                var cluster = Cluster.Builder().AddContactPoint("127.0.0.1").Build();
                Session = cluster.Connect();
                Session.CreateKeyspaceIfNotExists(keyspaceName);
                Session.ChangeKeyspace(keyspaceName);
            }
        }

        [TestFixtureTearDown]
        public virtual void TestFixtureTearDown()
        {
            if (UseRemoteCcm)
            {
                CCMBridge.ReusableCCMCluster.Drop();
            }
            else
            {
                try
                {
                    //Try to close the connections
                    Session.Dispose();
                }
                catch
                {

                }
                //Remove the cluster
                TestUtils.ExecuteLocalCcmClusterRemove(CcmLocalConfigDir);
            }
        }
    }
}
