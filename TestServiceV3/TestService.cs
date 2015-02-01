using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace FluentMigrator.ServiceStack.TestV3
{
    public class TestService : Service
    {
        public object Get(Contact contact)
        {
            return null;
        }

        public object Post(Contact contact)
        {
            return null;
        }

        public object Delete(Contact contact)
        {
            return null;
        }

        public object Put(Contact contact)
        {
            return null;
        }
    }

    [Route("/contacts", "GET,PUT")]
    [Route("/contacts/{Id}", "GET,POST,DELETE")]
    public class Contact
    {
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }
    }
}