using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Agents.AI;
using WebUI;
using Xunit;

namespace DEMOMAF.Tests;

public class AgentEvaluationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AgentEvaluationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Chatbot_ShouldRespond()
    {
        // Arrange
        var services = _factory.Services;
        var agent = services.GetRequiredKeyedService<AIAgent>("chatbot");
        var chatClient = agent as IChatClient;
        Assert.NotNull(chatClient);

        var conversation = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "Hello, how are you?")
        };

        // Act
        var response = await chatClient.GetResponseAsync(conversation);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Text);
        Assert.Contains("pirate", response.Text.ToLower()); // Since it's pirate mode
    }

    // TODO: Add evaluation tests using Microsoft.Extensions.AI.Evaluation once evaluators are available
}
