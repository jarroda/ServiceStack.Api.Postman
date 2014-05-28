using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.Api.Postman
{
    public static class PostmanExtensions
    {
        public static string ReplaceVariables(this string route)
        {
            return route.Replace('{', ':').Replace("}", string.Empty);
        }
    }
}