using Shouldly;
using WolverineWebApi;

namespace Wolverine.Http.Tests;

public class posting_json : IntegrationContext
{
    public posting_json(AppFixture fixture) : base(fixture)
    {
    }
    
    [Fact]
    public async Task post_json_happy_path()
    {
        var response = await Host.Scenario(x =>
        {
            x.Post.Json(new Question { One = 3, Two = 4 }).ToUrl("/question");
            x.WithRequestHeader("accepts", "application/json");
        });

        var result = await response.ReadAsJsonAsync<Results>();
        
        result.Product.ShouldBe(12);
        result.Sum.ShouldBe(7);
    }

    [Fact]
    public async Task post_json_garbage_get_400()
    {
        var response = await Host.Scenario(x =>
        {
            x.Post.Text("garbage").ToUrl("/question");
            x.WithRequestHeader("content-type", "application/json");
            x.StatusCodeShouldBe(400);
        });
    }
    
    [Fact]
    public async Task post_text_get_415()
    {
        var response = await Host.Scenario(x =>
        {
            x.Post.Text("garbage").ToUrl("/question");
            x.WithRequestHeader("content-type", "text/plain");
            x.StatusCodeShouldBe(415);
        });
    }

    [Fact]
    public async Task post_json_but_accept_text_get_406()
    {
        var response = await Host.Scenario(x =>
        {
            x.Post.Json(new Question { One = 3, Two = 4 }).ToUrl("/question");
            x.WithRequestHeader("accept", "text/plain");
            x.StatusCodeShouldBe(406);
        });
    }
}