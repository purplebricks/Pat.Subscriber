using System;
using Newtonsoft.Json;

namespace PB.ITOps.Messaging.PatLite.Serialiser
{
    public class NewtonsoftMessageDeserialiser: IMessageDeserialiser
    {
        public object DeserialiseObject(string message, Type messageType)
        {
            return JsonConvert.DeserializeObject(message, messageType);
        }
    }
}
