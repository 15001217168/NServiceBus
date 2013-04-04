namespace NServiceBus.Satellites
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Config;
    using Logging;
    using ObjectBuilder;
    using Unicast.Transport;

    public class SatelliteLauncher : IWantToRunWhenBusStartsAndStops
    {
        public IBuilder Builder { get; set; }

        public void Start()
        {
            Configure.Instance.Builder
                .BuildAll<ISatellite>()
                .ToList()
                .ForEach(s =>
                {
                    if (s.Disabled)
                    {
                        return;
                    }

                    var ctx = new SatelliteContext
                    {
                        Instance = s
                    };

                    if (s.InputAddress != null)
                    {
                        ctx.Transport = Builder.Build<TransportReceiver>();

                        var advancedSatellite = s as IAdvancedSatellite;
                        if (advancedSatellite != null)
                        {
                            var receiverCustomization = advancedSatellite.GetReceiverCustomization();

                            receiverCustomization(ctx.Transport);
                        }
                    }

                    StartSatellite(ctx);

                    satellites.Add(ctx);
                });
        }

        public void Stop()
        {
            for (int index   = 0; index < satellites.Count; index++)
            {
                var ctx = satellites[index];

                Logger.DebugFormat("Stopping {1}/{2} '{0}' satellite", ctx.Instance.GetType().AssemblyQualifiedName, index + 1, satellites.Count);

                if (ctx.Transport != null)
                {
                    ctx.Transport.Stop();
                }

                ctx.Instance.Stop();

                Logger.DebugFormat("Stopped {1}/{2} '{0}' satellite", ctx.Instance.GetType().AssemblyQualifiedName, index + 1, satellites.Count);
            }
        }

        void HandleMessageReceived(object sender, TransportMessageReceivedEventArgs e, ISatellite satellite)
        {
            try
            {
                if (!satellite.Handle(e.Message))
                {
                    ((ITransport)sender).AbortHandlingCurrentMessage();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("{0} satellite could not handle message.", satellite.GetType().AssemblyQualifiedName), ex);
                throw;
            }            
        }

        void StartSatellite(SatelliteContext ctx)
        {
            Logger.DebugFormat("Starting satellite {0} for {1}.", ctx.Instance.GetType().AssemblyQualifiedName, ctx.Instance.InputAddress);

            try
            {
                if (ctx.Transport != null)
                {
                    ctx.Transport.TransportMessageReceived += (o, e) => HandleMessageReceived(o, e, ctx.Instance);
                    ctx.Transport.Start(ctx.Instance.InputAddress);
                }
                else
                {
                    Logger.DebugFormat("No input queue configured for {0}", ctx.Instance.GetType().AssemblyQualifiedName);
                }

                ctx.Instance.Start();
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("Satellite {0} failed to start.", ctx.Instance.GetType().AssemblyQualifiedName), ex);

                if (ctx.Transport != null)
                {
                    ctx.Transport.ChangeMaximumConcurrencyLevel(0);                        
                }
            }
        }
     
        static readonly ILog Logger = LogManager.GetLogger(typeof(SatelliteLauncher));

        private readonly List<SatelliteContext> satellites = new List<SatelliteContext>();
    }   
}