using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Include.Setters
{
    class TypePoolDict<T> : ITypePoolDict<T> where T : class
    {
        private readonly IDictionary<int, T> _dict = new Dictionary<int, T>();

        public T this[int key] { get => _dict[key]; set => _dict[key] = value; }

        public ICollection<int> Keys => _dict.Keys;

        public ICollection<T> Values => _dict.Values;

        public int Count => _dict.Count;

        public bool IsReadOnly => _dict.IsReadOnly;

        public void Add(int key, T value) => _dict.Add(key, value);

        public void Add(KeyValuePair<int, T> item) => _dict.Add(item);

        public void Clear() => _dict.Clear();

        public bool Contains(KeyValuePair<int, T> item) => _dict.Contains(item);

        public bool ContainsKey(int key) => _dict.ContainsKey(key);

        public void CopyTo(KeyValuePair<int, T>[] array, int arrayIndex) => _dict.CopyTo(array, arrayIndex);

        public ILookup<int, T1> GetEntityLookupOfType<T1>(Func<T1, int> fkFunc) where T1 : class, T
        {
            return _dict.Values.OfType<T1>().ToLookup(fkFunc);
        }

        public IEnumerator<KeyValuePair<int, T>> GetEnumerator() => _dict.GetEnumerator();

        public bool Remove(int key) => _dict.Remove(key);

        public bool Remove(KeyValuePair<int, T> item) => _dict.Remove(item);

        public bool TryGetValue(int key, out T value) => _dict.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => _dict.GetEnumerator();
    }
}
