using System;
using System.Reflection;

namespace Pat.Subscriber.MessageMapping
{
    public class MessageTypeMapping
    {
        /// <summary>
        /// Constructs a new instance of a <see cref="MessageTypeMapping"/>.
        /// </summary>
        /// <param name="messageType">The type of the message to handle.</param>
        /// <param name="handlerType">The type of the handler class that is handling the message.</param>
        public MessageTypeMapping(Type messageType, Type handlerType)
        {
            MessageType = messageType;
            MessageContentTypeName = messageType.SimpleQualifiedName();
            HandlerType = handlerType;
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
        public string MessageTypeName => MessageType.FullName;

        /// <summary>
        /// Gets the Type Full Name followed by the unversioned Assembly Name of a message that is used in the ContentType property of the message on Azure Service Bus.
        /// The receiving endpoint uses this property when de-serialising the message.
        /// </summary>
        public string MessageContentTypeName { get; }

        /// <summary>
        /// Gets a reference to the method on the handler that will process messages of the <see cref="P:MessageType"/> type.
        /// </summary>
        public MethodInfo HandlerMethod { get; }
    }
}