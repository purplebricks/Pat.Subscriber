using System;

namespace PB.ITOps.Messaging.PatLite.Deserialiser
{
    public interface IMessageDeserialiser
    {
        object DeserialiseObject(string messageBody, Type messageType);
    }

}
