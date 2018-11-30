using LinqToDB;
using LinqToDB.Mapping;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Utils
{
    public static class IncludeExtensions
    {
        public static async Task<List<T>> ToListAsync<T, U>(this IQueryable<T> query, Expression<Func<T, U>> include, bool isSlowQuery = false) where T : class where U : class
        {
            //TODO
            //Implement iSSlowQuery
            //if(result.Count > 2000 || PK == Composite Key)
            //{ TempTable }
            //else
            //{ In function}


            var dbContext = query.GetDataContext<IDataContext>();
            var schema = GetPropertyParts(include, dbContext.MappingSchema);

            var entities = await query.ToListAsync();
            //need to know the below for when we assign it values back to the parent entity
            var (childQuery, reusableQuery) = ChildEntityQueryBuilder.BuildQueryableForProperty(dbContext, query, include, schema);

            var childEntities = await childQuery.ToListAsync();
            entities = EntityPropertySetter.SetField(entities, childEntities, schema).ToList();

            return entities;
        }



        internal static EntityBuilderSchema GetPropertyParts<TParent, TChild>(Expression<Func<TParent, TChild>> expr, MappingSchema mappingSchema) where TParent : class where TChild : class
        {
            string propertyName = null;
            Type entityType = null;
            Type parentType = null;
            Type propertyType = null;

            if (expr.Body is MethodCallExpression)
            {
                var methodCall = expr.Body as MethodCallExpression;
                var propertyExpr = methodCall.Arguments.FirstOrDefault() as MemberExpression;

                if (propertyExpr == null)
                {
                    propertyExpr = methodCall.Object as MemberExpression;
                }

                propertyName = propertyExpr.Member.Name;
                entityType = typeof(TChild);
                propertyType = propertyExpr.Type;
                parentType = propertyExpr.Expression.Type;

            }
            else if (expr.Body is MemberExpression)
            {
                var propertyExpr = expr.Body as MemberExpression;

                propertyName = propertyExpr.Member.Name;
                entityType = propertyExpr.Type;
                propertyType = propertyExpr.Type;
                parentType = propertyExpr.Expression.Type;
            }


            var parentDesc = mappingSchema.GetEntityDescriptor(parentType);
            var assoc = parentDesc.Associations.Where(x => x.MemberInfo.Name == propertyName).Single();


            return new EntityBuilderSchema(propertyName, entityType, propertyType, parentType, parentDesc, assoc);
        }
    }


    static class SchemaCache
    {
        static class Cache<T> where T : class, IDataContext
        {
            internal static readonly ConcurrentDictionary<MemberInfo, EntityBuilderSchema> DictionaryCache = new ConcurrentDictionary<MemberInfo, EntityBuilderSchema>();
            internal static int MappingSchemaHashCode;
        }
        
        internal static EntityBuilderSchema Get<T>(this T context, MemberInfo member) where T : class, IDataContext
        {
            var schema = new EntityBuilderSchema();
            if(Cache<T>.MappingSchemaHashCode != context.MappingSchema.GetHashCode())
            {
                Cache<T>.DictionaryCache.Clear();
                return null;
            }


            if(Cache<T>.DictionaryCache.TryGetValue(member, out schema))
            {
                return schema;
            }

            return null;            
        }

        internal static bool Set<T>(this T context, PropertyInfo property, EntityBuilderSchema schema) where T : class, IDataContext =>        
            Cache<T>.DictionaryCache.TryAdd(property, schema);            
        
    }

    public static class IncludeHelper
    {        
        public static EntityLoader<TEntity, TProperty> Include<TEntity, TProperty>(this IQueryable<TEntity> query, Expression<Func<TEntity, TProperty>> expr) where TEntity : class where TProperty : class
            => new EntityLoader<TEntity, TProperty>(query, expr, true);       
    }


    public abstract class EntityLoader<TEntity> where TEntity : class
    {
        protected readonly bool _isRootItem;
        protected readonly List<EntityLoader<TEntity>> _propertyEntityLoaders = new List<EntityLoader<TEntity>>();
        protected readonly IQueryable<TEntity> _query;

        public EntityLoader(IQueryable<TEntity> query, bool isRootItem)
        {
            _query = query;
            _isRootItem = isRootItem;
        }
    }

    public class EntityLoader<TEntity, TPreviousProperty> : EntityLoader<TEntity> where TEntity : class where TPreviousProperty : class
    {        
        public EntityLoader(IQueryable<TEntity> query, Expression<Func<TEntity, TPreviousProperty>> expr, bool isRootItem = false) : base(query, isRootItem)
        {        
            //TODO
            //if property is nested then allow for a nested search into the _propertyEntityLoaders to get\create the parent entity loader
            //and add the expr to it

            //also, create a generic version of EntityBuilderSchema so that we can pass items and IQueryable into it and process them properly

            var propertyInfo = GetPropertyInfo(expr);
            var dbContext = query.GetDataContext<Data.DataConnection>();
            dynamic context = dbContext;
            var schema = SchemaCache.Get(context, propertyInfo);
            if (schema == null)
            {
                schema = IncludeExtensions.GetPropertyParts(expr, dbContext.MappingSchema);
                SchemaCache.Set(context, propertyInfo, schema);
            }

        }

        private static PropertyInfo GetPropertyInfo<T, U>(Expression<Func<T, U>> expr) where T : class where U : class
        {
            if (expr.Body is MethodCallExpression)
            {
                var methodCall = expr.Body as MethodCallExpression;
                if (!(methodCall.Arguments.FirstOrDefault() is MemberExpression propertyExpr))
                {
                    propertyExpr = methodCall.Object as MemberExpression;
                }

                return propertyExpr.Member as PropertyInfo;
            }

            if (expr.Body is MemberExpression)
            {
                var propertyExpr = expr.Body as MemberExpression;
                return propertyExpr.Member as PropertyInfo;
            }

            throw new ArgumentException($"Could not get PropertyInfo for {expr.ToString()}");
        }        

        public EntityLoader<TEntity, TProperty> Include<TProperty>(Expression<Func<TEntity, TProperty>> expr) where TProperty : class
        {
            var entityLoader = new EntityLoader<TEntity, TProperty>(_query, expr);
            _propertyEntityLoaders.Add(entityLoader);

            return entityLoader;
        }

        /*
        public EntityLoader<TPreviousProperty, TProperty> ThenInclude<TProperty>(Expression<Func<TPreviousProperty, TProperty>> expr) where TProperty : class
        {            
            return new EntityLoader<TPreviousProperty, TProperty>(_query, expr);
        }
        */
    }
}
