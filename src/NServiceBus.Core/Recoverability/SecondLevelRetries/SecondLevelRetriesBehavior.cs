namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Recoverability.SecondLevelRetries;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    class SecondLevelRetriesBehavior : PhysicalMessageProcessingStageBehavior
    {
        public SecondLevelRetriesBehavior(IPipelineBase<DispatchContext> dispatchPipeline, SecondLevelRetryPolicy retryPolicy, BusNotifications notifications)
        {
             this.dispatchPipeline = dispatchPipeline;
            this.retryPolicy = retryPolicy;
            this.notifications = notifications;
        }

        public override void Invoke(Context context, Action next)
        {
            try
            {
                next();
            }
            catch (MessageDeserializationException)
            {
                context.GetPhysicalMessage().Headers.Remove(Headers.Retries);
                throw; // no SLR for poison messages
            }
            catch (Exception ex)
            {
                var message = context.GetPhysicalMessage();
                var currentRetry = GetNumberOfRetries(message.Headers) +1;

                TimeSpan delay;

                if (retryPolicy.TryGetDelay(message, ex, currentRetry, out delay))
                {
                    var receiveAddress = PipelineInfo.PublicAddress;

                    var messageToRetry = new OutgoingMessage(context.GetPhysicalMessage().Id, message.Headers, message.Body);

                    messageToRetry.Headers[Headers.Retries] = currentRetry.ToString();
                    messageToRetry.Headers[RetriesTimestamp] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);

                    var dispatchContext = new DispatchContext(messageToRetry, context);

                    context.Set<RoutingStrategy>(new DirectToTargetDestination(receiveAddress));
                    context.Set(new List<DeliveryConstraint>
                    {
                        new DelayDeliveryWith(delay)
                    });

                    Logger.Warn(string.Format("Second Level Retry will reschedule message '{0}' after a delay of {1} because of an exception:", message.Id, delay), ex);
                    dispatchPipeline.Invoke(dispatchContext);

             
                    notifications.Errors.InvokeMessageHasBeenSentToSecondLevelRetries(currentRetry,message,ex);

                    return;
                }

                message.Headers.Remove(Headers.Retries);
                Logger.WarnFormat("Giving up Second Level Retries for message '{0}'.", message.Id);
                throw;
            }

        }

        static int GetNumberOfRetries(Dictionary<string, string> headers)
        {
            string value;
            if (headers.TryGetValue(Headers.Retries, out value))
            {
                int i;
                if (int.TryParse(value, out i))
                {
                    return i;
                }
            }
            return 0;
        }


        IPipelineBase<DispatchContext> dispatchPipeline;
        SecondLevelRetryPolicy retryPolicy;
        BusNotifications notifications;

        static ILog Logger = LogManager.GetLogger<SecondLevelRetriesBehavior>();

        public const string RetriesTimestamp = "NServiceBus.Retries.Timestamp";

        public class Registration : RegisterStep
        {
            public Registration()
                : base("SecondLevelRetries", typeof(SecondLevelRetriesBehavior), "Performs second level retries")
            {
                InsertBeforeIfExists("FirstLevelRetries");
                InsertBeforeIfExists("ReceivePerformanceDiagnosticsBehavior");
            }
        }

    }
}