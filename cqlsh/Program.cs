﻿// cassandra-sharp - a .NET client for Apache Cassandra
// Copyright (c) 2011-2012 Pierre Chalamet
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace cqlsh
{
    using System;
    using CassandraSharp;
    using CassandraSharp.Config;
    using cqlsh.Commands;
    using cqlsh.StatementReader;

    internal class Program
    {
        private static CliArgs _cliArgs;

        private static int Main(string[] args)
        {
            _cliArgs = new CliArgs();
            if (!CommandLineParser.ParseArguments(args, _cliArgs))
            {
                string usage = CommandLineParser.ArgumentsUsage(typeof(CliArgs));
                Console.WriteLine(usage);
                return 5;
            }

            string hostname = _cliArgs.Hostname;
            IStatementReader statementInput = new ConsoleInput(hostname);
            if (null != _cliArgs.File)
            {
                statementInput = new FileInput(_cliArgs.File);
            }

            try
            {
                var statementReader = new StatementSplitter(statementInput);
                Run(hostname, statementReader);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed with error {0}", ex);
                return 5;
            }

            return 0;
        }

        private static void Run(string hostname, IStatementReader statementReader)
        {
            CassandraSharpConfig cassandraSharpConfig = new CassandraSharpConfig();
            cassandraSharpConfig.Logger = typeof(ConsoleDebugLogger).AssemblyQualifiedName;
            ClusterManager.Configure(cassandraSharpConfig);

            ClusterConfig clusterConfig = new ClusterConfig
                {
                        Transport = new TransportConfig
                            {
                                    User = _cliArgs.User,
                                    Password = _cliArgs.Password,
                            },
                        Endpoints = new EndpointsConfig
                            {
                                    Servers = new[] {hostname},
                            }
                };

            using (ICluster cluster = ClusterManager.GetCluster(clusterConfig))
            {
                CommandContext.Cluster = cluster;

                if (_cliArgs.CheckConnection)
                {
                    Console.WriteLine("Connecting to {0}:", hostname);
                    const string checkStatement = "select cluster_name, data_center, rack, release_version from system.local";
                    new Exec {Statement = checkStatement}.Execute();
                    if (CommandContext.LastCommandFailed)
                    {
                        return;
                    }
                }

                if (_cliArgs.Banner)
                {
                    new Exec {Statement = "!help"}.Execute();
                }

                foreach (string statement in statementReader.Read())
                {
                    new Exec {Statement = statement}.Execute();
                    if (CommandContext.Exit)
                    {
                        return;
                    }
                }
            }
        }
    }
}