using LinqToDB.Include.Cache;
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
    class EntityPool
    {
        readonly static ConcurrentDictionary<Type, IBaseTypeHashCode> _typeHasCodeCache =
            new ConcurrentDictionary<Type, IBaseTypeHashCode>();
        
        private readonly Dictionary<Type, ITypePool> _typeBag = new Dictionary<Type, ITypePool>();
        internal readonly bool ConsolidateEntities = Settings.ConsolidateEntities;

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
            
            var pkFields = from x in desc.Columns
                           where x.IsPrimaryKey
                           orderby x.PrimaryKeyOrder
                           select x.MemberName;

            if (!pkFields.Any())
            {
                return entities;
            }

            var pkHasher = _typeHasCodeCache.GetOrAdd(baseType, k =>
            {
                var func = EntityPropertySetter.CreateHashCodeExpression<TBase>(pkFields);
                return new BaseTypeHashCode<TBase>(func);
            });

            var pkTypeHaster = pkHasher as BaseTypeHashCode<TBase>;
            var pkFunc = pkTypeHaster.HashCodeFunc;

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
}
