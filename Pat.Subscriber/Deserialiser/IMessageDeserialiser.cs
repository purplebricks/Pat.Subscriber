using System;

namespace Pat.Subscriber.Deserialiser
{
    public interface IMessageDeserialiser
    {
        /// <summary>Deserializes the string to the specified .NET type.</summary>
        /// <param name="messageBody">The string to deserialize.</param>
        /// <param name="messageType">The <see cref="T:System.Type" /> of object being deserialized.</param>
        /// <returns>The deserialized object.</returns>
        object DeserialiseObject(string messageBody, Type messageType);
    }

}
