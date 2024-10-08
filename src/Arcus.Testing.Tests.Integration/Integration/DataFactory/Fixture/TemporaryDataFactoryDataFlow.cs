﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Storage.Configuration;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.DataFactory.Models;
using Azure.ResourceManager.DataFactory;
using Microsoft.Extensions.Logging;
using Azure.Core.Expressions.DataFactory;

namespace Arcus.Testing.Tests.Integration.Integration.DataFactory.Fixture
{
    /// <summary>
    /// Represents the available data types the DataFlow supports in Azure DataFactory.
    /// </summary>
    public enum DataFlowDataType { Csv, Json }

    /// <summary>
    /// Represents the available JSON settings in the source of the DataFlow.
    /// </summary>
    public enum JsonDocForm { SingleDoc, ArrayOfDocs }

    /// <summary>
    /// Represents a test fixture that temporary creates a DataFlow instance on an Azure DataFactory resource.
    /// </summary>
    public class TemporaryDataFactoryDataFlow : IAsyncDisposable
    {
        private readonly string _sinkDatasetName, _linkedServiceName;
        private readonly TestConfig _config;
        private readonly ArmClient _arm;
        private readonly DataFlowDataType _dataType;
        private readonly ILogger _logger;

        private TemporaryBlobContainer _sourceContainer;
        private DataFactoryLinkedServiceResource _linkedService;
        private DataFactoryDatasetResource _sourceDataset, _sinkDataset;
        private DataFactoryDataFlowResource _dataFlow;

        private TemporaryDataFactoryDataFlow(DataFlowDataType dataType, TestConfig config, ILogger logger)
        {
            _dataType = dataType;
            _sinkDatasetName = RandomizeWith("sink");
            _linkedServiceName = RandomizeWith("storage");
            
            _arm = new ArmClient(new DefaultAzureCredential());
            _config = config;
            _logger = logger;

            Name = RandomizeWith("dataFlow");
            SourceName = RandomizeWith("source");
        }

        private string SubscriptionId => _config["Arcus:SubscriptionId"];
        private string ResourceGroupName => _config["Arcus:ResourceGroup:Name"];
        private DataFactoryConfig DataFactory => _config.GetDataFactory();
        private StorageAccount StorageAccount => _config.GetStorageAccount();
        private string SourceName { get; }

        /// <summary>
        /// Gets the unique name of the temporary DataFlow in Azure DataFactory.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the unique name of the sink of the temporary DataFlow in Azure DataFactory.
        /// </summary>
        public string SinkName { get; } = "dataflowsink";

        /// <summary>
        /// Creates a DataFlow with a CSV source and sink on an Azure DataFactory resource.
        /// </summary>
        public static async Task<TemporaryDataFactoryDataFlow> CreateWithCsvSinkSourceAsync(TestConfig config, ILogger logger, Action<AssertCsvOptions> configureOptions)
        {
            var options = new AssertCsvOptions();
            configureOptions?.Invoke(options);

            var temp = new TemporaryDataFactoryDataFlow(DataFlowDataType.Csv, config, logger);
            try
            {
                await temp.AddSourceBlobContainerAsync();
                await temp.AddLinkedServiceAsync();
                await temp.AddCsvSourceAsync(options);
                await temp.AddCsvSinkAsync(options);
                await temp.AddDataFlowAsync();
            }
            catch
            {
                await temp.DisposeAsync();
                throw;
            }

            return temp;
        }

        /// <summary>
        /// Creates a DataFlow with a JSON source and sink on an Azure DataFactory resource.
        /// </summary>
        public static async Task<TemporaryDataFactoryDataFlow> CreateWithJsonSinkSourceAsync(JsonDocForm docForm, TestConfig config, ILogger logger)
        {
            var temp = new TemporaryDataFactoryDataFlow(DataFlowDataType.Json, config, logger);
            try
            {
                await temp.AddSourceBlobContainerAsync();
                await temp.AddLinkedServiceAsync();
                await temp.AddJsonSourceAsync();
                await temp.AddJsonSinkAsync();
                await temp.AddDataFlowAsync(docForm);
            }
            catch
            {
                await temp.DisposeAsync();
                throw;
            }

            return temp;
        }

        private async Task AddSourceBlobContainerAsync()
        {
            _sourceContainer = await TemporaryBlobContainer.CreateIfNotExistsAsync(StorageAccount.Name, containerName: RandomizeWith("adf"), _logger);
        }

        private static string RandomizeWith(string label)
        {
            return label + Guid.NewGuid().ToString()[..5];
        }

        private async Task AddLinkedServiceAsync()
        {
            _logger.LogTrace("Adding Azure Blob storage linked service '{LinkedServiceName}' to Azure DataFactory '{DataFactoryName}'", _linkedServiceName, DataFactory.Name);

            ResourceIdentifier resourceId = DataFactoryLinkedServiceResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName, DataFactory.Name, _linkedServiceName);
            _linkedService = _arm.GetDataFactoryLinkedServiceResource(resourceId);

            await _linkedService.UpdateAsync(WaitUntil.Completed, new DataFactoryLinkedServiceData(new AzureBlobStorageLinkedService
            {
                AuthenticationType = AzureStorageAuthenticationType.AccountKey,
                ConnectionString = StorageAccount.ConnectionString,
            }));
        }

        private async Task AddCsvSourceAsync(AssertCsvOptions options)
        {
            _logger.LogTrace("Adding CSV source '{SourceName}' to DataFlow '{DataFlowName}' within Azure DataFactory '{DataFactoryName}'", SourceName, Name, DataFactory.Name);

            var blobStorageLinkedService = new DataFactoryLinkedServiceReference(DataFactoryLinkedServiceReferenceKind.LinkedServiceReference, _linkedServiceName);

            _sourceDataset = _arm.GetDataFactoryDatasetResource(DataFactoryDatasetResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName, DataFactory.Name, SourceName));
            var sourceProperties = new DelimitedTextDataset(blobStorageLinkedService)
            {
                ColumnDelimiter = options.Separator.ToString(),
                RowDelimiter = options.NewLine,
                QuoteChar = options.Quote.ToString(),
                FirstRowAsHeader = options.Header is AssertCsvHeader.Present,
                DataLocation = new AzureBlobStorageLocation()
                {
                    Container = _sourceContainer?.Name ?? throw new InvalidOperationException("Azure blob storage container should be available at this point"),
                    FolderPath = SourceName
                }
            };
            await _sourceDataset.UpdateAsync(WaitUntil.Completed, new DataFactoryDatasetData(sourceProperties));
        }

        private async Task AddJsonSourceAsync()
        {
            _logger.LogTrace("Adding JSON source '{SourceName}' to DataFlow '{DataFlowName}' within Azure DataFactory '{DataFactoryName}'", SourceName, Name, DataFactory.Name);

            var blobStorageLinkedService = new DataFactoryLinkedServiceReference(DataFactoryLinkedServiceReferenceKind.LinkedServiceReference, _linkedServiceName);

            _sourceDataset = _arm.GetDataFactoryDatasetResource(DataFactoryDatasetResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName, DataFactory.Name, SourceName));

            var sourceProperties = new JsonDataset(blobStorageLinkedService)
            {
                DataLocation = new AzureBlobStorageLocation()
                {
                    Container = _sourceContainer?.Name ?? throw new InvalidOperationException("Azure blob storage container should be available at this point"),
                    FolderPath = SourceName
                }
            };
            await _sourceDataset.UpdateAsync(WaitUntil.Completed, new DataFactoryDatasetData(sourceProperties));
        }

        private async Task AddCsvSinkAsync(AssertCsvOptions options)
        {
            _logger.LogTrace("Adding CSV sink '{SinkName}' to DataFlow '{DataFlowName}' within Azure DataFactory '{DataFactoryName}'", _sinkDatasetName, Name, DataFactory.Name);

            var blobStorageLinkedService = new DataFactoryLinkedServiceReference(DataFactoryLinkedServiceReferenceKind.LinkedServiceReference, _linkedServiceName);

            _sinkDataset = _arm.GetDataFactoryDatasetResource(DataFactoryDatasetResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName, DataFactory.Name, _sinkDatasetName));
            var sinkProperties = new DelimitedTextDataset(blobStorageLinkedService)
            {
                ColumnDelimiter = options.Separator.ToString(),
                RowDelimiter = options.NewLine,
                QuoteChar = options.Quote.ToString(),
                FirstRowAsHeader = options.Header is AssertCsvHeader.Present,
                DataLocation = new AzureBlobStorageLocation
                {
                    Container = _sourceContainer?.Name ?? throw new InvalidOperationException("Azure blob storage container should be available at this point"),
                    FolderPath = _sinkDatasetName
                }
            };

            await _sinkDataset.UpdateAsync(WaitUntil.Completed, new DataFactoryDatasetData(sinkProperties));
        }

        private async Task AddJsonSinkAsync()
        {
            _logger.LogTrace("Adding JSON sink '{SinkName}' to DataFlow '{DataFlowName}' within Azure DataFactory '{DataFactoryName}'", _sinkDatasetName, Name, DataFactory.Name);

            var blobStorageLinkedService = new DataFactoryLinkedServiceReference(DataFactoryLinkedServiceReferenceKind.LinkedServiceReference, _linkedServiceName);

            _sinkDataset = _arm.GetDataFactoryDatasetResource(DataFactoryDatasetResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName, DataFactory.Name, _sinkDatasetName));
            
            var sinkProperties = new JsonDataset(blobStorageLinkedService)
            {
                DataLocation = new AzureBlobStorageLocation
                {
                    Container = _sourceContainer?.Name ?? throw new InvalidOperationException("Azure blob storage container should be available at this point"),
                    FolderPath = _sinkDatasetName
                }
            };
            await _sinkDataset.UpdateAsync(WaitUntil.Completed, new DataFactoryDatasetData(sinkProperties));
        }

        private async Task AddDataFlowAsync(JsonDocForm docForm = JsonDocForm.SingleDoc)
        {
            _logger.LogTrace("Adding DataFlow '{DataFlowName}' to Azure DataFactory '{DataFactoryName}'", Name, DataFactory.Name);

            ResourceIdentifier dataFlowResourceId = DataFactoryDataFlowResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName, DataFactory.Name, Name);
            _dataFlow = _arm.GetDataFactoryDataFlowResource(dataFlowResourceId);

            var sourceName = "dataflowsource";
            var properties = new DataFactoryMappingDataFlowProperties
            {
                Sources =
                {
                    new DataFlowSource(sourceName)
                    {
                        Dataset = new DatasetReference(DatasetReferenceType.DatasetReference, SourceName)
                    }
                },
                Sinks =
                {
                    new DataFlowSink(SinkName)
                    {
                        Dataset = new DatasetReference(DatasetReferenceType.DatasetReference, _sinkDatasetName)
                    }
                }
            };

            IEnumerable<string> scriptLines = _dataType switch
            {
                DataFlowDataType.Csv => DataFlowCsvScriptLines(sourceName, SinkName),
                DataFlowDataType.Json => DataFlowJsonScriptLines(sourceName, SinkName, docForm),
                _ => throw new ArgumentOutOfRangeException()
            };

            foreach (string line in scriptLines)
            {
                properties.ScriptLines.Add(line);
            }

            await _dataFlow.UpdateAsync(WaitUntil.Completed, new DataFactoryDataFlowData(properties));
        }

        private static IEnumerable<string> DataFlowCsvScriptLines(string sourceName, string sinkName)
        {
            return new[]
            {
                "source(allowSchemaDrift: true,",
                "     validateSchema: false,",
                $"     ignoreNoFilesFound: false) ~> {sourceName}",
                $"{sourceName} sink(allowSchemaDrift: true,",
                "     validateSchema: false,",
                "     skipDuplicateMapInputs: true,",
                $"     skipDuplicateMapOutputs: true) ~> {sinkName}"
            };
        }

        private static IEnumerable<string> DataFlowJsonScriptLines(string sourceName, string sinkName, JsonDocForm docForm)
        {
            string documentForm = docForm switch
            {
                JsonDocForm.SingleDoc => "singleDocument",
                JsonDocForm.ArrayOfDocs => "arrayOfDocuments",
                _ => throw new ArgumentOutOfRangeException(nameof(docForm), docForm, null)
            };

            return new[]
            {
                "source(allowSchemaDrift: true,",
                "     validateSchema: false,",
                "     ignoreNoFilesFound: false,",
                $"    documentForm: '{documentForm}') ~> {sourceName}",
                $"{sourceName} sink(allowSchemaDrift: true,",
                "     validateSchema: false,",
                "     skipDuplicateMapInputs: true,",
                $"    skipDuplicateMapOutputs: true) ~> {sinkName}"
            };
        }

        /// <summary>
        /// Uploads a file to the source of the temporary DataFlow.
        /// </summary>
        public async Task UploadToSourceAsync(string expected)
        {
            string fileExtension = _dataType switch
            {
                DataFlowDataType.Csv => ".csv",
                DataFlowDataType.Json => ".json",
                _ => throw new ArgumentOutOfRangeException()
            };

            _logger.LogTrace("Upload {FileType} file to DataFlow source: {FileContents}", _dataType, expected);
            await _sourceContainer.UploadBlobAsync(
                Path.Combine(SourceName, RandomizeWith("input") + fileExtension), 
                BinaryData.FromString(expected));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);
            if (_sourceContainer != null)
            {
                disposables.Add(_sourceContainer);
            }

            if (_dataFlow != null)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    _logger.LogTrace("Deleting DataFlow '{DataFlowName}' from Azure DataFactory '{DataFactoryName}'", Name, DataFactory.Name);
                    await _dataFlow.DeleteAsync(WaitUntil.Completed);
                }));
            }

            if (_sourceDataset != null)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    _logger.LogTrace("Deleting CSV source '{SourceName}' from Azure DataFactory '{DataFactoryName}'", SourceName, DataFactory.Name);
                    await _sourceDataset.DeleteAsync(WaitUntil.Completed);
                }));
            }

            if (_sinkDataset != null)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    _logger.LogTrace("Deleting CSV sink '{SinkName}' from Azure DataFactory '{DataFactoryName}'", _sinkDatasetName, DataFactory.Name);
                    await _sinkDataset.DeleteAsync(WaitUntil.Completed);
                }));
            }

            if (_linkedService != null)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    _logger.LogTrace("Deleting Azure Blob storage linked service '{LinkedServiceName}' from Azure DataFactory '{DataFactoryName}'", _linkedServiceName, DataFactory.Name);
                    await _linkedService.DeleteAsync(WaitUntil.Completed);
                }));
            }
        }
    }
}
