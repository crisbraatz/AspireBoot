using System.Linq.Expressions;
using System.Reflection;
using AspireBoot.Domain;

namespace AspireBoot.Infrastructure.Extensions;

public static class QueryableExtension
{
    public static IQueryable<T> OrderBy<T>(this IQueryable<T> queryable, string? propertyName, bool ascending)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            return queryable;

        Type type = typeof(T);

        PropertyInfo? propertyInfo = type.GetProperty(
            AppSettings.AppLanguage.TextInfo.ToTitleCase(propertyName),
            BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
        if (propertyInfo is null)
            return queryable;

        MethodInfo methodInfo = typeof(Queryable)
            .GetMethods()
            .Where(x => x.Name == (ascending ? "OrderBy" : "OrderByDescending") && x.IsGenericMethodDefinition)
            .Single(x => x.GetParameters().ToList().Count is 2)
            .MakeGenericMethod(type, propertyInfo.PropertyType);

        ParameterExpression parameterExpression = Expression.Parameter(type, "x");

        return methodInfo.Invoke(
            methodInfo,
            [
                queryable,
                Expression.Lambda(Expression.Property(parameterExpression, propertyName), parameterExpression)
            ]) as IQueryable<T> ?? queryable;
    }

    public static IQueryable<T> PaginateBy<T>(this IQueryable<T> queryable, int page, int size) =>
        queryable.Skip((page - 1) * size).Take(size);
}
