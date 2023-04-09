using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly RabbitMQConfig _rabbitMQConfig;

        public OrderController(RabbitMQConfig rabbitMQConfig)
        {
            _rabbitMQConfig = rabbitMQConfig;
        }

        [HttpPost]
        public ActionResult CreateOrder([FromBody] OrderModel order)
        {
            // Create order here
            // ...


            // Publish message to RabbitMQ queue
            using (var connection = _rabbitMQConfig.GetConnectionFactory().CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var queueName = "order_queue";
                channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
                var message = JsonConvert.SerializeObject(order);
                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(exchange: "food.fanout", routingKey: queueName, basicProperties: null, body: body);
            }

            return Ok();
        }
    }
}
