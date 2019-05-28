using System;
using System.Reflection;

namespace Pat.Subscriber.MessageMapping
{
    public class MessageTypeMapping
    {
        private string _messageContentTypeName = null;
        /// <summary>
        /// Constructs a new instance of a <see cref="MessageTypeMapping"/>.
        /// </summary>
        /// <param name="messageType">The type of the message to handle.</param>
        /// <param name="handlerType">The type of the handler class that is handling the message.</param>
        public MessageTypeMapping(Type messageType, Type handlerType)
        {
            MessageType = messageType;
            HandlerType = handlerType;
            HandlerMethod = handlerType.GetMethod(nameof(IHandleEvent<object>.HandleAsync), new[] { messageType });
        }

        /// <summary>
        /// Constructs a new instance of a <see cref="MessageTypeMapping"/>.
        /// </summary>
        /// <param name="messageType">The type of the message to handle.</param>
        /// <param name="handlerType">The type of the handler class that is handling the message.</param>
        public MessageTypeMapping(Type messageType, string messageContentTypeName, Type handlerType)
        {
            MessageType = messageType;
            HandlerType = handlerType;
            _messageContentTypeName = messageContentTypeName;
            HandlerMethod = handlerType.GetMethod(nameof(IHandleEvent<object>.HandleAsync), new[] { messageType });
        }

        /// <summary>
        /// Gets the type of the message represented by this mapping.
        /// </summary>
        public Type MessageType { get; }

        /// <summary>
        /// Gets the type of the handler that messages of the <see cref="P:MessageType"/> type will be handled by.
        /// </summary>
        public Type HandlerType { get; }

        /// <summary>
        /// Gets the <see cref="Type.FullName"/> of a message that is used in the Subscription filter rule managed by Pat.
        /// </summary>
        public string MessageTypeName => _messageContentTypeName ?? MessageType.FullName;

        /// <summary>
        /// Gets a reference to the method on the handler that will process messages of the <see cref="P:MessageType"/> type.
        /// </summary>
        public MethodInfo HandlerMethod { get; }
    }
}