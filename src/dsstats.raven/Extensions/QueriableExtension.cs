using Raven.Client.Documents.Linq;
using System.Linq.Expressions;

namespace dsstats.raven.Extensions;

public static class QueriableExtension
{
    /// <summary>
    /// Builds the Queryable functions using a TSource property name.
    /// </summary>
    public static IRavenQueryable<T> CallOrderedQueryable<T>(this IRavenQueryable<T> query, string methodName, string propertyName,
            IComparer<object>? comparer = null)
    {
        var param = Expression.Parameter(typeof(T), "x");

        var body = propertyName.Split('.').Aggregate<string, Expression>(param, Expression.PropertyOrField);

        return comparer != null
            ? (IRavenQueryable<T>)query.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    methodName,
                    new[] { typeof(T), body.Type },
                    query.Expression,
                    Expression.Lambda(body, param),
                    Expression.Constant(comparer)
                )
            )
            : (IRavenQueryable<T>)query.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    methodName,
                    new[] { typeof(T), body.Type },
                    query.Expression,
                    Expression.Lambda(body, param)
                )
            );
    }

    public static IRavenQueryable<T> AppendOrderBy<T>(this IRavenQueryable<T> query, string propertyName)
    => query.Expression.Type == typeof(IOrderedQueryable<T>)
    ? ((IRavenQueryable<T>)query).CallOrderedQueryable("ThenBy", propertyName)
    : query.CallOrderedQueryable("OrderBy", propertyName);

    public static IRavenQueryable<T> AppendOrderByDescending<T>(this IRavenQueryable<T> query, string propertyName)
        => query.Expression.Type == typeof(IOrderedQueryable<T>)
            ? ((IRavenQueryable<T>)query).CallOrderedQueryable("ThenByDescending", propertyName)
            : query.CallOrderedQueryable("OrderByDescending", propertyName);
}
