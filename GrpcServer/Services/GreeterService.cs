using Grpc.Core;

namespace GrpcServer.Services;

public class GreeterService : Greeter.GreeterBase
{
    private readonly ILogger<GreeterService> _logger;
    private readonly StreamMonitorService _monitor;

    public GreeterService(ILogger<GreeterService> logger, StreamMonitorService monitor)
    {
        _logger = logger;
        _monitor = monitor;
    }

    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HelloReply
        {
            Message = "Hello " + request.Name
        });
    }

    public override async Task StreamHello(HelloRequest request,IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
    {
        try
        {
            // Log and track stream start
            _monitor.StreamStarted();
            _logger.LogInformation("Stream started - Active streams {Count}", _monitor.ActiveStreams);
            

            // Handle stream cancellation
            context.CancellationToken.Register(() =>
            {
                _monitor.StreamEnded();
                _logger.LogInformation("Stream cancelled - Active streams {Count}", _monitor.ActiveStreams);
            });

            var count = 0;
            while (context.CancellationToken.IsCancellationRequested == false)
            {
                count++;
                await responseStream.WriteAsync(new HelloReply
                {
                    Message = $"Hello {request.Name} {count} (Active: {_monitor.ActiveStreams})"
                });

                await Task.Delay(1000, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in stream");
            _monitor.StreamEnded();
        }
        finally
        {
            // If we exit the method normally (not via cancellation)
            if (!context.CancellationToken.IsCancellationRequested)
            {
                _monitor.StreamEnded();
            }
        }
    }
}
