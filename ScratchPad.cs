using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace App.Domain.Repository.Mapping
{
    class ScratchPad : DataConnection
    {
        /// <summary>
        /// Will not work with composite primary keys or composite foreign keys
        /// </summary>
        /// <typeparam name="TChild"></typeparam>
        /// <param name="entities"></param>
        /// <param name="childProperty"></param>
        /// <returns></returns>
        private ICollection<TParent> IncludePrivateForNonCompositeKeys<TParent, TChild>(ICollection<TParent> entities, Expression<Func<TParent, ICollection<TChild>>> childProperty)
            where TParent : class
            where TChild : class
        {
            var childType = ((MemberExpression)childProperty.Body).Type.GenericTypeArguments.Single();
            var childDesc = MappingSchema.GetEntityDescriptor(childType);
            var parentPK = childDesc.Columns.Single(x => x.IsPrimaryKey);


            var result = this.GetType()
                .GetMethod(nameof(IncludePrivate), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .MakeGenericMethod(typeof(TParent), typeof(TChild), parentPK.MemberType)
                .Invoke(this, new object[] { entities, childProperty, parentPK.MemberName }) as ICollection<TParent>;

            return result;
        }

        //TODO make the code bleow handle composite pk and fk
        //  ToLookup needs to be able to handle composite fk.  Is it still feasible to use ToLookup in this case?
        //replace use of Temp<> with TParent and...
        //  create mapping on temp table to only the fk\pk fields
        //TODO Add ExpressionPredicate usage
        //Create a version to accept IQueryable 
        //Create Concurrent object to store Lambda once built
        //TODO Allow this to work with 1 to 1 relationships


        //TODO make sure it's all fine if ParentPK type is not the same as Child FK Type. e.g. int to long
        private ICollection<TParent> IncludePrivateForNonCompositeKeys<TParent, TChild, TParentPk>(ICollection<TParent> people, Expression<Func<TParent, ICollection<TChild>>> childProperty, string parentPkName)
            where TParent : class
            where TChild : class
            where TParentPk : struct
        {

            TempTable<Temp<TParentPk>> tempTable = null;
            try
            {
                var personDesc = MappingSchema.GetEntityDescriptor(typeof(TParent));
                var childPropertyName = ((MemberExpression)childProperty.Body).Member.Name;
                var childType = ((MemberExpression)childProperty.Body).Type.GenericTypeArguments.Single();
                var assoc = personDesc.Associations.Where(x => x.MemberInfo.Name == childPropertyName).Single();
                var childDesc = MappingSchema.GetEntityDescriptor(childType);

                var childExpr = CreateExpressionProperty<TChild, TParentPk>(assoc.OtherKey.Single());
                Func<TChild, TParentPk> fkExpr = childExpr.Compile();

                var pkExpr = CreateExpressionProperty<TParent, TParentPk>(assoc.ThisKey.Single()).Compile();


                IQueryable<TChild> query = null;
                //if items are materialised and pk field is 1 column
                if (people.Count < 1)// 2000)
                {
                    var list = people.Select(pkExpr).Distinct().ToList();
                    query = GetTable<TChild>().Where(x => list.Contains(Sql.Property<TParentPk>(x, assoc.OtherKey.Single())));
                }
                else
                {
                    var equalsExpr = BuildEqualExpression<TChild, Temp<TParentPk>, TParentPk>(childExpr, p => p.PK);
                    tempTable = CreateTempTable(people.Select(x => new Temp<TParentPk> { PK = pkExpr(x) }));
                    query = GetTable<TChild>().InnerJoin<TChild, Temp<TParentPk>, TChild>(tempTable, equalsExpr, (s, t) => s);
                }
                var pkChild = childDesc.Columns.Single(x => x.IsPrimaryKey).MemberName;

                //TODO create expressions to replace the use of SQl.Property below

                Expression<Func<TChild, bool>> expr = o => query.Where(c => Sql.Property<int>(c, pkChild) == Sql.Property<int>(o, pkChild)).Any();


                var finalQuery = GetTable<TChild>().Where(expr);
                var orders = finalQuery.ToLookup(fkExpr);

                var childPropertyCompiled = childProperty.Compile();
                foreach (var entity in people)
                {
                    var collection = childPropertyCompiled(entity);
                    foreach (var childEntity in orders[pkExpr(entity)])
                    {
                        collection.Add(childEntity);
                    }
                }


                return people;
            }
            finally
            {
                ((IDisposable)tempTable)?.Dispose();
            }
        }



        private IQueryable<TParent> Include<TParent, TChild>(IQueryable<TParent> entities, Expression<Func<TParent, ICollection<TChild>>> childProperty)
          where TParent : class
          where TChild : class
        {
            var childType = ((MemberExpression)childProperty.Body).Type.GenericTypeArguments.Single();
            var childDesc = MappingSchema.GetEntityDescriptor(childType);
            var parentPK = childDesc.Columns.Single(x => x.IsPrimaryKey);


            var result = this.GetType()
                .GetMethod(nameof(IncludePrivate), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .MakeGenericMethod(typeof(TParent), typeof(TChild), parentPK.MemberType)
                .Invoke(this, new object[] { entities, childProperty, parentPK.MemberName }) as ICollection<TParent>;

            return entities;
        }


        //TODO make sure it's all fine if ParentPK type is not the same as Child FK Type. e.g. int to long
        private IQueryable<TParent> IncludePrivate<TParent, TChild, TParentPk>(IQueryable<TParent> sourceQuery, Expression<Func<TParent, ICollection<TChild>>> childProperty, string parentPkName)
            where TParent : class
            where TChild : class
            where TParentPk : struct
        {

            TempTable<Temp<TParentPk>> tempTable = null;
            try
            {
                var childPropertyName = ((MemberExpression)childProperty.Body).Member.Name;

                var parentDesc = MappingSchema.GetEntityDescriptor(typeof(TParent));
                var assoc = parentDesc.Associations.Where(x => x.MemberInfo.Name == childPropertyName).Single();

                var childType = ((MemberExpression)childProperty.Body).Type.GenericTypeArguments.Single();
                var childDesc = MappingSchema.GetEntityDescriptor(childType);

                var childExpr = CreateExpressionProperty<TChild, TParentPk>(assoc.OtherKey.Single());
                Func<TChild, TParentPk> fkExpr = childExpr.Compile();

                var pkExpr = CreateExpressionProperty<TParent, TParentPk>(assoc.ThisKey.Single()).Compile();


                IQueryable<TChild> query = null;

                var joinExpr = BuildJoinExpression<TParent, TChild>(assoc);
                var subSelect = sourceQuery.Join(GetTable<TChild>(), SqlJoinType.Inner, joinExpr, (p, b) => b);
                query = subSelect.Distinct();


                var res = BuildSelectLambda<TChild>(assoc.OtherKey);
                var test = query.ToList().OrderBy(res.Compile());

                var orders = query.ToLookup(fkExpr);

                var childPropertyCompiled = childProperty.Compile();
                var items = sourceQuery.ToList();
                foreach (var entity in items)
                {
                    var collection = childPropertyCompiled(entity);
                    foreach (var childEntity in orders[pkExpr(entity)])
                    {
                        collection.Add(childEntity);
                    }
                }


                return sourceQuery;
            }
            finally
            {
                ((IDisposable)tempTable)?.Dispose();
            }
        }



        private Expression<Func<TChild, TParent, bool>> BuildSelfJoinWhereExpression<TChild, TParent>(IEnumerable<ColumnDescriptor> columns) where TChild : TParent
        {
            ParameterExpression childParam = Expression.Parameter(typeof(TChild), "c");
            ParameterExpression parentParam = Expression.Parameter(typeof(TParent), "p");


            BinaryExpression previousExpr = null;
            foreach (var column in columns)
            {
                var childProperty = Expression.Property(childParam, column.MemberName);
                var parentProperty = Expression.Property(parentParam, column.MemberName);
                var equals = Expression.Equal(childProperty, parentProperty);
                if (previousExpr == null)
                {
                    previousExpr = equals;
                }
                else
                {
                    previousExpr = Expression.AndAlso(previousExpr, equals);
                }
            }

            var expr1 = Expression.Lambda<Func<TChild, TParent, bool>>(previousExpr, childParam, parentParam);
            return expr1;
        }


        private Expression<Func<TChild, TParent, bool>> BuildJoinExpression<TChild, TParent>(AssociationDescriptor assoc)
        {
            ParameterExpression childParam = Expression.Parameter(typeof(TChild), "c");
            ParameterExpression parentParam = Expression.Parameter(typeof(TParent), "p");

            BinaryExpression previousExpr = null;
            for (int i = 0; i < assoc.ThisKey.Length; i++)
            {
                MemberExpression childProperty = Expression.Property(childParam, assoc.OtherKey[i]);
                MemberExpression parentProperty = Expression.Property(parentParam, assoc.ThisKey[i]);
                var equals = Expression.Equal(childProperty, parentProperty);
                if (previousExpr == null)
                {
                    previousExpr = equals;
                }
                else
                {
                    previousExpr = Expression.AndAlso(previousExpr, equals);
                }
            }

            var expr = Expression.Lambda<Func<TChild, TParent, bool>>(previousExpr, childParam, parentParam);
            return expr;
        }


        private TempTable<T> CreateTempTable<T>(IEnumerable<T> entities)
        {
            var table = new TempTable<T>(this, Guid.NewGuid().ToString());
            table.BulkCopy(entities);
            return table;
        }


        private TempTable<TParent> CreateTempTable2<TParent, TChild>(IEnumerable<TParent> entities, string fkName) where TParent : class
        {
            //T = TParent
            var parentDesc = this.MappingSchema.GetEntityDescriptor(typeof(TParent));
            var childDesc = this.MappingSchema.GetEntityDescriptor(typeof(TChild));
            var tableName = Guid.NewGuid().ToString();
            var table = new TempTable<TParent>(this, tableName);

            var cols = parentDesc.Columns.Where(x => x.IsPrimaryKey);
            var res = BuildSelectLambda<TParent>(cols);


            table.BulkCopy(entities.Select(res.Compile()));
            return table;
        }

        static Expression<Func<T, T>> BuildSelectLambda<T>(IEnumerable<ColumnDescriptor> properties) where T : class
        {
            var columnSelect = new List<MemberBinding>();
            var createdType = typeof(T);

            ParameterExpression param = Expression.Parameter(typeof(T), "x");
            var ctor = Expression.New(createdType);
            foreach (var property in properties)
            {
                var propertyExpr = Expression.Property(param, property.MemberName);
                var displayValueProperty = createdType.GetProperty(property.MemberName);
                var displayValueAssignment = Expression.Bind(displayValueProperty, propertyExpr);
                columnSelect.Add(displayValueAssignment);
            }

            var memberInit = Expression.MemberInit(ctor, columnSelect.ToArray());

            return Expression.Lambda<Func<T, T>>(memberInit, param);
        }


        static Expression<Func<T, T>> BuildSelectLambda<T>(IEnumerable<string> properties) where T : class
        {
            var columnSelect = new List<MemberBinding>();
            var createdType = typeof(T);

            ParameterExpression param = Expression.Parameter(typeof(T), "x");
            var ctor = Expression.New(createdType);
            foreach (var property in properties)
            {
                var propertyExpr = Expression.Property(param, property);
                var displayValueProperty = createdType.GetProperty(property);
                var displayValueAssignment = Expression.Bind(displayValueProperty, propertyExpr);
                columnSelect.Add(displayValueAssignment);
            }

            var memberInit = Expression.MemberInit(ctor, columnSelect.ToArray());

            return Expression.Lambda<Func<T, T>>(memberInit, param);
        }




        public static Expression<Func<T, U>> CreateExpressionProperty<T, U>(string propertyName)
        {
            ParameterExpression x = Expression.Parameter(typeof(T), "x");
            MemberExpression body = Expression.Property(x, propertyName);
            var expr = Expression.Lambda<Func<T, U>>(body, x);

            return expr;
        }


        public static Expression<Func<TEntity1, TEntity2, bool>> BuildEqualExpression<TEntity1, TEntity2, TValue>(Expression<Func<TEntity1, TValue>> sourceExpr, Expression<Func<TEntity2, TValue>> comparerExpr)
        {
            var resultExpr = Expression.Lambda<Func<TEntity1, TEntity2, bool>>(Expression.Equal(sourceExpr.Body, comparerExpr.Body), sourceExpr.Parameters.Union(comparerExpr.Parameters));
            return resultExpr;
        }


        //?Does this create anonymous object 
        public static Expression<Func<U, T>> BuildLambda<T, U>(Expression<Func<T, U>> property) where T : class
        {
            var createdType = typeof(T);
            var displayValueParam = Expression.Parameter(typeof(U), "displayValue");
            var ctor = Expression.New(createdType);
            var displayValueProperty = createdType.GetProperty("DisplayValue");
            var displayValueAssignment = Expression.Bind(displayValueProperty, displayValueParam);

            var memberInit = Expression.MemberInit(ctor, displayValueAssignment);

            return Expression.Lambda<Func<U, T>>(memberInit, displayValueParam);
        }
    }

    public class Temp<T>
    {
        public T PK { get; set; }
    }

}