﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Platibus.Http;

namespace Platibus.Diagnostics
{
    /// <summary>
    /// A <see cref="IDiagnosticService"/> implementation that tracks key metrics to report on
    /// the activity and health of the instance
    /// </summary>
    public class MetricsCollector : IDiagnosticEventSink, IDisposable
    {
        private readonly TimeSpan _sampleRate;
        private readonly Timer _sampleTimer;

        private Metrics _current = new Metrics();
        private Metrics _sample = new Metrics();
        
        private bool _disposed;

        /// <summary>
        /// Returns the latest sample metrics normalized per second
        /// </summary>
        public Dictionary<string, double> Sample
        {
            get
            {
                var sample = _sample;
                var sampleRateInSeconds = _sampleRate.TotalSeconds;
                return new Dictionary<string, double>
                {
                    {"Requests", sample.Requests/sampleRateInSeconds},
                    {"Received", sample.Received/sampleRateInSeconds},
                    {"Acknowledgements", sample.Acknowledgements/sampleRateInSeconds},
                    {"AcknowledgementFailures", sample.AcknowledgementFailures/sampleRateInSeconds},
                    {"Expired", sample.Expired/sampleRateInSeconds},
                    {"Dead", sample.Dead/sampleRateInSeconds},
                    {"Sent", sample.Sent/sampleRateInSeconds},
                    {"Published", sample.Published/sampleRateInSeconds},
                    {"Delivered", sample.Delivered/sampleRateInSeconds},
                    {"DeliveryFailures", sample.DeliveryFailures/sampleRateInSeconds},
                    {"Errors", sample.Errors/sampleRateInSeconds},
                    {"Warnings", sample.Warnings/sampleRateInSeconds}
                };
            }
        }

        /// <summary>
        /// Initializes a new <see cref="MetricsCollector"/>
        /// </summary>
        /// <param name="sampleRate">The rate at which samples should be taken</param>
        public MetricsCollector(TimeSpan sampleRate = default(TimeSpan))
        {
            if (sampleRate <= TimeSpan.Zero)
            {
                sampleRate = TimeSpan.FromSeconds(3);
            }
            _sampleRate = sampleRate;
            _sampleTimer = new Timer(_ => TakeSample(), null, _sampleRate, _sampleRate);
        }
        
        /// <inheritdoc />
        public void Consume(DiagnosticEvent @event)
        {
            IncrementCounters(@event);
        }

        /// <inheritdoc />
        public Task ConsumeAsync(DiagnosticEvent @event, CancellationToken cancellationToken = new CancellationToken())
        {
            IncrementCounters(@event);
            return Task.FromResult(0);
        }

        private void IncrementCounters(DiagnosticEvent @event)
        {
            switch (@event.Type.Level)
            {
                case DiagnosticEventLevel.Error:
                    Interlocked.Increment(ref _current.Errors);
                    break;
                case DiagnosticEventLevel.Warn:
                    Interlocked.Increment(ref _current.Warnings);
                    break;
            }

            if (@event.Type == HttpEventType.HttpRequestReceived)
            {
                Interlocked.Increment(ref _current.Requests);
            }
            if (@event.Type == DiagnosticEventType.MessageReceived)
            {
                Interlocked.Increment(ref _current.Received);
            }
            else if (@event.Type == DiagnosticEventType.MessageAcknowledged)
            {
                Interlocked.Increment(ref _current.Acknowledgements);
            }
            else if (@event.Type == DiagnosticEventType.MessageNotAcknowledged)
            {
                Interlocked.Increment(ref _current.AcknowledgementFailures);
            }
            else if (@event.Type == DiagnosticEventType.MessageSent)
            {
                Interlocked.Increment(ref _current.Sent);
            }
            else if (@event.Type == DiagnosticEventType.MessagePublished)
            {
                Interlocked.Increment(ref _current.Published);
            }
            else if (@event.Type == DiagnosticEventType.MessageDelivered)
            {
                Interlocked.Increment(ref _current.Delivered);
            }
            else if (@event.Type == DiagnosticEventType.MessageDeliveryFailed)
            {
                Interlocked.Increment(ref _current.DeliveryFailures);
            }
            else if (@event.Type == DiagnosticEventType.MessageExpired)
            {
                Interlocked.Increment(ref _current.Expired);
            }
            else if (@event.Type == DiagnosticEventType.DeadLetter)
            {
                Interlocked.Increment(ref _current.Dead);
            }
        }

        private void TakeSample()
        {
            var current = Interlocked.Exchange(ref _current, new Metrics());
            Interlocked.Exchange(ref _sample, current);
        }

        /// <summary>
        /// Releases or frees managed and unmanaged resources
        /// </summary>
        /// <param name="disposing">Whether this method is called from <see cref="Dispose()"/></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sampleTimer.Dispose();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            Dispose(true);
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        ~MetricsCollector()
        {
            Dispose(false);
        }
        
        private class Metrics
        {
            public long Requests;
            public long Received;
            public long Acknowledgements;
            public long AcknowledgementFailures;
            public long Expired;
            public long Dead;
            public long Sent;
            public long Published;
            public long Delivered;
            public long DeliveryFailures;
            public long Errors;
            public long Warnings;
        }
    }
}
