using System;
using Newtonsoft.Json;

namespace Pat.Subscriber.Deserialiser
{
    public class NewtonsoftMessageDeserialiser: IMessageDeserialiser
    {
        /// <inheritdoc />
        public object DeserialiseObject(string message, Type messageType)
        {
            return JsonConvert.DeserializeObject(message, messageType);
        }
    }
}
