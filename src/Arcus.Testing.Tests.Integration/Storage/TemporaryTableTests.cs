﻿using System;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Storage.Fixture;
using Azure.Data.Tables;
using Xunit;

namespace Arcus.Testing.Tests.Integration.Storage
{
    public class TemporaryTableTests : IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryTableTests" /> class.
        /// </summary>
        public TemporaryTableTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        [Fact]
        public async Task CreateTempTable_OnNonExistingTable_SucceedsByExistingDuringLifetime()
        {
            // Arrange
            await using TableStorageTestContext context = await GivenTableStorageAsync();

            TableClient client = context.WhenTableUnavailable();

            TemporaryTable temp = await CreateTempTableAsync(client);
            await context.ShouldStoreTableAsync(client);

            // Act
            await temp.DisposeAsync();

            // Assert
            await context.ShouldNotStoreTableAsync(client);
        }

        [Fact]
        public async Task CreateTempTable_OnExistingTable_SucceedsByLeavingAfterLifetime()
        {
            // Arrange
            await using TableStorageTestContext context = await GivenTableStorageAsync();

            TableClient client = await context.WhenTableAvailableAsync();
            TableEntity createdBefore = await context.WhenTableEntityAvailableAsync(client);

            TemporaryTable temp = await CreateTempTableAsync(client);
            await context.ShouldStoreTableAsync(client);
            await context.ShouldStoreTableEntityAsync(client, createdBefore);

            TableEntity createdAfter = await context.WhenTableEntityAvailableAsync(client);
            TableEntity createdByUs = await AddTableEntityAsync(context, temp);

            // Act
            await temp.DisposeAsync();

            // Assert
            await context.ShouldStoreTableAsync(client);
            await context.ShouldStoreTableEntityAsync(client, createdBefore);
            await context.ShouldStoreTableEntityAsync(client, createdAfter);
            await context.ShouldNotStoreTableEntityAsync(client, createdByUs);
        }

        [Fact]
        public async Task CreateTempTable_OnNonExistingTableWhenTableIsDeletedOutsideFixture_SucceedsByIgnoringAlreadyDeletedTable()
        {
            // Arrange
            await using TableStorageTestContext context = await GivenTableStorageAsync();

            TableClient client = context.WhenTableUnavailable();
            TemporaryTable temp = await CreateTempTableAsync(client);
            await context.WhenTableDeletedAsync(client);

            // Act
            await temp.DisposeAsync();

            // Assert
            await context.ShouldNotStoreTableAsync(client);
        }

        [Fact]
        public async Task CreateTempTableWithSetupCleanAll_OnExistingEntityBeforeFixtureCreation_SucceedsByRemovingEntityUponCreation()
        {
            // Arrange
            await using TableStorageTestContext context = await GivenTableStorageAsync();

            TableClient client = await context.WhenTableAvailableAsync();
            TableEntity createdBefore = await context.WhenTableEntityAvailableAsync(client);

            // Act
            _ = await CreateTempTableAsync(client, options =>
            {
                options.OnSetup.CleanAllEntities();
            });

            // Assert
            await context.ShouldNotStoreTableEntityAsync(client, createdBefore);
        }

        [Fact]
        public async Task CreateTempTableWithTeardownCleanAll_OnExistingEntityAfterFixtureCreation_SucceedsByRemovingEntityUponFixtureDeletion()
        {
            // Arrange
            await using TableStorageTestContext context = await GivenTableStorageAsync();

            TableClient client = await context.WhenTableAvailableAsync();
            TableEntity createdBefore = await context.WhenTableEntityAvailableAsync(client);

            TemporaryTable temp = await CreateTempTableAsync(client, options =>
            {
                options.OnTeardown.CleanAllEntities();
            });
            await context.ShouldStoreTableEntityAsync(client, createdBefore);
            TableEntity createdAfter = await context.WhenTableEntityAvailableAsync(client);
            TableEntity createdByUs = await AddTableEntityAsync(context, temp);

            // Act
            await temp.DisposeAsync();

            // Assert
            await context.ShouldNotStoreTableEntityAsync(client, createdBefore);
            await context.ShouldNotStoreTableEntityAsync(client, createdAfter);
            await context.ShouldNotStoreTableEntityAsync(client, createdByUs);
        }

        [Fact]
        public async Task CreateTempTableWithSetupCleanMatched_OnExistingEntityBeforeFixtureCreation_SucceedsByRemovingOnlyMatchedEntityUponCreation()
        {
            // Arrange
            await using TableStorageTestContext context = await GivenTableStorageAsync();

            TableClient client = await context.WhenTableAvailableAsync();
            TableEntity matchedEntity = await context.WhenTableEntityAvailableAsync(client);
            TableEntity notMatchedEntity = await context.WhenTableEntityAvailableAsync(client);

            // Act
            TemporaryTable temp = await CreateTempTableAsync(client, options =>
            {
                options.OnSetup.CleanMatchingEntities(entity => entity.RowKey == matchedEntity.RowKey);
            });

            // Assert
            await context.ShouldNotStoreTableEntityAsync(client, matchedEntity);
            await context.ShouldStoreTableEntityAsync(client, notMatchedEntity);
            await client.AddEntityAsync(matchedEntity, TestContext.Current.CancellationToken);

            await temp.DisposeAsync();
            await context.ShouldStoreTableEntityAsync(client, matchedEntity);
            await context.ShouldStoreTableEntityAsync(client, notMatchedEntity);
        }

        [Fact]
        public async Task CreateTempTableWithTeardownCleanMatched_OnMatchingEntity_SucceedsByRemovingOnlyMatchedEntityUponDeletion()
        {
            // Arrange
            await using TableStorageTestContext context = await GivenTableStorageAsync();

            TableClient client = await context.WhenTableAvailableAsync();
            TableEntity matchedEntity = await context.WhenTableEntityAvailableAsync(client);
            TableEntity notMatchedEntity = await context.WhenTableEntityAvailableAsync(client);

            // Act
            TemporaryTable temp = await CreateTempTableAsync(client, options =>
            {
                options.OnTeardown.CleanMatchingEntities(entity => entity.RowKey == matchedEntity.RowKey);
            });

            // Assert
            await context.ShouldStoreTableEntityAsync(client, matchedEntity);
            await context.ShouldStoreTableEntityAsync(client, notMatchedEntity);

            await temp.DisposeAsync();

            await context.ShouldNotStoreTableEntityAsync(client, matchedEntity);
            await context.ShouldStoreTableEntityAsync(client, notMatchedEntity);
        }

        [Fact]
        public async Task CreateTempTableWithSetupTeardownOptions_OnMatchingEntity_SucceedsByCombining()
        {
            // Arrange
            await using TableStorageTestContext context = await GivenTableStorageAsync();

            TableClient client = await context.WhenTableAvailableAsync();

            TableEntity createdBefore = await context.WhenTableEntityAvailableAsync(client);
            TemporaryTable temp = await CreateTempTableAsync(client, options =>
            {
                options.OnSetup.CleanAllEntities();
                options.OnTeardown.CleanAllEntities();
            });
            await context.ShouldNotStoreTableEntityAsync(client, createdBefore);

            TableEntity createdByUs = await AddTableEntityAsync(context, temp);
            TableEntity matched = await context.WhenTableEntityAvailableAsync(client);
            TableEntity notMatched = await context.WhenTableEntityAvailableAsync(client);
            temp.OnTeardown.CleanMatchingEntities(entity => entity.RowKey == matched.RowKey);

            // Act
            await temp.DisposeAsync();

            // Assert
            await context.ShouldNotStoreTableEntityAsync(client, matched);
            await context.ShouldNotStoreTableEntityAsync(client, createdByUs);
            await context.ShouldStoreTableEntityAsync(client, notMatched);
        }

        private async Task<TemporaryTable> CreateTempTableAsync(TableClient client, Action<TemporaryTableOptions> configureOptions = null)
        {
            var temp = configureOptions is null
                ? await TemporaryTable.CreateIfNotExistsAsync(client.AccountName, client.Name, Logger)
                : await TemporaryTable.CreateIfNotExistsAsync(client.AccountName, client.Name, Logger, configureOptions);

            Assert.Equal(client.Name, temp.Name);
            Assert.Equal(client.AccountName, temp.Client.AccountName);

            return temp;
        }

        private static async Task<TableEntity> AddTableEntityAsync(TableStorageTestContext context, TemporaryTable table)
        {
            TableEntity entity = context.WhenTableEntityUnavailable();
#pragma warning disable CS0618 // Type or member is obsolete: currently still testing deprecated functionality.
            await table.AddEntityAsync(entity);
#pragma warning restore CS0618 // Type or member is obsolete

            return entity;
        }

        private async Task<TableStorageTestContext> GivenTableStorageAsync()
        {
            return await TableStorageTestContext.GivenAsync(Configuration, Logger);
        }
    }
}
