using A2A.AspNetCore;
using DEMOMAF;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using WebUI.AgentHost;
using WebUI.AgentHost.Utilities;
using WebUI.Custom;
using WebUI.StoryTellersAgents;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddOpenApi();

// Add services to the container.
builder.Services.AddProblemDetails();

// Configure the chat model and our agent.
await builder.AddKeyedChatClientAsync("chat-model");

// Add DevUI services
builder.AddDevUI();

// Add OpenAI services
builder.AddOpenAIChatCompletions();
builder.AddOpenAIResponses();


builder.Services.AddAGUI();


//groupchat does not work yet..
//builder.AddDnDGroup("chat-model");
builder.AddStoryWriters("chat-model");
// Workflow consisting of multiple specialized agents
var chemistryAgent = builder.AddAIAgent("chemist",
    instructions: "You are a chemistry expert. Answer thinking from the chemistry perspective",
    description: "An agent that helps with chemistry.",
    chatClientServiceKey: "chat-model");

var mathsAgent = builder.AddAIAgent("mathematician",
    instructions: "You are a mathematics expert. Answer thinking from the maths perspective",
    description: "An agent that helps with mathematics.",
    chatClientServiceKey: "chat-model");

var literatureAgent = builder.AddAIAgent("literator",
    instructions: "You are a literature expert. Answer thinking from the literature perspective",
    description: "An agent that helps with literature.",
    chatClientServiceKey: "chat-model");

var scienceSequentialWorkflow = builder.AddWorkflow("science-sequential-workflow", (sp, key) =>
{
    List<IHostedAgentBuilder> usedAgents = [chemistryAgent, mathsAgent, literatureAgent];
    var agents = usedAgents.Select(ab => sp.GetRequiredKeyedService<AIAgent>(ab.Name));
    return AgentWorkflowBuilder.BuildSequential(workflowName: key, agents: agents);
}).AddAsAIAgent();


builder.AddWorkflow("nonAgentWorkflow", (sp, key) =>
{
    List<IHostedAgentBuilder> usedAgents = [chemistryAgent];
    var agents = usedAgents.Select(ab => sp.GetRequiredKeyedService<AIAgent>(ab.Name));
    return AgentWorkflowBuilder.BuildSequential(workflowName: key, agents: agents);
});

builder.Services.AddKeyedSingleton("NonAgentAndNonmatchingDINameWorkflow", (sp, key) =>
{
    List<IHostedAgentBuilder> usedAgents = [chemistryAgent];
    var agents = usedAgents.Select(ab => sp.GetRequiredKeyedService<AIAgent>(ab.Name));
    return AgentWorkflowBuilder.BuildSequential(workflowName: "random-name", agents: agents);
});





var app = builder.Build();
app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Agents API"));

await FoundrySetup.StartChatFoundryService("phi-4-mini");
var chatClient = FoundrySetup.StartChatClient();

AIAgent agent = chatClient.CreateAIAgent(
    name: "AGUIAssistant",
    instructions: "You are a helpful assistant.",
    description: "An agent that speaks like a pirate."
   );

// Map the AG-UI agent endpoint
app.MapAGUI("/ag-ui", agent);
// Configure the HTTP request pipeline.
app.UseExceptionHandler();


app.MapDevUI();

app.MapOpenAIResponses();
app.MapOpenAIConversations();

app.MapOpenAIChatCompletions(agent);

// Map the agents HTTP endpoints
app.MapAgentDiscovery("/agents");

app.MapDefaultEndpoints();
app.Run();