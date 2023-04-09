using RabbitMQ.Client;

namespace OrderService
{
    public class RabbitMQConfig
    {
        public string HostName { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public ConnectionFactory GetConnectionFactory()
        {
            HostName = "inpdy-d-0019";
            Port = 5672;
            UserName= "guest";
            Password = "guest";

            return new ConnectionFactory()
            {
                HostName = HostName,
                Port = Port,
                UserName = UserName,
                Password = Password
            };
        }
    }
}
