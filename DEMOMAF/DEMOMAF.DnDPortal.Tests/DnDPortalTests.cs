using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using DEMOMAF.DnDPortal.Components;

namespace DEMOMAF.DnDPortal.Tests;

/// <summary>
/// Smoke and DI tests for the D&amp;D Portal website.
/// Fast tests verify startup and structure; Nightly tests perform real AI calls
/// (requires a valid chat-model connection string in appsettings.json or env vars).
/// </summary>
public class DnDPortalTests : IClassFixture<WebApplicationFactory<App>>
{
    private readonly WebApplicationFactory<App> _factory;

    public DnDPortalTests(WebApplicationFactory<App> factory) => _factory = factory;

    // ── Pages load ───────────────────────────────────────────────────────────

    [Theory]
    [Trait("Category", "Fast")]
    [InlineData("/")]
    [InlineData("/world-builder")]
    [InlineData("/party-chat")]
    [InlineData("/story-writers")]
    public async Task Pages_ShouldReturn_200(string url)
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(url);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // ── DI resolution ────────────────────────────────────────────────────────

    [Theory]
    [Trait("Category", "Fast")]
    [InlineData("World_Builder")]
    [InlineData("Character_Creator")]
    [InlineData("Plot_Generator")]
    [InlineData("Aragorn_The_Hero")]
    [InlineData("Lila_The_Thief")]
    [InlineData("Zoltan_The_Mage")]
    [InlineData("Fantasy_Expert")]
    [InlineData("Horror_Expert")]
    [InlineData("SciFi_Expert")]
    public void Agent_ShouldBeResolvable_FromDI(string agentKey)
    {
        var agent = _factory.Services.GetRequiredKeyedService<AIAgent>(agentKey);
        Assert.NotNull(agent);
    }

    [Theory]
    [Trait("Category", "Fast")]
    [InlineData("dnd-story-workflow")]
    [InlineData("dnd-party-workflow")]
    [InlineData("dnd-worldbuilder-workflow")]
    public void Workflow_ShouldBeResolvable_AsAIAgent(string workflowKey)
    {
        var agent = _factory.Services.GetRequiredKeyedService<AIAgent>(workflowKey);
        Assert.NotNull(agent);
    }

    // ── Live agent smoke tests (Nightly) ─────────────────────────────────────

    [Theory]
    [Trait("Category", "Nightly")]
    [InlineData("World_Builder",     "Describe a misty mountain pass at dawn.")]
    [InlineData("Character_Creator", "A young thief stands in a crowded bazaar.")]
    [InlineData("Plot_Generator",    "A merchant's cart loses a wheel near the city gates.")]
    public async Task WorldBuilderAgents_ShouldRespond(string agentKey, string prompt)
    {
        var agent = _factory.Services.GetRequiredKeyedService<AIAgent>(agentKey);
        var messages = new List<ChatMessage> { new(ChatRole.User, prompt) };

        var response = await agent.RunAsync(messages);

        Assert.NotNull(response);
        Assert.NotEmpty(response.Text);
    }

    [Theory]
    [Trait("Category", "Nightly")]
    [InlineData("Aragorn_The_Hero", "The bridge ahead is guarded by a troll. What is your first move?")]
    [InlineData("Lila_The_Thief",  "The chest is locked. What do you do?")]
    [InlineData("Zoltan_The_Mage", "A strange symbol glows on the door. What do you sense?")]
    public async Task PartyAgents_ShouldRespond(string agentKey, string prompt)
    {
        var agent = _factory.Services.GetRequiredKeyedService<AIAgent>(agentKey);
        var messages = new List<ChatMessage> { new(ChatRole.User, prompt) };

        var response = await agent.RunAsync(messages);

        Assert.NotNull(response);
        Assert.NotEmpty(response.Text);
    }

    [Theory]
    [Trait("Category", "Nightly")]
    [InlineData("Fantasy_Expert", "Write two sentences opening a scene in an enchanted forest.")]
    [InlineData("Horror_Expert",  "Write two sentences building dread in an empty lighthouse.")]
    [InlineData("SciFi_Expert",   "Write two sentences describing a derelict space station.")]
    public async Task StoryAgents_ShouldRespond(string agentKey, string prompt)
    {
        var agent = _factory.Services.GetRequiredKeyedService<AIAgent>(agentKey);
        var messages = new List<ChatMessage> { new(ChatRole.User, prompt) };

        var response = await agent.RunAsync(messages);

        Assert.NotNull(response);
        Assert.NotEmpty(response.Text);
    }

    // ── Sequential workflow integration (Nightly) ────────────────────────────

    [Fact]
    [Trait("Category", "Nightly")]
    public async Task WorldBuilderWorkflow_ShouldProduceChainedResponse()
    {
        const string premise = "A volcanic island where fire sprites tend ancient forges.";
        var history = new List<ChatMessage> { new(ChatRole.User, premise) };

        // Step 1 — World Builder
        var worldAgent = _factory.Services.GetRequiredKeyedService<AIAgent>("World_Builder");
        var worldResponse = await worldAgent.RunAsync(history);
        Assert.NotEmpty(worldResponse.Text);
        history.Add(new(ChatRole.Assistant, worldResponse.Text));

        // Step 2 — Character Creator (has world context)
        var charAgent = _factory.Services.GetRequiredKeyedService<AIAgent>("Character_Creator");
        var charResponse = await charAgent.RunAsync(history);
        Assert.NotEmpty(charResponse.Text);
        history.Add(new(ChatRole.Assistant, charResponse.Text));

        // Step 3 — Plot Generator (has both world and character context)
        var plotAgent = _factory.Services.GetRequiredKeyedService<AIAgent>("Plot_Generator");
        var plotResponse = await plotAgent.RunAsync(history);
        Assert.NotEmpty(plotResponse.Text);
    }

    // ── Concurrent workflow integration (Nightly) ────────────────────────────

    [Fact]
    [Trait("Category", "Nightly")]
    public async Task StoryConclave_AllThreeAgents_ShouldRespondConcurrently()
    {
        const string premise = "A ship crewed by ghosts sails toward the edge of the world.";
        var messages = new List<ChatMessage> { new(ChatRole.User, premise) };

        var fantasy = _factory.Services.GetRequiredKeyedService<AIAgent>("Fantasy_Expert");
        var horror  = _factory.Services.GetRequiredKeyedService<AIAgent>("Horror_Expert");
        var scifi   = _factory.Services.GetRequiredKeyedService<AIAgent>("SciFi_Expert");

        var results = await Task.WhenAll(
            fantasy.RunAsync(messages),
            horror.RunAsync(messages),
            scifi.RunAsync(messages)
        );

        foreach (var r in results)
        {
            Assert.NotNull(r);
            Assert.NotEmpty(r.Text);
        }
    }
}
