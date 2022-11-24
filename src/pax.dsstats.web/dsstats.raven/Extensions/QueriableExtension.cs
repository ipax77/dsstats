using Raven.Client.Documents.Linq;
using System.Linq.Expressions;

namespace dsstats.raven.Extensions;

public static class QueriableExtension
{
    /// <summary>
    /// Builds the Queryable functions using a TSource property name.
    /// </summary>
    public static IRavenQueryable<T> CallRavenOrderedQueryable<T>(this IRavenQueryable<T> query, string methodName, string propertyName,
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

    public static IRavenQueryable<T> RavenAppendOrderBy<T>(this IRavenQueryable<T> query, string propertyName)
    => query.Expression.Type == typeof(IOrderedQueryable<T>)
    ? ((IRavenQueryable<T>)query).CallRavenOrderedQueryable("ThenBy", propertyName)
    : query.CallRavenOrderedQueryable("OrderBy", propertyName);

    public static IRavenQueryable<T> RavenAppendOrderByDescending<T>(this IRavenQueryable<T> query, string propertyName)
        => query.Expression.Type == typeof(IOrderedQueryable<T>)
            ? ((IRavenQueryable<T>)query).CallRavenOrderedQueryable("ThenByDescending", propertyName)
            : query.CallRavenOrderedQueryable("OrderByDescending", propertyName);

    /// <summary>
    /// Builds the Queryable functions using a TSource property name.
    /// </summary>
    public static IOrderedQueryable<T> CallOrderedQueryable<T>(this IQueryable<T> query, string methodName, string propertyName,
            IComparer<object>? comparer = null)
    {
        var param = Expression.Parameter(typeof(T), "x");

        var body = propertyName.Split('.').Aggregate<string, Expression>(param, Expression.PropertyOrField);

        return comparer != null
            ? (IOrderedQueryable<T>)query.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    methodName,
                    new[] { typeof(T), body.Type },
                    query.Expression,
                    Expression.Lambda(body, param),
                    Expression.Constant(comparer)
                )
            )
            : (IOrderedQueryable<T>)query.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    methodName,
                    new[] { typeof(T), body.Type },
                    query.Expression,
                    Expression.Lambda(body, param)
                )
            );
    }

    public static IOrderedQueryable<T> AppendOrderBy<T>(this IQueryable<T> query, string propertyName)
    => query.Expression.Type == typeof(IOrderedQueryable<T>)
    ? ((IOrderedQueryable<T>)query).CallOrderedQueryable("ThenBy", propertyName)
    : query.CallOrderedQueryable("OrderBy", propertyName);

    public static IOrderedQueryable<T> AppendOrderByDescending<T>(this IQueryable<T> query, string propertyName)
        => query.Expression.Type == typeof(IOrderedQueryable<T>)
            ? ((IOrderedQueryable<T>)query).CallOrderedQueryable("ThenByDescending", propertyName)
            : query.CallOrderedQueryable("OrderByDescending", propertyName);
}
