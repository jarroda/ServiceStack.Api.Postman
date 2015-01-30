using System;
using System.Collections.Generic;

namespace ServiceStack.Api.Postman
{
    public static class PostmanExtensions
    {
        public static string ReplaceVariables(this string route)
        {
            return route.Replace('{', ':').Replace("}", string.Empty);
        }

        public static string FormatLabel(this string template, Type requestType, string path)
        {
            return template.ToLower()
                .Replace("{type}", requestType.Name)
                .Replace("{route}", path);
        }

        public static U GetValueOrDefault<T, U>(this Dictionary<T, U> dictionary, T key)
        {
            U value = default(U);
            dictionary.TryGetValue(key, out value);
            return value;
        }
    }
}