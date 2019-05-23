using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Pat.Subscriber.MessageMapping
{
    public class MessageMapper
    {
        private static List<MessageTypeMapping> _messageTypeMappings;

        public static void MapMessageTypesToHandlers(Assembly[] handlerAssemblies)
        {
            var allTypes = AssemblyScanner.AllTypesInAssemblies(handlerAssemblies).ToArray();
            _messageTypeMappings = AssemblyScanner
                .AddDerivedTypeMappings(AssemblyScanner.MessageHandlerMappingsIn(allTypes), allTypes).ToList();

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
            var mapping = _messageTypeMappings.SingleOrDefault(x => x.MessageTypeName == messageType);
            if (mapping == null)
            {
                throw new UnmappedMessageTypeException(messageType, _messageTypeMappings);
            }
            return mapping;
        }

        public static IEnumerable<Type> GetHandledTypes()
        {
            return _messageTypeMappings.Select(m => m.MessageType);
        }

        internal static void AddCustomMessageMaps(IList<CustomMessageMap> customMethodMap)
        {
            foreach (var map in customMethodMap ?? new List<CustomMessageMap>())
            {
                _messageTypeMappings.Add(new MessageTypeMapping(map.OriginalMessageType, map.CustomMessageType, map.HandlerType ));
            }
        }
    }
}