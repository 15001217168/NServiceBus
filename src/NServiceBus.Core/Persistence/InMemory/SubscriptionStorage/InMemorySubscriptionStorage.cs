namespace NServiceBus.InMemory.SubscriptionStorage
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Unicast.Subscriptions;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

    /// <summary>
    ///     In memory implementation of the subscription storage
    /// </summary>
    class InMemorySubscriptionStorage : ISubscriptionStorage, IQuerySubscriptions
    {
        public Task Subscribe(string address, IEnumerable<MessageType> messageTypes, SubscriptionStorageOptions options)
        {
            foreach (var m in messageTypes)
            {
                var dict = storage.GetOrAdd(m, type => new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase));

                dict.AddOrUpdate(address, addValueFactory, updateValueFactory);
            }
            return Task.FromResult(0);
        }

        public Task Unsubscribe(string address, IEnumerable<MessageType> messageTypes, SubscriptionStorageOptions options)
        {
            foreach (var m in messageTypes)
            {
                ConcurrentDictionary<string, object> dict;
                if (storage.TryGetValue(m, out dict))
                {
                    object _;
                    dict.TryRemove(address, out _);
                }
            }
            return Task.FromResult(0);
        }

        public Task<IEnumerable<string>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
        {
            var result = new HashSet<string>();
            foreach (var m in messageTypes)
            {
                ConcurrentDictionary<string, object> list;
                if (storage.TryGetValue(m, out list))
                {
                    result.UnionWith(list.Keys);
                }
            }
            return Task.FromResult((IEnumerable<string>) result);
        }

        ConcurrentDictionary<MessageType, ConcurrentDictionary<string, object>> storage = new ConcurrentDictionary<MessageType, ConcurrentDictionary<string, object>>();
        Func<string, object> addValueFactory = a => null;
        Func<string, object, object> updateValueFactory = (a, o) => null;
    }
}