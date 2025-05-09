using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Mednote.Client.Utils
{
    // Event messages
    public class TranscriptionCompletedEvent
    {
        public string TranscriptionId { get; }

        public TranscriptionCompletedEvent(string transcriptionId)
        {
            TranscriptionId = transcriptionId;
        }
    }

    public class TranscriptionDeletedEvent
    {
        public string TranscriptionId { get; }

        public TranscriptionDeletedEvent(string transcriptionId)
        {
            TranscriptionId = transcriptionId;
        }
    }

    public class RecordingStateChangedEvent
    {
        public bool IsRecording { get; }
        public bool IsPaused { get; }

        public RecordingStateChangedEvent(bool isRecording, bool isPaused)
        {
            IsRecording = isRecording;
            IsPaused = isPaused;
        }
    }

    // Event aggregator interface
    public interface IEventAggregator
    {
        void Publish<TEvent>(TEvent eventToPublish);
        void Subscribe<TEvent>(Action<TEvent> action);
        void Unsubscribe<TEvent>(Action<TEvent> action);
    }

    // Event aggregator implementation
    public class EventAggregator : IEventAggregator
    {
        private readonly ConcurrentDictionary<Type, List<object>> _subscriptions = new();

        public void Publish<TEvent>(TEvent eventToPublish)
        {
            if (eventToPublish == null)
                throw new ArgumentNullException(nameof(eventToPublish));

            var eventType = typeof(TEvent);

            if (_subscriptions.TryGetValue(eventType, out var subscribers))
            {
                // Create a copy to avoid modification during enumeration
                var subscribersCopy = subscribers.ToList();

                foreach (var subscriber in subscribersCopy)
                {
                    if (subscriber is Action<TEvent> action)
                    {
                        try
                        {
                            action(eventToPublish);
                        }
                        catch (Exception ex)
                        {
                            Serilog.Log.Error(ex, $"Error in event handler for {eventType.Name}");
                        }
                    }
                }
            }
        }

        public void Subscribe<TEvent>(Action<TEvent> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var eventType = typeof(TEvent);

            _subscriptions.AddOrUpdate(
                eventType,
                _ => new List<object> { action },
                (_, list) =>
                {
                    list.Add(action);
                    return list;
                });
        }

        public void Unsubscribe<TEvent>(Action<TEvent> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var eventType = typeof(TEvent);

            if (_subscriptions.TryGetValue(eventType, out var subscribers))
            {
                subscribers.Remove(action);

                // If no more subscribers, remove the event type
                if (subscribers.Count == 0)
                {
                    _subscriptions.TryRemove(eventType, out _);
                }
            }
        }
    }
}