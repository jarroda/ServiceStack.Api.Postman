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