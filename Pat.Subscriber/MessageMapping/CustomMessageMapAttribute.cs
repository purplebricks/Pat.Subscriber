using System;

namespace Pat.Subscriber.MessageMapping
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class CustomMessageContentType : Attribute
    {
        public string ContentType { get; set; }
    }
}