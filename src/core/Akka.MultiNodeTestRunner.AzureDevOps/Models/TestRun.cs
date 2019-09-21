﻿namespace Akka.MultiNodeTestRunner.AzureDevOps.Models
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using static XmlHelper;

    public class TestRun : ITestEntity
    {
        public TestRun(string name)
        {
            Name = name;
        }

        public Identifier Id { get; } = Identifier.Create();
        public string Name { get; }
        public string RunUser { get; set; }
        public Times Times { get; } = new Times();
        public TestList TestList { get; } = new TestList("Results Not in a List");
        public IList<UnitTest> UnitTests { get; } = new List<UnitTest>();
        public Output Output { get; set; }

        public void Log(string item)
        {
            if (Output == null)
            {
                Output = new Output();
            }

            Output.TextMessages.Add(item);
        }

        public UnitTest AddUnitTest(string className, string name, string storage)
        {
            var unitTest = new UnitTest(className, name, TestList.Id, storage);
            UnitTests.Add(unitTest);

            return unitTest;
        }

        public XElement Serialize()
        {
            return Elem("TestRun",
                Attr("id", Id),
                Attr("name", Name),
                Attr("runUser", RunUser),
                Times,
                // TestSettings
                ElemList("Results", UnitTests.SelectMany(x => x.Results)),
                ElemList("TestDefinitions", UnitTests),
                ElemList("TestEntries", UnitTests.Select(x => new TestEntry(x.Id, x.ExecutionId, x.TestListId))),
                Elem("TestLists",
                    TestList
                ),
                new ResultSummary(UnitTests, Output)
            );
        }
    }
}