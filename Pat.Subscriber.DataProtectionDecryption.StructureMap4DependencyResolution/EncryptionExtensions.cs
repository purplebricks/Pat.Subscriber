using System;
using Pat.DataProtection;
using Pat.Subscriber.Deserialiser;
using StructureMap;

namespace Pat.Subscriber.DataProtectionDecryption.StructureMap4DependencyResolution
{
    public static class EncryptionExtensions
    {
        /// <summary>
        /// Returns the EncryptedMessageDeserialiser for encrypted messages otherwise returns default NewtonsoftMessageDeserialiser 
        /// </summary>
        /// <param name="dataProtectionConfiguration">Settings describing the keys to use for encryption / description</param>
        /// <returns>Factory method for obtaining appropriate deserialiser for the message</returns>
        public static Func<IContext, IMessageDeserialiser> EncryptionEnabledMessageDeserialiser(
            DataProtectionConfiguration dataProtectionConfiguration)
        {
            return provider => provider.GetInstance<MessageContext>().MessageEncrypted
                ? new EncryptedMessageDeserialiser(dataProtectionConfiguration)
                : (IMessageDeserialiser)new NewtonsoftMessageDeserialiser();
        }

        /// <summary>
        /// <para>Returns the EncryptedMessageDeserialiser for encrypted messages otherwise returns default NewtonsoftMessageDeserialiser</para>
        /// <para>DataProtectionConfiguration must be registered and obtainable from IoC Container</para>
        /// <para>Use the overloaded version if you need to control the specific DataProtectionConfiguration settings to use</para>
        /// </summary>
        /// <returns>Factory method for obtaining appropriate deserialiser for the message</returns>
        public static Func<IContext, IMessageDeserialiser> EncryptionEnabledMessageDeserialiser()
        {
            return provider => provider.GetInstance<MessageContext>().MessageEncrypted
                ? new EncryptedMessageDeserialiser(provider.GetInstance<DataProtectionConfiguration>())
                : (IMessageDeserialiser)new NewtonsoftMessageDeserialiser();
        }
    }
}
