using System;
using Microsoft.AspNetCore.DataProtection;
using Pat.DataProtection;
using Pat.Subscriber.Deserialiser;
using DataProtectionProvider = Pat.DataProtection.DataProtectionProvider;

namespace Pat.Subscriber.DataProtectionDecryption
{
    public class EncryptedMessageDeserialiser : IMessageDeserialiser
    {
        private readonly IDataProtector _protector;
        private readonly NewtonsoftMessageDeserialiser _newtonsoftMessageDeserialiser;

        public EncryptedMessageDeserialiser(DataProtectionConfiguration configuration)
        {
            var provider = DataProtectionProvider.Create(configuration);
            _protector = provider.CreateProtector("PatLite");
            _newtonsoftMessageDeserialiser = new NewtonsoftMessageDeserialiser();
        }

        public object DeserialiseObject(string messageBody, Type messageType)
        {
            var unprotectedMessageBody = _protector.Unprotect(messageBody);
            return _newtonsoftMessageDeserialiser.DeserialiseObject(unprotectedMessageBody, messageType);
        }
    }
}
