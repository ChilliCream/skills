using Microsoft.Extensions.DependencyInjection;
using Skills.Extensions;
using Skills.Net;
using Skills.Sources.Providers;
using Xunit;

namespace Skills.Tests.Extensions;

/// <summary>
/// Proves the buffer limit and redirect policy <see cref="ServiceCollectionExtensions.AddSkillsServices"/>
/// applies to <see cref="BlobClient"/> and <see cref="WellKnownProvider"/> stay scoped to those two
/// named clients and do not leak onto a client an embedding host registers for its own use.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    private const string HostClientName = "Host.Api";

    [Theory]
    [InlineData(BlobClient.HttpClientName)]
    [InlineData(WellKnownProvider.HttpClientName)]
    public void Skills_Named_Clients_Get_The_Buffer_Limit_And_Redirect_Policy(string clientName)
    {
        var services = new ServiceCollection();
        services.AddSkillsServices("skills");
        services.AddHttpClient(HostClientName);
        var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient(clientName);
        var primaryHandler = Assert.IsType<HttpClientHandler>(ResolvePrimaryHandler(provider, clientName));

        Assert.Equal(BlobClient.MaxResponseBytes, client.MaxResponseContentBufferSize);
        Assert.False(primaryHandler.AllowAutoRedirect);
    }

    [Fact]
    public void Host_Registered_Client_Keeps_Default_Buffer_And_Redirect_Behavior()
    {
        var services = new ServiceCollection();
        services.AddSkillsServices("skills");
        services.AddHttpClient(HostClientName);
        var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var hostClient = factory.CreateClient(HostClientName);
        var hostHandler = Assert.IsType<SocketsHttpHandler>(ResolvePrimaryHandler(provider, HostClientName));

        Assert.NotEqual(BlobClient.MaxResponseBytes, hostClient.MaxResponseContentBufferSize);
        Assert.Equal(int.MaxValue, hostClient.MaxResponseContentBufferSize);
        Assert.True(hostHandler.AllowAutoRedirect);
    }

    private static HttpMessageHandler ResolvePrimaryHandler(IServiceProvider provider, string clientName)
    {
        var handlerFactory = provider.GetRequiredService<IHttpMessageHandlerFactory>();
        var handler = handlerFactory.CreateHandler(clientName);
        while (handler is DelegatingHandler delegating)
        {
            handler = delegating.InnerHandler!;
        }

        return handler;
    }
}
