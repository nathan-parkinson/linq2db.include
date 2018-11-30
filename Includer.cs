using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LinqToDB.Utils
{
    public class MemberDetails
    {
        public MemberInfo MemberInfo { get; set; }
        public bool IsInheritedType { get; set; }
        public Type InheritedType { get; set; }
    }

    public static class Includer<TSource>
    {
        public static IReadOnlyList<MemberDetails> Get<TResult, TContext>(TContext context, Expression<Func<TSource, TResult>> expression) where TContext : class, IDataContext
        {
            var visitor = new PropertyVisitor<TContext>(context);
            visitor.Visit(expression.Body);
            visitor.Path.Reverse();
            return visitor.Path;
        }

        class PropertyVisitor<TContext> : ExpressionVisitor where TContext : class, IDataContext
        {
            internal readonly List<MemberDetails> Path = new List<MemberDetails>();
            private readonly TContext _context;

            public PropertyVisitor(TContext context)
            {
                _context = context;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                //if schema does not already exist then creste it now
                if(SchemaCache.Get(_context, node.Member) == null)
                {
                    
                   // IncludeExtensions.GetPropertyParts()
                }


                Path.Add(new MemberDetails { MemberInfo = node.Member });
                return base.VisitMember(node);
            }

            protected override Expression VisitUnary(UnaryExpression node)
            {

                switch (node.NodeType)
                {
                    case ExpressionType.Convert:
                    case ExpressionType.ConvertChecked:
                        var details = Path.LastOrDefault();
                        if (details != null)
                        {
                            details.IsInheritedType = true;
                            details.InheritedType = node.Type;
                        }
                        break;
                    default:
                        break;
                }
                return base.VisitUnary(node);
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Method.Name == nameof(Enumerable.OfType) && node.Method.DeclaringType == typeof(Enumerable))
                {
                    var details = Path.LastOrDefault();
                    if (details != null)
                    {
                        details.IsInheritedType = true;
                        details.InheritedType = node.Method.GetGenericArguments().First();
                    }
                }
                return base.VisitMethodCall(node);
            }

        }
    }
}
