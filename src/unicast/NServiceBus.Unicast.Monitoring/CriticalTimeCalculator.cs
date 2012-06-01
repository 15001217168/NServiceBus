﻿namespace NServiceBus.Unicast.Monitoring
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    /// Performance counter for the critical time
    /// </summary>
    public class CriticalTimeCalculator
    {
        /// <summary>
        /// Updates the counter based on the passed times
        /// </summary>
        /// <param name="sent"> </param>
        /// <param name="processingStarted"></param>
        /// <param name="processingEnded"></param>
        public void Update(DateTime sent, DateTime processingStarted, DateTime processingEnded)
        {
            counter.RawValue = Convert.ToInt32((processingEnded - sent).TotalSeconds);

            timeOfLastCounter = processingEnded;

            maxDelta = (processingEnded - processingStarted).Add(TimeSpan.FromSeconds(1));
        }


        /// <summary>
        /// Verified that the counter exists
        /// </summary>
        public void Initialize(PerformanceCounter cnt)
        {
            counter = cnt;

            
            timer = new Timer(ClearPerfCounter, null, 0, 2000);
        }


        void ClearPerfCounter(object state)
        {
            var delta = DateTime.UtcNow - timeOfLastCounter;

            if (delta > maxDelta)
                counter.RawValue = 0;
        }
      
        PerformanceCounter counter;

        Timer timer;
        DateTime timeOfLastCounter;
        TimeSpan maxDelta = TimeSpan.FromSeconds(2);
    }
}