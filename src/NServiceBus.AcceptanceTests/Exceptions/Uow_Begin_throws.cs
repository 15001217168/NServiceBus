﻿namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using System.Runtime.Serialization;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NServiceBus.UnitOfWork;
    using NUnit.Framework;

    public class Uow_Begin_throws : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_exception_thrown_from_begin()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.When(c => c.Subscribed, bus => bus.SendLocal(new Message())))
                    .AllowExceptions()
                    .Done(c => c.ExceptionReceived)
                    .Run();

            Assert.AreEqual(typeof(BeginException), context.Exception.GetType());
            StackTraceAssert.StartsWith(
@"at NServiceBus.AcceptanceTests.Exceptions.Uow_Begin_throws.Endpoint.UnitOfWorkThatThrowsInBegin.Begin()", context.Exception);
        }

        public class Context : ScenarioContext
        {
            public bool ExceptionReceived { get; set; }
            public Exception Exception { get; set; }
            public bool Subscribed { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.RegisterComponents(c => c.ConfigureComponent<UnitOfWorkThatThrowsInBegin>(DependencyLifecycle.InstancePerUnitOfWork));
                    b.DisableFeature<TimeoutManager>();
                    b.DisableFeature<SecondLevelRetries>();
                })
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    });
            }



            class ErrorNotificationSpy : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public BusNotifications BusNotifications { get; set; }

                public void Start()
                {
                    BusNotifications.Errors.MessageSentToErrorQueue.Subscribe(e =>
                    {
                        Context.Exception = e.Exception;
                        Context.ExceptionReceived = true;
                    });

                    Context.Subscribed = true;
                }

                public void Stop() { }
            }

            public class UnitOfWorkThatThrowsInBegin : IManageUnitsOfWork
            {
                public void Begin()
                {
                    throw new BeginException();
                }

                public void End(Exception ex = null)
                {
                }
            }

            class Handler : IHandleMessages<Message>
            {
                public void Handle(Message message)
                {
                }
            }
        }

        [Serializable]
        public class Message : IMessage
        {
        }

        [Serializable]
        public class BeginException : Exception
        {
            public BeginException()
                : base("BeginException")
            {

            }

            protected BeginException(SerializationInfo info, StreamingContext context)
            {
            }
        }
    }

}