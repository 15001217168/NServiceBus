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

    public class Uow_Begin_and_diff_End_throws : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_AggregateException_with_both_exceptions()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.When(c => c.Subscribed, bus => bus.SendLocal(new Message())))
                    .AllowExceptions()
                    .Done(c => c.ExceptionReceived)
                    .Run();

            Assert.AreEqual(typeof(BeginException), context.Exception.InnerExceptions[0].GetType());
            Assert.AreEqual(typeof(EndException), context.Exception.InnerExceptions[1].GetType());

            StackTraceAssert.StartsWith(
@"at NServiceBus.UnitOfWorkBehavior.Invoke(Context context, Action next)", context.Exception);

            StackTraceAssert.StartsWith(
string.Format(@"at NServiceBus.AcceptanceTests.Exceptions.Uow_Begin_and_diff_End_throws.Endpoint.{0}.End(Exception ex)", context.TypeName), context.Exception.InnerExceptions[1]);

        }

        public class Context : ScenarioContext
        {
            public bool ExceptionReceived { get; set; }
            public AggregateException Exception { get; set; }
            public bool FirstOneExecuted { get; set; }
            public string TypeName { get; set; }
            public bool Subscribed { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.RegisterComponents(c =>
                    {
                        c.ConfigureComponent<UnitOfWorkThatThrows1>(DependencyLifecycle.InstancePerUnitOfWork);
                        c.ConfigureComponent<UnitOfWorkThatThrows2>(DependencyLifecycle.InstancePerUnitOfWork);
                    });
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
                        var aggregateException = (AggregateException)e.Exception;
                        Context.Exception = aggregateException;
                        Context.ExceptionReceived = true;
                    });

                    Context.Subscribed = true;
                }

                public void Stop() { }
            }

            public class UnitOfWorkThatThrows1 : IManageUnitsOfWork
            {
                public Context Context { get; set; }

                bool throwAtEnd;

                public void Begin()
                {
                    if (Context.FirstOneExecuted)
                    {
                        throw new BeginException();
                    }

                    Context.FirstOneExecuted = throwAtEnd = true;
                }

                public void End(Exception ex = null)
                {
                    if (throwAtEnd)
                    {
                        Context.TypeName = GetType().Name;

                        throw new EndException();
                    }
                }
            }
            public class UnitOfWorkThatThrows2 : IManageUnitsOfWork
            {
                public Context Context { get; set; }

                bool throwAtEnd;

                public void Begin()
                {
                    if (Context.FirstOneExecuted)
                    {
                        throw new BeginException();
                    }

                    Context.FirstOneExecuted = throwAtEnd = true;
                }

                public void End(Exception ex = null)
                {
                    if (throwAtEnd)
                    {
                        Context.TypeName = GetType().Name;

                        throw new EndException();
                    }
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

        [Serializable]
        public class EndException : Exception
        {
            public EndException()
                : base("EndException")
            {

            }

            protected EndException(SerializationInfo info, StreamingContext context)
            {
            }
        }
    }

}