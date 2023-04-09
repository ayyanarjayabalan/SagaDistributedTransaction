namespace OrderService
{
    using System;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;

    public class OrderQueueHostedService : BackgroundService
    {
        private readonly RabbitMQConfig _rabbitMQConfig;
        private readonly ILogger<OrderQueueHostedService> _logger;

        public OrderQueueHostedService(RabbitMQConfig rabbitMQConfig, ILogger<OrderQueueHostedService> logger)
        {
            _rabbitMQConfig = rabbitMQConfig;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var connection = _rabbitMQConfig.GetConnectionFactory().CreateConnection();
            var channel = connection.CreateModel();
            string[] queues = new string[] {"delivery_queue" };
            foreach (var queueName in queues)
            {
                channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    OrderModel? order = JsonConvert.DeserializeObject<OrderModel>(message);
                    if (order != null)
                    {
                        if (order.Status == OrderStatus.DELIVERY_SUCCESS)
                        {
                            _logger.LogInformation($"Order Service - Received Order: {order?.OrderId} - {order.Status.ToString()}");
                            // Update final order status here.
                            order.Status = OrderStatus.ORDER_SUCCESS;
                            _logger.LogInformation($"Order Service - Order Fullfilled: {order?.OrderId} - {order.Status.ToString()}");
                            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        }
                        else if (order.Status == OrderStatus.PAYMENT_FAILURE || order.Status == OrderStatus.DELIVERY_FAILURE)
                        {
                            _logger.LogInformation($"Order Service - Received Order: {order?.OrderId} - {order.Status.ToString()}");
                            // Update final order status here.
                            order.Status = OrderStatus.ORDER_FAILURE;
                            _logger.LogInformation($"Order Service - Order Failed: {order?.OrderId}");
                            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        }
                        else
                        {
                            // _logger.LogInformation($"Message received is empty in order queue");
                        }
                    }
                    
                };
                channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
            }
           
            await Task.CompletedTask;
        }

        //public void PublishMessage(IConnection connection, OrderModel order)
        //{
        //    using (var channel = connection.CreateModel())
        //    {
        //        var queueName = "payment_queue";
        //        channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
        //        var message = JsonConvert.SerializeObject(order);
        //        var body = Encoding.UTF8.GetBytes(message);
        //        channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: null, body: body);
        //    }
        //}

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Stopping OrderQueueHostedService...");
            await base.StopAsync(stoppingToken);
        }
    }

    public class PaymentQueueHostedService : BackgroundService
    {
        private readonly RabbitMQConfig _rabbitMQConfig;
        private readonly ILogger<PaymentQueueHostedService> _logger;

        public PaymentQueueHostedService(RabbitMQConfig rabbitMQConfig, ILogger<PaymentQueueHostedService> logger)
        {
            _rabbitMQConfig = rabbitMQConfig;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var connection = _rabbitMQConfig.GetConnectionFactory().CreateConnection();
            var channel = connection.CreateModel();
            string[] queues = new string[] { "order_queue" };
            foreach (var queueName in queues)
            {
                channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    OrderModel? order = JsonConvert.DeserializeObject<OrderModel>(message);
                    if (order != null)
                    {
                        if (order != null && order.Status == OrderStatus.ORDER_CREATED)
                        {
                            _logger.LogInformation($"Payment Service - Received Order: {order?.OrderId} - {order.Status.ToString()}");
                            // Process payment here
                            order.Status = OrderStatus.PAYMENT_SUCCESS;
                            PublishMessage(connection, order);
                            _logger.LogInformation($"Payment Service - Payment Completed: {order?.OrderId} - {order.Status.ToString()}");
                            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        }
                        else if (order.Status == OrderStatus.DELIVERY_FAILURE)
                        {
                            _logger.LogInformation($"Payment Service - Received Order: {order?.OrderId} - {order.Status.ToString()}");
                            // Process payment here
                            //order.Status = OrderStatus.PAYMENT_SUCCESS;
                            //PublishMessage(connection, order);
                            _logger.LogInformation($"Payment Service - Payment Reverted: {order?.OrderId} - {order.Status.ToString()}");
                            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        }
                        else
                        {
                            //_logger.LogInformation($"Message received is empty in order queue");
                        }
                    }
                       

                };
                channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
            }
           
            await Task.CompletedTask;
        }

        public void PublishMessage(IConnection connection, OrderModel order)
        {
            using (var channel = connection.CreateModel())
            {
                var queueName = "payment_queue";
                channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
                var message = JsonConvert.SerializeObject(order);
                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(exchange: "food.fanout", routingKey: "", basicProperties: null, body: body);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Stopping PaymentQueueHostedService...");
            await base.StopAsync(stoppingToken);
        }
    }

    public class DeliveryQueueHostedService : BackgroundService
    {
        private readonly RabbitMQConfig _rabbitMQConfig;
        private readonly ILogger<DeliveryQueueHostedService> _logger;

        public DeliveryQueueHostedService(RabbitMQConfig rabbitMQConfig, ILogger<DeliveryQueueHostedService> logger)
        {
            _rabbitMQConfig = rabbitMQConfig;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var connection = _rabbitMQConfig.GetConnectionFactory().CreateConnection();
            var channel = connection.CreateModel();
            var queueName = "payment_queue";
            channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                OrderModel? order = JsonConvert.DeserializeObject<OrderModel>(message);
                if (order != null )
                {
                    if (order.Status == OrderStatus.PAYMENT_SUCCESS)
                    {
                        _logger.LogInformation($"Delivery Service - Received Order: {order?.OrderId} - {order.Status.ToString()}");
                        // Process delivery here
                        order.Status = OrderStatus.DELIVERY_FAILURE;
                        PublishMessage(connection, order);
                        _logger.LogInformation($"Delivery Service - delivery Completed: {order?.OrderId} - {order.Status.ToString()}");
                        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    //else
                    //{
                    //    _logger.LogInformation($"Message received as PAYMENT FAILED in payment queue");
                    //}
                    
                }
                else
                {
                    //_logger.LogInformation($"Message received is empty in delivery queue");
                }

            };
            channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
            await Task.CompletedTask;
        }

        public void PublishMessage(IConnection connection, OrderModel order)
        {
            using (var channel = connection.CreateModel())
            {
                var queueName = "delivery_queue";
                channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
                var message = JsonConvert.SerializeObject(order);
                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(exchange: "food.fanout", routingKey: "", basicProperties: null, body: body);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Stopping DeliveryQueueHostedService...");
            await base.StopAsync(stoppingToken);
        }
    }

}
