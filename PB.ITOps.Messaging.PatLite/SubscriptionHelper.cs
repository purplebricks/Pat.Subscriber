using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace PB.ITOps.Messaging.PatLite
{
    public class MessageClientPair
    {
        public Message Message { get; set; }
        public IMessageReceiver MessageReceiver { get; set; }
    }

    public static class SubscriptionHelper
    {
        private const string AddressKey = "SubscriptionClientAddress";

        public static IList<MessageClientPair> GetMessages(this IList<IMessageReceiver> clients, int batchSize, int receiveTimeout)
        {
            var messageQueue = new List<MessageClientPair>();
            Task.WaitAll(clients.Select(c => QueueMessages(c, messageQueue, batchSize, receiveTimeout)).ToArray());
            return messageQueue;
        }

        private static async Task QueueMessages(IMessageReceiver messageReceiver, List<MessageClientPair> queueMessages, int batchSize, int receiveTimeout)
        {
            var messages = await messageReceiver.ReceiveAsync(batchSize, TimeSpan.FromSeconds(receiveTimeout)) ?? new List<Message>();
            foreach (var message in messages.Where(m => m != null))
            {
                message.UserProperties.Add(AddressKey, messageReceiver.Path);
                queueMessages.Add(new MessageClientPair
                {
                    Message = message,
                    MessageReceiver = messageReceiver
                });
            }
        }

        public static string RetrieveServiceBusAddress(this string connectionString)
        {
            var endpointPrefix = "Endpoint=sb://";
            var startIndex = connectionString.IndexOf(endpointPrefix, StringComparison.OrdinalIgnoreCase);
            var endIndex = connectionString.IndexOf(";", StringComparison.OrdinalIgnoreCase);
            if (startIndex >= 0 && endIndex > startIndex)
            {
                return connectionString.Substring(startIndex + endpointPrefix.Length, endIndex - endpointPrefix.Length);
            }
            return String.Empty;
        }

        public static string RetrieveServiceBusAddress(this Message message)
        {
            return message.UserProperties.ContainsKey(AddressKey) ? message.UserProperties[AddressKey].ToString() : null;
        }

        public static string RetrieveServiceBusAddressWithOnlyLetters(this Message message)
        {
            return Regex.Replace(message.RetrieveServiceBusAddress(), "[^a-zA-Z]", "");
        }
    }
}