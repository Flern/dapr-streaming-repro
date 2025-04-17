using Dapr.Client;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using GrpcClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprClient();

var useDapr = Environment.GetEnvironmentVariable("USE_DAPR") == "1" ? true : false;
var serverPort = Environment.GetEnvironmentVariable("DIRECT_PORT") ?? "50220";
var grpcPort = useDapr 
    ? Environment.GetEnvironmentVariable("DAPR_GRPC_PORT") ?? "32021"
    : serverPort;
var useDockerAddress = Environment.GetEnvironmentVariable("USE_DOCKER_ADDRESS") == "1" ? true : false;

var urlString = useDapr == false && useDockerAddress ? $"http://kubernetes.docker.internal:{grpcPort}" : $"http://localhost:{grpcPort}";

Console.WriteLine($"Communicating using {urlString}");

var channelDictionary = new Dictionary<string, CallInvoker>();

builder.Services.AddGrpcClient<Greeter.GreeterClient>(options =>
{
    options.Address = new Uri(urlString);
    options.Creator = _ => new Greeter.GreeterClient(CreateGrpcClientCallInvoker("grpc-server", urlString));
});

builder.Services.AddHostedService<StreamingClientFactory>();

var app = builder.Build();
app.Run();

return;

CallInvoker CreateGrpcClientCallInvoker(string appId, string urlString)
{
    return GrpcChannel.ForAddress(urlString, new GrpcChannelOptions { Credentials = ChannelCredentials.Insecure  })
                      .Intercept(
                        m =>
                        {
                            m.Add("dapr-app-id", appId);
                            m.Add("dapr-stream", "true");
                            return m;
                        });
}
