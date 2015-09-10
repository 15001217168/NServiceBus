﻿namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Sagas;
    using NServiceBus.TransportDispatch;

    class AttachSagaDetailsToOutGoingMessageBehavior : Behavior<OutgoingContext>
    {
        public override void Invoke(OutgoingContext context, Action next)
        {
            ActiveSagaInstance saga;

            //attach the current saga details to the outgoing headers for correlation
            if (context.TryGet(out saga) && HasBeenFound(saga) && !string.IsNullOrEmpty(saga.SagaId))
            {
                context.SetHeader(Headers.OriginatingSagaId, saga.SagaId);
                context.SetHeader(Headers.OriginatingSagaType, saga.Metadata.SagaType.AssemblyQualifiedName);
            }

            next();
        }


        static bool HasBeenFound(ActiveSagaInstance saga)
        {
            return !saga.NotFound;
        }
    }
}