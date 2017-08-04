using System;

namespace PB.ITOps.Messaging.PatLite.Serialiser
{
    public interface IMessageDeserialiser
    {
        object DeserialiseObject(string messageBody, Type messageType);
    }

}
