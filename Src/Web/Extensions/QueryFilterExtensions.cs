using System.Linq.Expressions;

namespace TeraLinkaMSDocEditorApi.Web.Extensions;

// Define a static class to hold extension methods for IQueryable
public static class QueryFilterExtensions
{
    // Define an extension method to filter data based on the provided query string
    public static IQueryable<T> FilterFromQueryString<T>(
        this IQueryable<T> source,
        string queryString
    )
    {
        // Split the query string into individual filters based on '^' delimiter
        var filters = queryString.Split('^', StringSplitOptions.RemoveEmptyEntries);

        foreach (var filter in filters)
        {
            // Split each filter into column name and filter value
            var parts = filter.Split('=', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                continue; // Skip if the filter format is incorrect

            var propertyName = parts[0];
            var propertyValue = parts[1];

            // Build a lambda expression to filter the data
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, propertyName);
            var constant = Expression.Constant(propertyValue);
            var equals = Expression.Equal(property, constant);
            var lambda = Expression.Lambda<Func<T, bool>>(equals, parameter);

            // Apply the filter to the IQueryable
            source = source.Where(lambda);
        }

        return source;
    }
}
