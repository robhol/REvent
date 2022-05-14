using System.Collections.Generic;
using System.Linq;

namespace REvent.Utility
{
    public class MutableLookup<TKey, TValue> where TKey : notnull
    {
        private readonly Dictionary<TKey, List<TValue>> _dictionary = new();

        public IEnumerable<TValue> this[TKey key] => _dictionary.TryGetValue(key, out var vs) ? vs : Enumerable.Empty<TValue>();

        private List<TValue> GetOrCreateList(TKey key)
        {
            if (!_dictionary.TryGetValue(key, out var list))
            {
                list = new List<TValue>();
                _dictionary.Add(key, list);
            }

            return list;
        }

        public void Add(TKey key, TValue value) => GetOrCreateList(key).Add(value);

        public void AddRange(TKey key, IEnumerable<TValue> values) => GetOrCreateList(key).AddRange(values);

        public void Remove(TKey key, TValue value)
        {
            if (_dictionary.TryGetValue(key, out var list))
                list.Remove(value);
        }

        public void ClearKey(TKey key)
        {
            if (_dictionary.TryGetValue(key, out var list))
                list.Clear();
        }

    }
}