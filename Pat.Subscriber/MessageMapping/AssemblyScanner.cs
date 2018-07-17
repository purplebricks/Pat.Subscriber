using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Pat.Subscriber.MessageMapping
{
    public static class AssemblyScanner
    {
        /// <summary>
        /// Scans the given list of assemblies and produces message-to-handler mappings for all classes that implement <see cref="IHandleEvent{TMessage}"/>.
        /// </summary>
        /// <param name="allTypes"></param>
        /// <returns>An enumerable set of message-to-handler mappings.</returns>
        public static IEnumerable<MessageTypeMapping> MessageHandlerMappingsIn(IEnumerable<Type> allTypes)
        {
            foreach (var possibleHandlerType in allTypes)
            {
                foreach (var messageType in AllMessageTypesHandledBy(possibleHandlerType))
                {
                    yield return new MessageTypeMapping(messageType, possibleHandlerType);
                }
            }
        }

        public static IEnumerable<Type> AllTypesInAssemblies(Assembly[] assembliesToScan)
            => assembliesToScan.SelectMany(assembly => assembly.DefinedTypes.Select(x => x.AsType()));

        public static IEnumerable<Type> AllMessageTypesHandledBy(Type possibleHandlerType)
            => possibleHandlerType
                .GetInterfaces()
                .Where(IsHandlerInterface)
                .Select(handlesInterface => handlesInterface.GenericTypeArguments[0]);

        private static bool IsHandlerInterface(Type type)
            => type.IsConstructedGenericType
               && type.GetGenericTypeDefinition() == typeof(IHandleEvent<>);

        public static IEnumerable<MessageTypeMapping> AddDerivedTypeMappings(IEnumerable<MessageTypeMapping> mappedMessageTypes, Type[] allTypes)
        {
            var messageTypes = mappedMessageTypes.Select(m => m.MessageType).ToArray();
            foreach (var mappedMessageType in mappedMessageTypes)
            {
                var derivedTypes = allTypes.Where(t => mappedMessageType.MessageType.IsAssignableFrom(t) && t != mappedMessageType.MessageType).ToArray();
                var newDerivedTypes = derivedTypes.Where(m => !messageTypes.Contains(m));
                
                foreach (var newDerivedType in newDerivedTypes)
                {
                    yield return new MessageTypeMapping(newDerivedType, mappedMessageType.HandlerType);
                }

                yield return mappedMessageType;
            }
        }
    }
}