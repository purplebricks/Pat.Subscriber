namespace PB.ITOps.Messaging.PatLite.StructureMap4
{
    public interface IPatLiteRegistryBuilder
    {
        IMessagePipelineBuilder DefineMessagePipeline();
        IBatchPipelineBuilder DefineBatchPipeline();
        IPatLiteRegistryBuilder Use(SubscriberConfiguration subscriberConfiguration);
        PatLiteRegistry Build();
    }
}