﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Configuration;
using Arcus.Testing.Tests.Integration.Fixture;
using Arcus.Testing.Tests.Integration.Messaging.Configuration;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Sdk;

namespace Arcus.Testing.Tests.Integration.Messaging.Fixture
{
    /// <summary>
    /// Represents test-friendly operations to interact with the Azure Service bus resource.
    /// </summary>
    internal class ServiceBusTestContext : IAsyncDisposable
    {
        private readonly TemporaryManagedIdentityConnection _connection;
        private readonly ServiceBusAdministrationClient _adminClient;
        private readonly ServiceBusClient _messagingClient;
        private readonly Collection<string> _topicNames = new(), _queueNames = new();
        private readonly Collection<(string topicName, string subscriptionName)> _subscriptionNames = new();
        private readonly ILogger _logger;

        private static readonly Faker Bogus = new();

        private ServiceBusTestContext(
            TemporaryManagedIdentityConnection connection,
            ServiceBusAdministrationClient adminClient,
            ServiceBusClient messagingClient,
            ILogger logger)
        {
            _connection = connection;
            _adminClient = adminClient;
            _messagingClient = messagingClient;
            _logger = logger;
        }

        /// <summary>
        /// Creates a <see cref="ServiceBusTestContext"/> instance that tracks interactions with Azure Service bus.
        /// </summary>
        public static ServiceBusTestContext Given(TestConfig config, ILogger logger)
        {
            ServicePrincipal servicePrincipal = config.GetServicePrincipal();
            ServiceBusNamespace serviceBus = config.GetServiceBus();

            var connection = TemporaryManagedIdentityConnection.Create(servicePrincipal);
            var credential = new DefaultAzureCredential();
            var adminClient = new ServiceBusAdministrationClient(serviceBus.HostName, credential);
            var messagingClient = new ServiceBusClient(serviceBus.HostName, credential);

            return new ServiceBusTestContext(connection, adminClient, messagingClient, logger);
        }

        /// <summary>
        /// Provides an Azure Service bus queue that is available remotely.
        /// </summary>
        /// <returns>The name of the available queue.</returns>
        public async Task<string> WhenQueueAvailableAsync()
        {
            string queueName = WhenQueueUnavailable();
            _logger.LogTrace("[Test:Setup] Create available Azure Service Bus queue '{QueueName}'", queueName);

            await _adminClient.CreateQueueAsync(queueName);
            return queueName;
        }

        /// <summary>
        /// Provides an Azure Service bus queue that is not available remotely.
        /// </summary>
        /// <returns>The name of the unavailable queue.</returns>
        public string WhenQueueUnavailable()
        {
            string queueName = $"queue-{Guid.NewGuid()}";
            _queueNames.Add(queueName);
            
            return queueName;
        }

        /// <summary>
        /// Provides an Azure Service bus topic that is available remotely.
        /// </summary>
        /// <returns>The name of the available topic.</returns>
        public async Task<string> WhenTopicAvailableAsync()
        {
            string topicName = WhenTopicUnavailable();
            _logger.LogTrace("[Test:Setup] Create available Azure Service Bus topic '{TopicName}'", topicName);

            await _adminClient.CreateTopicAsync(topicName);
            return topicName;
        }

        /// <summary>
        /// Provides an Azure Service bus topic that is unavailable remotely.
        /// </summary>
        /// <returns>The name of the unavailable topic.</returns>
        public string WhenTopicUnavailable()
        {
            string topicName = $"topic-{Guid.NewGuid()}";
            _topicNames.Add(topicName);

            return topicName;
        }

        /// <summary>
        /// Provides an Azure Service bus topic subscription that is available remotely.
        /// </summary>
        /// <param name="topicName">The name of the topic where the subscription should be available.</param>
        /// <returns>The name of the available subscription.</returns>
        public async Task<string> WhenTopicSubscriptionAvailableAsync(string topicName)
        {
            string subscriptionName = WhenTopicSubscriptionUnavailable(topicName);
            _logger.LogTrace("[Test:Setup] Create available Azure Service Bus topic subscription '{SubscriptionName}' on topic '{TopicName}'", subscriptionName, topicName);

            await _adminClient.CreateSubscriptionAsync(topicName, subscriptionName);
            return subscriptionName;
        }

        /// <summary>
        /// Provides an Azure Service bus topic subscription rule that is available remotely.
        /// </summary>
        /// <returns>The name of the available subscription.</returns>
        public async Task<RuleProperties> WhenTopicSubscriptionRuleAvailableAsync(string topicName, string subscriptionName)
        {
            _logger.LogTrace("[Test:Setup] Create available Azure Service Bus topic subscription '{SubscriptionName}' on topic '{TopicName}'", subscriptionName, topicName);

            string ruleName = WhenTopicSubscriptionRuleUnavailable();
            var filter = Bogus.PickRandom<RuleFilter>(
                new TrueRuleFilter(),
                new FalseRuleFilter(),
                new SqlRuleFilter("1=1"),
                new CorrelationRuleFilter(Bogus.Random.Guid().ToString()));

            var action = new SqlRuleAction($"SET sys.CorrelationId = '{Bogus.Random.Guid()}';");

            return await _adminClient.CreateRuleAsync(topicName, subscriptionName, new CreateRuleOptions(ruleName, filter) { Action = action });
        }

        /// <summary>
        /// Provides an Azure Service bus topic subscription that is unavailable remotely.
        /// </summary>
        /// <param name="topicName">THe name of the topic where the subscription should be unavailable.</param>
        /// <returns>The name of the unavailable subscription.</returns>
        public string WhenTopicSubscriptionUnavailable(string topicName)
        {
            string subscriptionName = $"sub-{Guid.NewGuid()}";
            _subscriptionNames.Add((topicName, subscriptionName));

            return subscriptionName;
        }

        public string WhenTopicSubscriptionRuleUnavailable()
        {
            string ruleName = $"rule-{Bogus.Random.Guid()}";
            return ruleName;
        }

        /// <summary>
        /// Makes sure that the Azure Service bus queue is deleted.
        /// </summary>
        public async Task WhenQueueDeletedAsync(string queueName)
        {
            _logger.LogTrace("[Test:Setup] Delete available Azure Service Bus queue '{QueueName}'", queueName);
            await _adminClient.DeleteQueueAsync(queueName);
        }

        /// <summary>
        /// Makes sure that the Azure Service bus topic is deleted.
        /// </summary>
        public async Task WhenTopicDeletedAsync(string topicName)
        {
            _logger.LogTrace("[Test:Setup] Delete available Azure Service Bus topic '{TopicName}'", topicName);
            await _adminClient.DeleteTopicAsync(topicName);
        }

        /// <summary>
        /// Makes sure that the Azure Service bus subscription is deleted on the topic.
        /// </summary>
        public async Task WhenTopicSubscriptionDeletedAsync(string topicName, string subscriptionName)
        {
            _logger.LogTrace("[Test:Setup] Delete available Azure Service Bus topic subscription '{SubscriptionName}' in topic '{TopicName}'", subscriptionName, topicName);
            await _adminClient.DeleteSubscriptionAsync(topicName, subscriptionName);
        }

        public async Task<ServiceBusMessage> WhenMessageSentAsync(string entityName)
        {
            await using ServiceBusSender sender = _messagingClient.CreateSender(entityName);
            ServiceBusMessage message = WhenMessageUnsent();

            await sender.SendMessageAsync(message);
            return message;
        }

        public ServiceBusMessage WhenMessageUnsent()
        {
            var message = new ServiceBusMessage(Bogus.Random.Bytes(10))
            {
                MessageId = $"test-{Bogus.Random.Guid()}"
            };

            return message;
        }

        /// <summary>
        /// Verifies that the Service bus queue is available.
        /// </summary>
        public async Task ShouldHaveQueueAsync(string queueName)
        {
            Assert.True(await _adminClient.QueueExistsAsync(queueName), $"Azure Service Bus queue '{queueName}' should be available on the namespace, but it isn't");
        }

        /// <summary>
        /// Verifies that the Service bus queue is unavailable.
        /// </summary>
        public async Task ShouldNotHaveQueueAsync(string queueName)
        {
            Assert.False(await _adminClient.QueueExistsAsync(queueName), $"Azure Service Bus queue '{queueName}' should not be available on the namespace, but it is");
        }

        /// <summary>
        /// Verifies that the Service bus topic is available.
        /// </summary>
        public async Task ShouldHaveTopicAsync(string topicName)
        {
            Assert.True(await _adminClient.TopicExistsAsync(topicName), $"Azure Service Bus topic '{topicName}' should be available on the namespace, but it isn't");
        }

        /// <summary>
        /// Verifies that the Service bus topic is unavailable.
        /// </summary>
        public async Task ShouldNotHaveTopicAsync(string topicName)
        {
            Assert.False(await _adminClient.TopicExistsAsync(topicName), $"Azure Service Bus topic '{topicName}' should not be available on the namespace, but it is");
        }

        /// <summary>
        /// Verifies that the Service bus topic subscription is available.
        /// </summary>
        public async Task ShouldHaveTopicSubscriptionAsync(string topicName, string subscriptionName)
        {
            Assert.True(await _adminClient.SubscriptionExistsAsync(topicName, subscriptionName), $"Azure Service Bus topic '{topicName}' should have a subscription '{subscriptionName}', but it hasn't");
        }

        /// <summary>
        /// Verifies that the Service bus topic subscription is unavailable.
        /// </summary>
        public async Task ShouldNotHaveTopicSubscriptionAsync(string topicName, string subscriptionName)
        {
            Assert.False(await _adminClient.SubscriptionExistsAsync(topicName, subscriptionName), $"Azure Service Bus topic '{topicName}' should not have a subscription '{subscriptionName}', but it has");
        }

        public async Task ShouldHaveTopicSubscriptionRuleAsync(string topicName, string subscriptionName, RuleProperties rule)
        {
            await ShouldHaveTopicSubscriptionRuleAsync(topicName, subscriptionName, rule.Name, actualRule =>
            {
                Assert.Equal(rule.Filter, actualRule.Filter);
            });
        }

        public async Task ShouldHaveTopicSubscriptionRuleAsync(string topicName, string subscriptionName, string ruleName, Action<RuleProperties> assertion = null)
        {
            Assert.True(await _adminClient.RuleExistsAsync(topicName, subscriptionName, ruleName), $"Azure Service bus topic subscription '{topicName}/{subscriptionName}' should have a subscription rule '{ruleName}', but it hasn't");

            RuleProperties actualRule = await _adminClient.GetRuleAsync(topicName, subscriptionName, ruleName);
            assertion?.Invoke(actualRule);
        }

        public async Task ShouldNotHaveTopicSubscriptionRuleAsync(string topicName, string subscriptionName, string ruleName)
        {
            Assert.False(await _adminClient.RuleExistsAsync(topicName, subscriptionName, ruleName), $"Azure Service bus topic subscription '{topicName}/{subscriptionName}' should not have a subscription rule '{ruleName}', but it has");
        }

        public async Task ShouldLeaveMessageAsync(string entityName, ServiceBusMessage message)
        {
            await using ServiceBusReceiver receiver = _messagingClient.CreateReceiver(entityName);
            IEnumerable<ServiceBusReceivedMessage> messages = await receiver.PeekMessagesAsync(100);
            Assert.True(messages.Any(actual => actual.MessageId == message.MessageId), $"Azure Service bus '{entityName}' should have message '{message.MessageId}' still available on the bus, but it is not");
        }

        public async Task ShouldDeadLetteredMessageAsync(string entityName, ServiceBusMessage message)
        {
            await using ServiceBusReceiver receiver = _messagingClient.CreateReceiver(entityName, new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });
            IEnumerable<ServiceBusReceivedMessage> messages = await receiver.PeekMessagesAsync(100);
            Assert.True(messages.Any(actual => actual.MessageId == message.MessageId), $"Azure Service bus '{entityName}' should have dead-lettered message '{message.MessageId}', but can't find it in the dead-letter sub-queue");
        }

        public async Task ShouldCompleteMessageAsync(string entityName, ServiceBusMessage message)
        {
            await using ServiceBusReceiver receiver = _messagingClient.CreateReceiver(entityName);
            IEnumerable<ServiceBusReceivedMessage> messages = await receiver.PeekMessagesAsync(100);
            Assert.False(messages.Any(actual => actual.MessageId == message.MessageId), $"Azure Service bus '{entityName}' should have completed message '{message.MessageId}', but it can still be found on the queue");

            await using ServiceBusReceiver deadLetter = _messagingClient.CreateReceiver(entityName, new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });
            IEnumerable<ServiceBusReceivedMessage> deadLetteredMessages = await deadLetter.PeekMessagesAsync(100);
            Assert.False(deadLetteredMessages.Any(actual => actual.MessageId == message.MessageId), $"Azure Service bus '{entityName}' should have completed message '{message.MessageId}', but it can still be found on the dead-lettered queue");
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);

            disposables.AddRange(_queueNames.Select(queueName => AsyncDisposable.Create(async () =>
            {
                if (await _adminClient.QueueExistsAsync(queueName))
                {
                    _logger.LogTrace("[Test:Teardown] Fallback delete Azure Service Bus queue '{QueueName}'", queueName);
                    await _adminClient.DeleteQueueAsync(queueName);
                }
            })));

            disposables.AddRange(_subscriptionNames.Select(item => AsyncDisposable.Create(async () =>
            {
                if (await _adminClient.SubscriptionExistsAsync(item.topicName, item.subscriptionName))
                {
                    _logger.LogTrace("[Test:Teardown] Fallback delete Azure Service Bus topic subscription '{SubscriptionName}' on topic '{TopicName}'", item.subscriptionName, item.topicName);
                    await _adminClient.DeleteSubscriptionAsync(item.topicName, item.subscriptionName);
                }
            })));

            disposables.AddRange(_topicNames.Select(topicName => AsyncDisposable.Create(async () =>
            {
                if (await _adminClient.TopicExistsAsync(topicName))
                {
                    _logger.LogTrace("[Test:Teardown] Fallback delete Azure Service Bus topic '{TopicName}'", topicName);
                    await _adminClient.DeleteTopicAsync(topicName);
                }
            })));

            disposables.Add(_messagingClient);
            disposables.Add(_connection);
        }
    }
}
