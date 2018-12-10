using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pat.Subscriber.MessageProcessing
{
    public class MessageProcessingBehaviourPipeline
    {
        private readonly ICollection<IMessageProcessingBehaviour> _behaviours;
        private Func<MessageContext, Task> _pipeline;

        public MessageProcessingBehaviourPipeline AddBehaviour(IMessageProcessingBehaviour nextBehaviour)
        {
            _behaviours.Add(nextBehaviour);
            return this;
        }

        public MessageProcessingBehaviourPipeline()
        {
            _behaviours = new List<IMessageProcessingBehaviour>();
        }

        public async Task Invoke(MessageContext messageContext)
        {
            if (_pipeline == null)
            {
                _pipeline = BuildPipeline();
            }
            await _pipeline(messageContext).ConfigureAwait(false);
        }

        public void Build()
        {
            BuildPipeline();
        }

        private Func<MessageContext, Task> BuildPipeline()
        {
            Func<MessageContext, Task> current = null;
            foreach (var behaviour in _behaviours.Reverse())
            {

                var next = current;
                current = async m => await behaviour.Invoke(next, m).ConfigureAwait(false);
            }
            return current;
        }
    }
}