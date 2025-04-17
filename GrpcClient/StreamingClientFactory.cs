namespace GrpcClient;

using System.Runtime.InteropServices.ComTypes;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class StreamingClientFactory : BackgroundService
{
    private readonly ILogger<StreamingClientFactory> _logger;
    private readonly string _numberOfStreams;
    private readonly IServiceProvider _services;

    public StreamingClientFactory(IServiceProvider services, ILogger<StreamingClientFactory> logger)
    {
        _logger = logger;
        _services = services;
        _numberOfStreams = Environment.GetEnvironmentVariable("STREAM_COUNT") ?? "350";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (int.TryParse(_numberOfStreams, out var numberOfStreams) == false)
        {
            throw new InvalidOperationException($"Attempted to start non-integer number of streams {_numberOfStreams}");
        }
        
        List<Task> clients = new();
        for (var clientNumber = 0; clientNumber < numberOfStreams; clientNumber++)
        {
            var scope = _services.CreateScope();
            
            clients.Add(DoWork((Greeter.GreeterClient)scope.ServiceProvider.GetService(typeof(Greeter.GreeterClient))!, clientNumber, stoppingToken));
            
            _logger.LogInformation("Starting streaming for client {ClientNumber}", clientNumber);
            
            await Task.Delay(50, CancellationToken.None);
        }

        await Task.WhenAll(clients);
    }

    private async Task DoWork(Greeter.GreeterClient grpcClient, int clientNumber, CancellationToken stoppingToken)
    {
        try
        {
            stoppingToken.Register(() => _logger.LogInformation("Stopping streaming for client {ClientNumber}", clientNumber));

            while (stoppingToken.IsCancellationRequested == false)
            {
                _logger.LogInformation("Starting streaming for client {ClientNumber}", clientNumber);
                
                var stream = grpcClient.StreamHello(new HelloRequest { Name = "GreeterClient" }, cancellationToken: stoppingToken);

                await foreach (var response in stream.ResponseStream.ReadAllAsync(stoppingToken))
                {
                    _logger.LogInformation("Greeting from client {ClientNumber}: {Message}", clientNumber, response.Message);
                    await Task.Delay(1000, CancellationToken.None);
                }
            }
        }
        catch (RpcException)
        {
            _logger.LogInformation("Rpc exception in client {ClientNumber}", clientNumber);
        }
        catch (Exception)
        {
            _logger.LogError("Error in stream for client {ClientNumber}", clientNumber);
        }
    }
}