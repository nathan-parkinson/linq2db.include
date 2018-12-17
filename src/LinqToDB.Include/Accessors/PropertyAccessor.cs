using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;


namespace LinqToDB.Include
{
    class PropertyAccessor<TClass, TProperty> : PropertyAccessor<TClass> where TClass : class where TProperty : class
    {
        private readonly int _memberInfoHashCode;

        public PropertyAccessor(MemberExpression exp, MappingSchema mappingSchema)
        {
            _memberInfoHashCode = exp.Member.GetHashCode();
            _propertyName = exp.Member.Name;
            _declaringType = exp.Member.DeclaringType;
            _memberType = exp.Type;
            _memberEntityType = typeof(TProperty);
            _isMemberTypeICollection = _memberType.IsICollection();
            _isMemberEntityTypeIEnumerable = _memberEntityType.IsIEnumerable();

            ChildEntityDescriptor = mappingSchema.GetEntityDescriptor(_memberEntityType);
            ParentEntityDescriptor = mappingSchema.GetEntityDescriptor(DeclaringType);
            AssociationDescriptor = ParentEntityDescriptor.Associations.Single(x => x.MemberInfo.Name == PropertyName);
        }

        public override HashSet<IPropertyAccessor> Properties
        {
            get => new HashSet<IPropertyAccessor>(PropertiesOfTClass);
        }
        
        public HashSet<IPropertyAccessor<TProperty>> PropertiesOfTClass
        {
            get;
        } = new HashSet<IPropertyAccessor<TProperty>>();

        private IQueryable<TProperty> GetReusableQuery(IQueryable<TClass> query)
        {
            //get cache func
            var customQueryBuilder = EntityMapOverride.Get<TClass, TProperty>(_memberInfoHashCode);

            if (customQueryBuilder?.ReusableQueryBuilder != null)
            {
                return customQueryBuilder.ReusableQueryBuilder(query, _propertyFilter);
            }
            
            var reusableQuery = PropertyQueryBuilder.BuildReusableQueryableForProperty(query, this);
            if (_propertyFilter != null)
            {
                reusableQuery = reusableQuery.Where(_propertyFilter);
            }
            return reusableQuery;
        }

        private List<TProperty> ExecuteQuery(IQueryable<TClass> query)
        {
            //get cache func
            var customQueryBuilder = EntityMapOverride.Get<TClass, TProperty>(_memberInfoHashCode);
            
            if (customQueryBuilder?.QueryExecuter != null)
            {
                return customQueryBuilder.QueryExecuter(query, _propertyFilter);
            }

            //get query
            var propertyQuery = PropertyQueryBuilder.BuildQueryableForProperty(query, this);
            if (_propertyFilter != null)
            {
                propertyQuery = propertyQuery.Where(_propertyFilter);
            }

            //run query into list
            var propertyEntities = propertyQuery.ToList();
            return propertyEntities;

        }

        internal override void Load(List<TClass> entities, IQueryable<TClass> query)
        {            
            //get query
            var propertyEntities = ExecuteQuery(query);

            IQueryable<TProperty> reusableQuery = null;
            //run nested properties
            //TODO make sure the base member is always executed first in case
            //where inheritance is used
            foreach (var propertyAccessor in PropertiesOfTClass.OrderBy(x => x.PropertyName))
            {
                if (reusableQuery == null)
                {
                    reusableQuery = GetReusableQuery(query);
                }

                if (propertyAccessor is PropertyAccessor<TProperty> accessorImpl)
                {
                    accessorImpl.Load(propertyEntities, reusableQuery);
                }
                else if (MemberEntityType.IsAssignableFrom(propertyAccessor.DeclaringType))
                {
                    dynamic dynamicAccessor = propertyAccessor;
                    LoadForInheritedType(dynamicAccessor, propertyEntities, reusableQuery);
                }
                else
                {                    
                    throw new PropertyAccessorNotFoundException($"PropertyAccessor<{typeof(TProperty).Name}> not found");
                }
            }

            //set values to entities           
            //TODO change this to a cached Func
            this.SetField(entities, propertyEntities);
        }

        private Expression<Func<TProperty, bool>> _propertyFilter;
        internal void AddFilter(Expression<Func<TProperty, bool>> expr)
        {
            if (_propertyFilter == null)
            {
                _propertyFilter = expr;
                return;
            }

            _propertyFilter = AddToExpression(_propertyFilter, expr);
        }

        private static Expression<Func<TProperty, bool>> AddToExpression(Expression<Func<TProperty, bool>> expr1,
                                                       Expression<Func<TProperty, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<TProperty, bool>>
                  (Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
        }

        private static void LoadForInheritedType<T>(IPropertyAccessor<T> accessor, List<TProperty> propertyEntities, IQueryable<TProperty> query)
            where T : class
        {
            var accessorImpl = (PropertyAccessor<T>)accessor;

            var entitiesOfType = propertyEntities.OfType<T>().ToList();
            var queryOfType = query.OfType<T>();

            accessorImpl.Load(entitiesOfType, queryOfType);
        }

        internal AssociationDescriptor AssociationDescriptor { get; }
        internal EntityDescriptor ParentEntityDescriptor { get; }
        internal EntityDescriptor ChildEntityDescriptor { get; }
    }
}
