using System.Threading;

namespace GrpcServer.Services;

public class StreamMonitorService
{
    private int _activeStreams;
    private int _totalStreams;
    
    public int ActiveStreams => _activeStreams;
    public int TotalStreams => _totalStreams;
    
    public void StreamStarted()
    {
        Interlocked.Increment(ref _activeStreams);
        Interlocked.Increment(ref _totalStreams);
    }
    
    public void StreamEnded()
    {
        Interlocked.Decrement(ref _activeStreams);
    }
}