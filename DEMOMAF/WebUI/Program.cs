using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using WebUI.AgentHost;
using WebUI.AgentHost.Utilities;


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

builder.AddStoryWriters("chat-model");
builder.AddDnDGroup("chat-model");

var ChatBot = builder.AddAIAgent("chatbot",
    instructions: "You are an amazing helpfull assistant that allways talks like a pirate",
    description: "An helpfull agent in pirate mode.",
    chatClientServiceKey: "chat-model");

var app = builder.Build();

app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Agents API"));


// Configure the HTTP request pipeline.
app.UseExceptionHandler();

using (var scope = app.Services.CreateScope())
{
    var fantasyAgent = scope.ServiceProvider.GetRequiredKeyedService<AIAgent>("chatbot");
    app.MapAGUI("/ag-ui", fantasyAgent);
    app.MapOpenAIChatCompletions(fantasyAgent);
}

app.MapDevUI();

app.MapOpenAIResponses();
app.MapOpenAIConversations();

// Map the agents HTTP endpoints
app.MapAgentDiscovery("/agents");

app.MapDefaultEndpoints();
app.Run();
