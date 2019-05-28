using System;

namespace Pat.Subscriber.MessageMapping
{
    public class CustomMessageTypeMap
    {
        public string MessageType { get; }
        public Type MappedMessageType { get; }
        public Type HandlerType { get; }

        public CustomMessageTypeMap(string messageType, Type mappedMessageType, Type handlerType)
        {
            MessageType = messageType;
            MappedMessageType = mappedMessageType;
            HandlerType = handlerType;
        }

        public MessageTypeMapping ToMessageTypeMapping()
        {
            return new MessageTypeMapping(MappedMessageType, MessageType, HandlerType);
        }
    }
}