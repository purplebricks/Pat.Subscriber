using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace PB.ITOps.Messaging.PatLite
{
    public static class SubscriptionHelper
    {
        private const string AddressKey = "SubscriptionClientAddress";

        public static async Task<IList<Message>> GetMessages(this IMessageReceiver messageReceiver, int batchSize, int receiveTimeoutSeconds)
        {
            var messages = await messageReceiver.ReceiveAsync(batchSize, TimeSpan.FromSeconds(receiveTimeoutSeconds)) ?? new List<Message>();
            foreach (var message in messages.Where(m => m != null))
            {
                message.UserProperties.Add(AddressKey, messageReceiver.Path);
            }
            return messages;
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