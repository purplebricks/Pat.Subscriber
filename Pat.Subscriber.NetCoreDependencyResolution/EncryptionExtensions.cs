using System;
using Microsoft.Extensions.DependencyInjection;
using Pat.DataProtection;
using Pat.Subscriber.DataProtectionDecryption;
using Pat.Subscriber.Deserialiser;

namespace Pat.Subscriber.NetCoreDependencyResolution
{
    public static class EncryptionExtensions
    {
        /// <summary>
        /// Returns the EncryptedMessageDeserialiser for encrypted messages otherwise returns default NewtonsoftMessageDeserialiser 
        /// </summary>
        /// <param name="dataProtectionConfiguration">Settings describing the keys to use for encryption / description</param>
        /// <returns>Factory method for obtaining appropriate deserialiser for the message</returns>
        public static Func<IServiceProvider, IMessageDeserialiser> EncryptedMessageDeserialiser(
            DataProtectionConfiguration dataProtectionConfiguration)
        {
            return provider => provider.GetService<MessageContext>().MessageEncrypted
                ? new EncryptedMessageDeserialiser(dataProtectionConfiguration)
                : (IMessageDeserialiser)new NewtonsoftMessageDeserialiser();
        }

        /// <summary>
        /// <para>Returns the EncryptedMessageDeserialiser for encrypted messages otherwise returns default NewtonsoftMessageDeserialiser</para>
        /// <para>DataProtectionConfiguration must be registered and obtainable from IoC Container</para>
        /// <para>Use the overloaded version if you need to control the specific DataProtectionConfiguration settings to use</para>
        /// </summary>
        /// <returns>Factory method for obtaining appropriate deserialiser for the message</returns>
        public static Func<IServiceProvider, IMessageDeserialiser> EncryptedMessageDeserialiser()
        {
            return provider => provider.GetService<MessageContext>().MessageEncrypted
                ? new EncryptedMessageDeserialiser(provider.GetService<DataProtectionConfiguration>())
                : (IMessageDeserialiser)new NewtonsoftMessageDeserialiser();
        }
    }
}
