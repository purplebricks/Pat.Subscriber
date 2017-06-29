using System;
using System.Linq;
using System.Reflection;

namespace PB.ITOps.Messaging.PatLite.MessageMapping
{
    public class MessageMapper
    {
        private static MessageTypeMapping[] _messageTypeMappings;

        public static void MapMessageTypesToHandlers()
        {
            var allTypes = AssemblyScanner.AllTypesInAssemblies(new[] {Assembly.GetEntryAssembly()}).ToArray();
            _messageTypeMappings = AssemblyScanner
                .AddDerivedTypeMappings(AssemblyScanner.MessageHandlerMappingsIn(allTypes), allTypes).ToArray();

            var groupedMappings = _messageTypeMappings.GroupBy(g => g.MessageType);
            var multipleMessageHandler = groupedMappings.FirstOrDefault(g => g.Count() > 1);
            if (multipleMessageHandler != null)
            {
                throw new InvalidOperationException($"Each event should only have one handler in a subscriber, {multipleMessageHandler.Key.FullName} has the following handlers:\n" +
                                                    $"{string.Join("\n", multipleMessageHandler.Select(h => $"{h.HandlerType.FullName}.{h.HandlerMethod.Name}({h.HandlerMethod.GetParameters().FirstOrDefault()?.ParameterType})"))}");
            }
        }

        public static MessageTypeMapping GetHandlerForMessageType(string messageType)
        {
            return _messageTypeMappings.Single(x => x.MessageTypeName == messageType);
        }
    }
}