using TcpMessageProcessor.Core.Interfaces;
using TcpMessageProcessor.Infrastructure.Parsing;
using TcpMessageProcessor.Infrastructure.Queues;
using TcpMessageProcessor.Infrastructure.Services;
using TcpMessageProcessor.Service;
using TcpMessageProcessor.Service.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure TCP Server options
builder.Services.Configure<TcpServerOptions>(
    builder.Configuration.GetSection(TcpServerOptions.SectionName));

// Add Redis for distributed deduplication
var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? builder.Configuration["Redis:ConnectionString"]
    ?? "localhost:6380";

Console.WriteLine($"🔴 Configuring Redis: {redisConnectionString}");

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "TcpMessageProcessor";
});

// Get RabbitMQ connection string
var rabbitMQConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")
    ?? builder.Configuration["RabbitMQ:ConnectionString"]
    ?? "amqp://admin:password123@localhost:5672/";

Console.WriteLine($"🐰 Configuring RabbitMQ: {rabbitMQConnectionString}");

// Register Core Services
builder.Services.AddSingleton<IMessageParser, BinaryMessageParser>();

// Redis-based distributed deduplication
builder.Services.AddSingleton<IDeduplicationService, RedisDeduplicationService>();

// *** THIS IS THE KEY CHANGE: RabbitMQ queues instead of InMemory ***
builder.Services.AddSingleton<IDeviceMessageQueue>(provider =>
    new RabbitMQDeviceMessageQueue(rabbitMQConnectionString));
builder.Services.AddSingleton<IDeviceEventQueue>(provider =>
    new RabbitMQDeviceEventQueue(rabbitMQConnectionString));

builder.Services.AddSingleton<IMessageRouter, MessageRouter>();
builder.Services.AddSingleton<IMessageProcessor, MessageProcessor>();

// Register TCP Background Service
builder.Services.AddHostedService<TcpMessageProcessorService>();

var app = builder.Build();

Console.WriteLine("🚀 Starting TCP Message Processor Microservice...");
Console.WriteLine($"🔴 Redis: {redisConnectionString} (Distributed Deduplication)");
Console.WriteLine($"🐰 RabbitMQ: {rabbitMQConnectionString} (Message Queues)");
Console.WriteLine("🎯 Production-ready scaling with Redis + RabbitMQ!");
Console.WriteLine("📊 RabbitMQ Management UI: http://localhost:15672 (admin/password123)");

app.Run();