using System.Linq.Expressions;

namespace pax.dsstats.dbng.Extensions;

public static class QueriableExtension
{
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
