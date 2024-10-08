﻿using System;
using System.Collections.Generic;
using Wikiled.Redis.Information;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Wikiled.Redis.UnitTests.Information
{
    [TestFixture]
    public class StatsInfoTests
    {
        private Dictionary<string, string> allValues;

        [SetUp]
        public void Setup()
        {
            allValues = new Dictionary<string, string>();
            allValues["total_commands_processed"] = "100";
        }

        [Test]
        public void Construct()
        {
            var server = InfoTestsHelper.Create("Stats", allValues);
            ClassicAssert.Throws<ArgumentNullException>(() => new StatsInfo(null));
            StatsInfo info = new StatsInfo(server);
            ClassicAssert.AreEqual(100, info.TotalCommands);
        }

        [Test]
        public void ConstructMissing()
        {
            allValues.Clear();
            var server = InfoTestsHelper.Create("Stats", allValues);
            StatsInfo info = new StatsInfo(server);
            ClassicAssert.IsNull(info.TotalCommands);
        }
    }
}
