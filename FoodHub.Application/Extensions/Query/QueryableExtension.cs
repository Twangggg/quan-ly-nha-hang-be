using FoodHub.Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FoodHub.Application.Extensions.Query
{
    public static class QueryableExtension
    {
        /// <summary>
        /// Áp d?ng tìm ki?m toàn c?c (Global Search) trên nhi?u tru?ng d? li?u
        /// S? d?ng Expression Trees d? t?o câu l?nh SQL LIKE d?ng
        /// </summary>
        public static IQueryable<T> ApplyGlobalSearch<T>(
            this IQueryable<T> query,
            string? search,
            List<Expression<Func<T, string?>>> searchableFields)
        {
            if (string.IsNullOrWhiteSpace(search))
                return query;

            var parameter = Expression.Parameter(typeof(T), "x");
            Expression? combinedExpression = null;

            var searchLower = search.ToLower();
            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });

            foreach (var field in searchableFields)
            {
                // Truy c?p vào thu?c tính c?a object (ví d?: x => x.FullName)
                var memberAccess = Expression.Invoke(field, parameter);
                
                // Chuy?n v? ch? thu?ng d? tìm ki?m không phân bi?t hoa thu?ng (Case-insensitive)
                var toLowerCall = Expression.Call(memberAccess, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
                
                // T?o l?nh .Contains() tuong ?ng v?i SQL LIKE '%search%'
                var containsCall = Expression.Call(toLowerCall, containsMethod!, Expression.Constant(searchLower));

                // K?t h?p các di?u ki?n tìm ki?m b?ng toán t? OR (OrElse)
                // Ví d?: FullName.Contains(...) OR Email.Contains(...)
                combinedExpression = combinedExpression == null
                    ? containsCall
                    : Expression.OrElse(combinedExpression, containsCall);
            }

            if (combinedExpression != null)
            {
                var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
                query = query.Where(lambda);
            }

            return query;
        }

        /// <summary>
        /// Áp d?ng b? l?c d?ng (Filtering) t? danh sách chu?i "key:value"
        /// H? tr? l?c theo kho?ng (minPrice, maxPrice) và t? d?ng ép ki?u d? li?u
        /// </summary>
        public static IQueryable<T> ApplyFilters<T>(
            this IQueryable<T> query,
            List<string>? filters,
            Dictionary<string, Expression<Func<T, object?>>> filterMapping)
        {
            if (filters == null || !filters.Any())
                return query;

            foreach (var filter in filters)
            {
                // Tách chu?i filter theo d?nh d?ng "key:value" (ví d?: "status:1", "minPrice:100000")
                var parts = filter.Split(':', 2);
                if (parts.Length != 2) continue;

                var rawKey = parts[0];
                var value = parts[1].Trim();

                string key;
                ExpressionType filterType;

                // 1. Phân tích lo?i toán t? so sánh (Equal, >=, <=) d?a vào ti?n t? min/max
                if (rawKey.StartsWith("min", StringComparison.OrdinalIgnoreCase))
                {
                    key = rawKey.Substring(3).ToLower();
                    filterType = ExpressionType.GreaterThanOrEqual; // >=
                }
                else if (rawKey.StartsWith("max", StringComparison.OrdinalIgnoreCase))
                {
                    key = rawKey.Substring(3).ToLower();
                    filterType = ExpressionType.LessThanOrEqual;    // <=
                }
                else
                {
                    key = rawKey.ToLower();
                    filterType = ExpressionType.Equal;              // ==
                }

                if (filterMapping.TryGetValue(key, out var propertySelector))
                {
                    var parameter = Expression.Parameter(typeof(T), "x");

                    // Trích xu?t thu?c tính th?c t? t? propertySelector (lo?i b? chuy?n d?i ki?u 'object' c?a AutoMapper)
                    Expression memberExpression = propertySelector.Body;
                    if (memberExpression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
                    {
                        memberExpression = unary.Operand;
                    }

                    // Gán l?i tham s? truy c?p d? li?u d? d?m b?o câu l?nh SQL h?p l?
                    var visitor = new ParameterRebinder(propertySelector.Parameters[0], parameter);
                    memberExpression = visitor.Visit(memberExpression);

                    var propertyType = memberExpression.Type;

                    try
                    {
                        // 2. T? d?ng ép ki?u chu?i "value" sang ki?u d? li?u c?a thu?c tính (int, decimal, bool, DateTime, ...)
                        object? convertedValue;
                        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            var underlyingType = Nullable.GetUnderlyingType(propertyType)!;
                            convertedValue = string.IsNullOrEmpty(value) ? null : Convert.ChangeType(value, underlyingType);
                        }
                        else
                        {
                            convertedValue = Convert.ChangeType(value, propertyType);
                        }

                        // 3. T?o bi?u th?c nh? phân (Binary Expression) và thêm vào câu l?nh .Where()
                        var constant = Expression.Constant(convertedValue, propertyType);
                        Expression body = Expression.MakeBinary(filterType, memberExpression, constant);

                        var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
                        query = query.Where(lambda);
                    }
                    catch
                    {
                        // N?u ép ki?u th?t b?i (ví d? nh?p ch? vào tru?ng giá), b? qua filter dó.
                        continue;
                    }
                }
            }

            return query;
        }

        /// <summary>
        /// L?p h? tr? d? "bu?c" l?i tham s? trong Expression Tree
        /// Ð?m b?o t?t c? các bi?u th?c con d?u dùng chung 1 bi?n tham s? 'x'
        /// </summary>
        private class ParameterRebinder : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParameter;
            private readonly ParameterExpression _newParameter;

            public ParameterRebinder(ParameterExpression oldParameter, ParameterExpression newParameter)
            {
                _oldParameter = oldParameter;
                _newParameter = newParameter;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _oldParameter ? _newParameter : base.VisitParameter(node);
            }
        }

        /// <summary>
        /// Áp d?ng s?p x?p da t?ng (Multi-Sorting)
        /// H? tr? d?nh d?ng "+property" ho?c "-property" (gi?m d?n)
        /// </summary>
        public static IQueryable<T> ApplySorting<T>(
            this IQueryable<T> query,
            string? orderBy,
            Dictionary<string, Expression<Func<T, object?>>> mapping,
            Expression<Func<T, object?>> defaultSort)
        {
            // N?u không có yêu c?u s?p x?p, dùng m?c d?nh (thu?ng là s?p x?p theo ID ho?c ngày t?o)
            if (string.IsNullOrWhiteSpace(orderBy))
            {
                return query.OrderBy(defaultSort);
            }

            // Tách danh sách s?p x?p (ví d?: "price,-createdAt" => s?p x?p giá tang d?n, sau dó ngày t?o gi?m d?n)
            var sortItems = orderBy.Split(',', StringSplitOptions.RemoveEmptyEntries);
            IOrderedQueryable<T>? orderedQuery = null;

            foreach (var item in sortItems)
            {
                var trimmedItem = item.Trim();
                var isDescending = trimmedItem.StartsWith("-");
                var propertyName = isDescending ? trimmedItem.Substring(1).ToLower() : trimmedItem.ToLower();

                if (mapping.TryGetValue(propertyName, out var selectedSort))
                {
                    if (orderedQuery == null)
                    {
                        // L?n d?u tiên: Dùng OrderBy
                        orderedQuery = isDescending
                            ? query.OrderByDescending(selectedSort)
                            : query.OrderBy(selectedSort);
                    }
                    else
                    {
                        // Các l?n sau: Dùng ThenBy d? gi? nguyên th? t? c?a các l?n tru?c dó
                        orderedQuery = isDescending
                            ? orderedQuery.ThenByDescending(selectedSort)
                            : orderedQuery.ThenBy(selectedSort);
                    }
                }
            }

            return orderedQuery ?? query.OrderBy(defaultSort);
        }
    }
}
