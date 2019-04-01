using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Include.Setters
{
    interface IGenericProcessor
    {
        IEnumerable<T> Process<T>(EntityPool entityPool, IDataContext db, IEnumerable<T> entities) where T : class;
    }

    class GenericProcessor : IGenericProcessor
    {
        private static readonly Type _objType = typeof(object);
        private static readonly Type _valueType = typeof(ValueType);
        private static ConcurrentDictionary<Type, Func<IEnumerable, IEnumerable>> _processors = new ConcurrentDictionary<Type, Func<IEnumerable, IEnumerable>>();

        private GenericProcessor()
        {

        }

        internal static List<T> ProcessEntities<T>(EntityPool queryPool, IDataContext db, IEnumerable<T> entities) where T : class
        {
            return new GenericProcessor().Process(queryPool, db, entities).ToList();
        }

        public IEnumerable<T> Process<T>(EntityPool entityPool, IDataContext db, IEnumerable<T> entities) where T : class
        {
            var type = typeof(T);
            var baseType = GetRealBaseType(type);

            //TODO Only build this once
            var instance = Expression.Parameter(typeof(EntityPool), "qp");
            var contextParam = Expression.Parameter(typeof(IDataContext), "db");
            var entitiesParam = Expression.Parameter(typeof(IEnumerable<T>), "entities");

            //use this to call the process entities method
            var methodExpr = Expression.Call(instance, nameof(EntityPool.ProcessEntities),
                new Type[] { type, baseType },
                contextParam,
                entitiesParam);

            var lambda = Expression.Lambda<Func<EntityPool, IDataContext, IEnumerable<T>, IEnumerable<T>>>(methodExpr,
                instance,
                contextParam,
                entitiesParam);

            var func = lambda.Compile();

            var result = func(entityPool, db, entities);


            //_processors.TryAdd(type, );


            return result;
        }

        private Type GetRealBaseType(Type type)
        {
            var baseType = type.BaseType;
            var objType = typeof(object);

            while (baseType != objType && baseType != typeof(ValueType) && !type.IsInterface && !baseType.IsInterface)
            {
                type = baseType;
                baseType = type.BaseType;
            }

            return type;
        }
    }

    class EntityPool
    {
        private readonly Dictionary<Type, ITypePool> _typeBag = new Dictionary<Type, ITypePool>();




        internal ILookup<int, T> GetEntitiesOfType<T, TBase>(Func<T, int> fkFunc)
            where TBase : class
            where T : class, TBase
        {
            var baseType = typeof(TBase);
            if (!_typeBag.ContainsKey(baseType))
            {
                return null;
            }

            var typePoolDict = _typeBag[baseType] as TypePoolDict<TBase>;
            var lookup = typePoolDict.GetEntityLookupOfType<T>(fkFunc);
            return lookup;
        }

        internal IEnumerable<T> ProcessEntities<T, TBase>(IDataContext db, IEnumerable<T> entities)
            where TBase : class
            where T : class, TBase
        {
            var baseType = typeof(TBase);
            var desc = db.MappingSchema.GetEntityDescriptor(baseType);

            //TODO add check to ensure PK is set
            var pkFields = from x in desc.Columns
                           where x.IsPrimaryKey
                           orderby x.PrimaryKeyOrder
                           select x.MemberName;

            if (!pkFields.Any())
            {
                return entities;
            }

            var pkExpression = EntityPropertySetter.CreateHashCodeExpression<TBase>(pkFields);
            var pkFunc = pkExpression.Compile();

            if (!_typeBag.ContainsKey(baseType))
            {
                _typeBag.Add(baseType, new TypePoolDict<TBase>() as ITypePool);
            }


            var typePoolDict = _typeBag[baseType] as TypePoolDict<TBase>;
            var entitiesAndKeys = entities.Select(x => new { Key = pkFunc(x), Entity = x }).ToList();
            var lookup = entitiesAndKeys.ToLookup(x => x.Key, x => x.Entity);

            foreach (var grouping in lookup)
            {
                if (!typePoolDict.ContainsKey(grouping.Key))
                {
                    typePoolDict[grouping.Key] = grouping.First();
                }
            }

            var deDupeData = entitiesAndKeys.Select(l => typePoolDict[l.Key]).OfType<T>().ToList();
            return deDupeData;
        }
    }

    interface ITypePool { }

    interface ITypePoolDict<T> : ITypePool, IDictionary<int, T> where T : class
    {
        ILookup<int, T1> GetEntityLookupOfType<T1>(Func<T1, int> fkFunc) where T1 : class, T;
    }

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
