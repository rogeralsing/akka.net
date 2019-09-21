﻿// -----------------------------------------------------------------------
//  <copyright file="TestEntry.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2019 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2019 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

namespace Akka.MultiNodeTestRunner.AzureDevOps.Models
{
    using System.Xml.Linq;

    public class TestEntry : ITestEntity
    {
        public TestEntry(Identifier testId, Identifier executionId, Identifier testListId)
        {
            TestId = testId;
            ExecutionId = executionId;
            TestListId = testListId;
        }

        public Identifier TestId { get; }
        public Identifier ExecutionId { get; }
        public Identifier TestListId { get; }

        public XElement Serialize() => XmlHelper.Elem("TestEntry",
            XmlHelper.Attr("testId", TestId),
            XmlHelper.Attr("executionId", ExecutionId),
            XmlHelper.Attr("testListId", TestListId)
        );
    }
}