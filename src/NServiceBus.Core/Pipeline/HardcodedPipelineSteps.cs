namespace NServiceBus.Pipeline
{
    class HardcodedPipelineSteps
    {

        public static void RegisterIncomingCoreBehaviors(PipelineSettings pipeline)
        {
            pipeline.RegisterConnector<TransportReceiveToPhysicalMessageProcessingConnector>("Allows to abort processing the message");
            pipeline.RegisterConnector<DeserializeLogicalMessagesConnector>("Deserializes the physical message body into logical messages");
            pipeline.RegisterConnector<ExecuteLogicalMessagesConnector>("Starts the execution of each logical message");
            pipeline.RegisterConnector<LoadHandlersConnector>("Gets all the handlers to invoke from the MessageHandler registry based on the message type.");


            pipeline
                .Register(WellKnownStep.ExecuteUnitOfWork, typeof(UnitOfWorkBehavior), "Executes the UoW")
                .Register(WellKnownStep.MutateIncomingTransportMessage, typeof(MutateIncomingTransportMessageBehavior), "Executes IMutateIncomingTransportMessages")
                .Register(WellKnownStep.MutateIncomingMessages, typeof(MutateIncomingMessageBehavior), "Executes IMutateIncomingMessages")
                .Register(WellKnownStep.InvokeHandlers, typeof(InvokeHandlersBehavior), "Calls the IHandleMessages<T>.Handle(T)");
        }
    }
}