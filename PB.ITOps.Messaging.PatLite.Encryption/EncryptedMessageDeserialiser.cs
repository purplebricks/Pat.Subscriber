using System;
using Microsoft.AspNetCore.DataProtection;
using PB.ITOps.Messaging.DataProtection;
using PB.ITOps.Messaging.PatLite.Serialiser;
using DataProtectionProvider = PB.ITOps.Messaging.DataProtection.DataProtectionProvider;

namespace PB.ITOps.Messaging.PatLite.Encryption
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
