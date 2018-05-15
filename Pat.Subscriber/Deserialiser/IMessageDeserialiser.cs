using System;

namespace Pat.Subscriber.Deserialiser
{
    public interface IMessageDeserialiser
    {
        object DeserialiseObject(string messageBody, Type messageType);
    }

}
