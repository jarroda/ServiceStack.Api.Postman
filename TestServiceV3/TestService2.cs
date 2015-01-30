using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace FluentMigrator.ServiceStack.TestV3
{
    public class TestService2 : Service
    {
        public void Get(GetDeleteOrder order)
        {
        }

        public void Post(OrderRequest contact)
        {
        }

        public void Delete(GetDeleteOrder order)
        {
        }

        public void Put(OrderRequest contact)
        {
        }

        public void Any(GetOrderStatus request)
        {   
        }
    }

    [Route("/order/number/{OrderNumber}", "GET")]
    [Route("/order/{Id}", "GET,DELETE")]
    public class GetDeleteOrder
    {
        public int Id { get; set; }

        public string OrderNumber { get; set; }
    }

    [Route("/order", "PUT")]
    [Route("/order/{Id}", "POST")]
    public class OrderRequest
    {
        public int Id { get; set; }

        public string OrderNumber { get; set; }

        public int ContactId { get; set; }

        public string Description { get; set; }
    }

    [Route("/order/{OrderId}/status")]
    public class GetOrderStatus
    {
        public int OrderId { get; set; }
    }
}