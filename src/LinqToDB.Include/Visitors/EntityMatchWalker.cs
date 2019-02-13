using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Include
{
    public class MatchWalker : ExpressionVisitor
    {
        private bool _ignore = false;
        private bool _isNot = false;

        private Expression _expression;

        private MatchWalker()
        {

        }

        public static Expression ExtractKeyNodes(Expression expression)
        {
            var walker = new MatchWalker();
            walker.Visit(expression);
            return walker._expression;
        }

        private static ExpressionType[] _validAccessors = { ExpressionType.MemberAccess, ExpressionType.Constant };        

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var ignoreAtStart = _ignore;

            _ignore = _ignore || node.NodeType == ExpressionType.OrElse;

            if(!_ignore && !_isNot)
            {
                if(node.NodeType == ExpressionType.Equal && 
                    _validAccessors.Contains(node.Right.NodeType) &&
                    _validAccessors.Contains(node.Left.NodeType))
                {
                    _expression = _expression == null ? node : Expression.AndAlso(_expression, node);
                }
            }
            
            var returnVal = base.VisitBinary(node);

            if(!ignoreAtStart && _ignore)
            {
                _ignore = false;
            }

            return returnVal;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            var isNotAtStart = _isNot;

            _isNot = _isNot || node.NodeType == ExpressionType.Not;

            var returnVal = base.VisitUnary(node);

            if (!isNotAtStart && _isNot)
            {
                _isNot = false;
            }

            return returnVal;
        }

        public override Expression Visit(Expression node)
        {
            return base.Visit(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            return base.VisitMember(node);
        }
        
        protected override Expression VisitConditional(ConditionalExpression node)
        {
            return base.VisitConditional(node);
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            return base.VisitMemberAssignment(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return base.VisitParameter(node);
        }

        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            return base.VisitTypeBinary(node);
        }
    }

}
