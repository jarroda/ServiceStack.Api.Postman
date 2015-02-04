using System.Collections.Generic;

namespace ServiceStack.Api.Postman.Types
{
    public class PostmanFolder
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public List<string> Order { get; set; } 

        public string CollectionName { get; set; }

        public string CollectionId { get; set; }

    }
}
