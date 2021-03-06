﻿// Copyright 2017 Google Inc. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#region

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Google.Cloud.Spanner.Data.IntegrationTests
{
    [Collection(nameof(TestDatabaseFixture))]
    public class AdminTests
    {
        // ReSharper disable once UnusedParameter.Local
        public AdminTests(TestDatabaseFixture testFixture, ITestOutputHelper outputHelper)
        {
            _testFixture = testFixture;
            TestLogger.TestOutputHelper = outputHelper;
        }

        private readonly TestDatabaseFixture _testFixture;

        [Fact]
        public async Task CreateDrop()
        {
            string dbName = "t_" + Guid.NewGuid().ToString("N").Substring(0, 28);

            using (var connection = new SpannerConnection(_testFixture.NoDbConnectionString))
            {
                var createCmd = connection.CreateDdlCommand($"CREATE DATABASE {dbName}");
                await createCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            using (var connection = new SpannerConnection(_testFixture.NoDbConnectionString))
            {
                var dropCommand = connection.CreateDdlCommand($"DROP DATABASE {dbName}");
                await dropCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task CreateWithExtrasDrop()
        {
            string dbName = "t_" + Guid.NewGuid().ToString("N").Substring(0, 28);

            using (var connection = new SpannerConnection(_testFixture.NoDbConnectionString))
            {
                const string tableCreate = @"CREATE TABLE TX (
                              K                   STRING(MAX) NOT NULL,
                              StringValue         STRING(MAX),
                            ) PRIMARY KEY (K)";

                var createCmd = connection.CreateDdlCommand(
                    $"CREATE DATABASE {dbName}",
                    tableCreate);

                await createCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            using (var connection = new SpannerConnection($"{_testFixture.NoDbConnectionString}/databases/{dbName}"))
            {
                var cmd = connection.CreateInsertCommand("TX", new SpannerParameterCollection
                {
                    {"K", SpannerDbType.String, "key" },
                    {"StringValue", SpannerDbType.String, "value"}
                });
                await cmd.ExecuteNonQueryAsync();
                cmd = connection.CreateSelectCommand("SELECT * FROM TX");
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    Assert.Equal("key", reader.GetFieldValue<string>("K"));
                    Assert.Equal("value", reader.GetFieldValue<string>("StringValue"));
                }
            }

            using (var connection = new SpannerConnection(_testFixture.NoDbConnectionString))
            {
                var dropCommand = connection.CreateDdlCommand($"DROP DATABASE {dbName}");
                await dropCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task CreateWithExtrasDrop2()
        {
            string dbName = "t_" + Guid.NewGuid().ToString("N").Substring(0, 28);

            using (var connection = new SpannerConnection(_testFixture.NoDbConnectionString))
            {
                var createCmd = connection.CreateDdlCommand(
                    $"CREATE DATABASE {dbName}");

                await createCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            using (var connection = new SpannerConnection($"{_testFixture.NoDbConnectionString}/databases/{dbName}"))
            {
                const string tableCreate1 = @"CREATE TABLE TX1 (
                              K                   STRING(MAX) NOT NULL,
                              StringValue         STRING(MAX),
                            ) PRIMARY KEY (K)";
                const string tableCreate2 = @"CREATE TABLE TX2 (
                              K                   STRING(MAX) NOT NULL,
                              StringValue         STRING(MAX),
                            ) PRIMARY KEY (K)";
                var updateCmd = connection.CreateDdlCommand(tableCreate1, tableCreate2);
                await updateCmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                var cmd = connection.CreateInsertCommand("TX2", new SpannerParameterCollection
                {
                    {"K", SpannerDbType.String, "key" },
                    {"StringValue", SpannerDbType.String, "value"}
                });
                await cmd.ExecuteNonQueryAsync();
                cmd = connection.CreateSelectCommand("SELECT * FROM TX2");
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    Assert.Equal("key", reader.GetFieldValue<string>("K"));
                    Assert.Equal("value", reader.GetFieldValue<string>("StringValue"));
                }
            }

            using (var connection = new SpannerConnection(_testFixture.NoDbConnectionString))
            {
                var dropCommand = connection.CreateDdlCommand($"DROP DATABASE {dbName}");
                await dropCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task DropDoesntSupportExtra()
        {
            string dbName = "t_" + Guid.NewGuid().ToString("N").Substring(0, 28);
            using (var connection = new SpannerConnection(_testFixture.NoDbConnectionString))
            {
                var createCmd = connection.CreateDdlCommand(
                    $"CREATE DATABASE {dbName}");
                await createCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            using (var connection = new SpannerConnection(_testFixture.NoDbConnectionString))
            {
                const string tableCreate1 = @"CREATE TABLE TX1 (
                              K                   STRING(MAX) NOT NULL,
                              StringValue         STRING(MAX),
                            ) PRIMARY KEY (K)";

                var dropCommand = connection.CreateDdlCommand($"DROP DATABASE {dbName}", tableCreate1);
                await Assert.ThrowsAsync<InvalidOperationException>(() => dropCommand.ExecuteNonQueryAsync());
                dropCommand = connection.CreateDdlCommand($"DROP DATABASE {dbName}");
                await dropCommand.ExecuteNonQueryAsync();
            }
        }
    }
}
