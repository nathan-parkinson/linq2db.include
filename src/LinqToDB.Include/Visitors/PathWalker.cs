using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Include
{
    class PathWalker : ExpressionVisitor
    {
        private List<MemberInfo> _members = new List<MemberInfo>();

        private PathWalker()
        {

        }

        internal static List<string> GetPath<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> expr)
            where TEntity : class
            where TProperty : class
        {
            var walker = new PathWalker();
            walker.Visit(expr);
            walker._members.Reverse();

            return walker._members.Select(x => x.Name).ToList();
        }

        internal static List<string> GetPath(MemberExpression expr)
        {
            var walker = new PathWalker();
            walker.Visit(expr);
            walker._members.Reverse();

            return walker._members.Select(x => x.Name).ToList();
        }


        internal static List<MemberInfo> GetMembers<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> expr)
            where TEntity : class
            where TProperty : class
        {
            var walker = new PathWalker();
            walker.Visit(expr);
            walker._members.Reverse();

            return walker._members;
        }

        internal static List<MemberInfo> GetMembers(MemberExpression expr)
        {
            var walker = new PathWalker();
            walker.Visit(expr);
            walker._members.Reverse();

            return walker._members;
        }



        protected override Expression VisitMember(MemberExpression node)
        {
            _members.Add(node.Member);
            return base.VisitMember(node);
        }
    }

}
