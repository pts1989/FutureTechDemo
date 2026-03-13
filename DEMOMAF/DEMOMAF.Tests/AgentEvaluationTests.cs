using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.DependencyInjection;


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
        var chatClient = agent as ChatClientAgent;
        Assert.NotNull(chatClient);

        var conversation = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "Hello, Do you have sailing advice?")
        };

        // Act
        var response = await chatClient.RunAsync(conversation);
        var chatreponse =new ChatResponse(response.Messages); // Convert to ChatResponse for evaluation
        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Text);
        var validator = new RelevanceEvaluator();
        var validationResults = await validator.EvaluateAsync(conversation, chatreponse, new ChatConfiguration(chatClient.ChatClient));
        var relevanceResult = validationResults.Metrics.First().Value;
        Assert.False(relevanceResult!.Interpretation!.Failed);
    }
}
