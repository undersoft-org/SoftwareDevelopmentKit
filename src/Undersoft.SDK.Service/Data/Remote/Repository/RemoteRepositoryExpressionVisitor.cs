using System.Linq.Expressions;

namespace Undersoft.SDK.Service.Data.Remote.Repository;

internal class RemoteRepositoryExpressionVisitor<T> : ExpressionVisitor where T : class, IOrigin, IInnerProxy
{
    private readonly IQueryable<T> newRoot;

    public RemoteRepositoryExpressionVisitor(IQueryable<T> newRoot)
    {
        this.newRoot = newRoot;
    }

    protected override Expression VisitConstant(ConstantExpression node) =>
        node.Type == typeof(RemoteRepository<T>)
            ? Expression.Constant(newRoot)
            : node;
}
