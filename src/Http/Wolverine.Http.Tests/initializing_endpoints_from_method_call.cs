using System.Text.Json;
using FastExpressionCompiler;
using JasperFx.CodeGeneration.Frames;
using JasperFx.CodeGeneration.Model;
using Lamar;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Shouldly;
using Wolverine.Configuration;
using Wolverine.Http.Metadata;
using WolverineWebApi;

namespace Wolverine.Http.Tests;

public class initializing_endpoints_from_method_call : IDisposable
{
    private readonly Container container;
    private readonly EndpointGraph parent;

    public initializing_endpoints_from_method_call()
    {
        container = new Container(x =>
        {
            x.ForConcreteType<JsonSerializerOptions>().Configure.Singleton();
            x.For<IServiceVariableSource>().Use(c => c.CreateServiceVariableSource()).Singleton();
        });

        parent = new EndpointGraph(new WolverineOptions
        {
            ApplicationAssembly = GetType().Assembly
        }, container);
    }

    public void Dispose()
    {
        container.Dispose();
    }

    [Fact]
    public void build_pattern_using_http_pattern_with_attribute()
    {
        var endpoint = EndpointChain.ChainFor<FakeEndpoint>(x => x.SayHello());

        endpoint.RoutePattern.RawText.ShouldBe("/hello");
        endpoint.RoutePattern.Parameters.Any().ShouldBeFalse();
    }

    [Fact]
    public void capturing_the_http_method_metadata()
    {
        var chain = EndpointChain.ChainFor<FakeEndpoint>(x => x.SayHello());
        var endpoint = chain.BuildEndpoint();

        var metadata = endpoint.Metadata.OfType<HttpMethodMetadata>().Single();
        metadata.HttpMethods.Single().ShouldBe("GET");
    }

    [Fact]
    public void capturing_accepts_metadata_for_request_type()
    {
        var chain = EndpointChain.ChainFor(typeof(TestEndpoints), nameof(TestEndpoints.PostJson));
        chain.RequestType.ShouldBe(typeof(Question));
        
        var endpoint = chain.BuildEndpoint();
        var metadata = endpoint.Metadata.OfType<WolverineAcceptsMetadata>()
            .Single();
        
        metadata.RequestType.ShouldBe(chain.RequestType);
        metadata.ContentTypes.Single().ShouldBe("application/json");
    }

    [Fact]
    public void capturing_metadata_for_resource_type()
    {
        var chain = EndpointChain.ChainFor(typeof(TestEndpoints), nameof(TestEndpoints.PostJson));
        chain.ResourceType.ShouldBe(typeof(Results));
        
        var endpoint = chain.BuildEndpoint();
        var metadata = endpoint.Metadata.OfType<IProducesResponseTypeMetadata>().ToArray();
        metadata.Length.ShouldBeGreaterThanOrEqualTo(3);

        var responseBody = metadata.FirstOrDefault(x => x.StatusCode == 200);
        responseBody.Type.ShouldBe(typeof(Results));
        responseBody.ContentTypes.Single().ShouldBe("application/json");

        var badRequest = metadata.FirstOrDefault(x => x.StatusCode == 400);
        badRequest.ContentTypes.Any().ShouldBeFalse();
        badRequest.Type.ShouldBeNull();
        
        var noValue = metadata.FirstOrDefault(x => x.StatusCode == 404);
        noValue.ContentTypes.Any().ShouldBeFalse();
        noValue.Type.ShouldBeNull();
    }
    
    [Theory]
    [InlineData(nameof(FakeEndpoint.SayHello), typeof(string))]
    [InlineData(nameof(FakeEndpoint.SayHelloAsync), typeof(string))]
    [InlineData(nameof(FakeEndpoint.SayHelloAsync2), typeof(string))]
    [InlineData(nameof(FakeEndpoint.Go), null)]
    [InlineData(nameof(FakeEndpoint.GoAsync), null)]
    [InlineData(nameof(FakeEndpoint.GoAsync2), null)]
    [InlineData(nameof(FakeEndpoint.GetResponse), typeof(BigResponse))]
    [InlineData(nameof(FakeEndpoint.GetResponseAsync), typeof(BigResponse))]
    [InlineData(nameof(FakeEndpoint.GetResponseAsync2), typeof(BigResponse))]
    public void determine_resource_type(string methodName, Type? expectedType)
    {
        var method = new MethodCall(typeof(FakeEndpoint), methodName);
        var endpoint = new EndpointChain(method, parent);

        if (expectedType == null)
        {
            endpoint.ResourceType.ShouldBeNull();
        }
        else
        {
            endpoint.ResourceType.ShouldBe(expectedType);
        }
    }
}

