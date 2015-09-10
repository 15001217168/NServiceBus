namespace NServiceBus
{
    using System;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Sagas;

    /// <summary>
    /// Finds the given type of saga by looking it up based on the given property.
    /// </summary>
    class PropertySagaFinder<TSagaData> : SagaFinder where TSagaData : IContainSagaData
    {
        ISagaPersister sagaPersister;

        public PropertySagaFinder(ISagaPersister sagaPersister)
        {
            this.sagaPersister = sagaPersister;
        }

        internal override IContainSagaData Find(IBuilder builder, SagaFinderDefinition finderDefinition, SagaPersistenceOptions options, object message)
        {
            var propertyAccessor = (Func<object,object>)finderDefinition.Properties["property-accessor"];
            var propertyValue = propertyAccessor(message);

            var sagaPropertyName = (string)finderDefinition.Properties["saga-property-name"];

            if (sagaPropertyName.ToLower() == "id")
            {
                return sagaPersister.Get<TSagaData>((Guid)propertyValue, options).GetAwaiter().GetResult();
            }

            return sagaPersister.Get<TSagaData>(sagaPropertyName, propertyValue, options).GetAwaiter().GetResult();
        }
    }
}
