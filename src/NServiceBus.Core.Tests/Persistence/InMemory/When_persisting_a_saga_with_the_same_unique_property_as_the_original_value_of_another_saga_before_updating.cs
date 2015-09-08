namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Saga;
    using NUnit.Framework;

    [TestFixture]
    class When_persisting_a_saga_with_the_same_unique_property_as_the_original_value_of_another_saga_before_updating
    {
        [Test]
        public async Task It_should_persist_successfully()
        {
            var saga1 = new SagaWithUniquePropertyData{Id = Guid.NewGuid(), UniqueString = "whatever"};
            var saga2 = new SagaWithUniquePropertyData{Id = Guid.NewGuid(), UniqueString = "whatever"};

            var options = new SagaPersistenceOptions(SagaMetadata.Create(typeof(SagaWithUniqueProperty)));

            var persister = InMemoryPersisterBuilder.Build<SagaWithUniqueProperty>();
            await persister.Save(saga1, options);
            saga1 = await persister.Get<SagaWithUniquePropertyData>(saga1.Id, options);
            saga1.UniqueString = "whatever2";
            await persister.Update(saga1, options);

            await persister.Save(saga2, options);
        }
    }
}