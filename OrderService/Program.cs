using OrderService;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Add RabbitMQ configuration
builder.Services.AddSingleton<RabbitMQConfig>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<OrderQueueHostedService>();
builder.Services.AddHostedService<PaymentQueueHostedService>();
builder.Services.AddHostedService<DeliveryQueueHostedService>();


var app = builder.Build();

//app.Services.GetService<IHostedService>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
