﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Metrics;
using Akka.Cluster.Routing;
using Akka.Configuration;
using Samples.Cluster.Metrics.Common;

namespace Samples.Cluster.AdaptiveGroup
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = ConfigurationFactory.ParseString(await File.ReadAllTextAsync("Application.conf"));

            var system = ActorSystem.Create("ClusterSystem", config);

            var cluster = Akka.Cluster.Cluster.Get(system);
            cluster.RegisterOnMemberUp(() =>
            {
                var paths = new List<string>
                {
                    "/user/factorialBackend-1",
                    "/user/factorialBackend-2",
                    "/user/factorialBackend-3",
                    "/user/factorialBackend-4",
                    "/user/factorialBackend-5",
                    "/user/factorialBackend-6"
                };

                system.ActorOf(
                    new ClusterRouterGroup(
                            local: new AdaptiveLoadBalancingGroup(MixMetricsSelector.Instance),
                            settings: new ClusterRouterGroupSettings(
                                10,
                                ImmutableHashSet.Create(paths.ToArray()),
                                allowLocalRoutees: true,
                                useRole: "backend"))
                        .Props(), "factorialBackendRouter");

                // system.ActorOf(FromConfig.Instance.Props(), name: "factorialBackendRouter");

                system.ActorOf(Props.Create<FactorialBackend>(), "factorialBackend-1");
                system.ActorOf(Props.Create<FactorialBackend>(), "factorialBackend-2");
                system.ActorOf(Props.Create<FactorialBackend>(), "factorialBackend-3");
                system.ActorOf(Props.Create<FactorialBackend>(), "factorialBackend-4");
                system.ActorOf(Props.Create<FactorialBackend>(), "factorialBackend-5");
                system.ActorOf(Props.Create<FactorialBackend>(), "factorialBackend-6");
            });

            Console.ReadKey();

            await system.Terminate();
        }
    }
}
