using System;

namespace PB.ITOps.Messaging.PatLite
{
    public class SubscriberConfig
    {
        public SubscriberConfig()
        {
            TopicName = "pat";
            UseDevelopmentTopic = true;
            BatchSize = 16;
            UsePartitioning = false;
        }

        private string _topicName;

        /// <summary>
        /// Array of service bus connection strings that the subscriber will attempt to connect to
        /// </summary>
        public string[] ConnectionStrings { get; set; }

        /// <summary>
        /// Name of topic where messages will be received from
        /// </summary>
        public string TopicName
        {
            get => UseDevelopmentTopic ? _topicName + Environment.MachineName : _topicName;
            set => _topicName = value;
        }

        /// <summary>
        /// On bootstrapping, if the topic needs creating this determines if a partitioned topic should be created (no affect for existing topics).
        /// In production this should be true for resiliency, for dev thi will usually be false
        /// </summary>
        public bool UsePartitioning { get; set; }

        /// <summary>
        /// Name of the subscriber / subscription queue that the subscriber will connect to
        /// Will be created if it does not exist
        /// </summary>
        public string SubscriberName { get; set; }

        /// <summary>
        /// Number of messages the subscriber will attempt to receive and process in one batch.
        /// </summary>
        public int BatchSize { get; set; }

        /// <summary>
        /// If set to true, the machine name will be appended to the topic name
        /// Should be set to true for local development.
        /// </summary>
        public bool UseDevelopmentTopic { get; set; }
    }
}