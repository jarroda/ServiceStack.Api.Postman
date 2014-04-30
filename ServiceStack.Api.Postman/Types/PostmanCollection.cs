using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.Api.Postman.Types
{
    public class PostmanCollection
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public long Timestamp { get; set; }

        public PostmanRequest[] Requests { get; set; }
    }
}