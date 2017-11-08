using System;
using System.Collections.Generic;
using System.Linq;

namespace PB.ITOps.Messaging.PatLite.MessageMapping
{
    public class UnmappedMessageTypeException : Exception
    {
        public UnmappedMessageTypeException(string requestedType, IEnumerable<MessageTypeMapping> mappedTypes): base((string) MessageBuilder(requestedType, mappedTypes))
        {
        }

        private static string MessageBuilder(string requestedType, IEnumerable<MessageTypeMapping> mappedTypes) 
            => $"Unable to find mapping for '{requestedType}', known types: \r\n\t{string.Join("\r\n\t", mappedTypes.Select(m => m.MessageTypeName))}";
    }
}