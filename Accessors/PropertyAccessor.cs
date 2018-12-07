using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;


namespace LinqToDB.Utils
{
    class PropertyAccessor<TClass, TProperty> : PropertyAccessor<TClass> where TClass : class where TProperty : class
    {
        public PropertyAccessor(MemberExpression exp, MappingSchema mappingSchema)
        {
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

        //TODO ?decide whether or not to remove this property
        public HashSet<IPropertyAccessor<TProperty>> PropertiesOfTClass
        {
            get;
        } = new HashSet<IPropertyAccessor<TProperty>>();


        internal override void Load(List<TClass> entities, IQueryable<TClass> query)
        {
            //TODO need to check if this is inherited member
            //perhaps sort by property name and checkl if is inherited and if property is already loaded


            //get query
            //TODO Change this to get a simpler query for execution and create another method to create a 
            //reusable query for nested properties
            var propertyQuery = PropertyQueryBuilder.BuildQueryableForProperty(query, this);
            if (propertyFilter != null)
            {
                propertyQuery = propertyQuery.Where(propertyFilter);
            }


            //run query into list
            var propertyEntities = propertyQuery.ToList();

            IQueryable<TProperty> reusableQuery = null;
            //run nested properties
            foreach (var propertyAccessor in PropertiesOfTClass.OrderBy(x => x.PropertyName))
            {
                if (reusableQuery == null)
                {
                    reusableQuery = PropertyQueryBuilder.BuildReusableQueryableForProperty(query, this);
                    if (propertyFilter != null)
                    {
                        reusableQuery = reusableQuery.Where(propertyFilter);
                    }
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
                    //TODO Create own exception type and ad a proper message
                    throw new Exception();
                }
            }

            //set values to entities           
            //TODO change this to a cached Func
            this.SetField(entities, propertyEntities);
        }

        private Expression<Func<TProperty, bool>> propertyFilter;
        internal void AddFilter(Expression<Func<TProperty, bool>> expr)
        {
            if (propertyFilter == null)
            {
                propertyFilter = expr;
                return;
            }

            propertyFilter = AddToExpression(propertyFilter, expr);
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
