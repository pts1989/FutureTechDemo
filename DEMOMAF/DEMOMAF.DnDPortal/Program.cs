using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using WebUI.AgentHost.Utilities;
using WebUI.StoryTellersAgents;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure the AI chat model (reads from ConnectionStrings["chat-model"])
await builder.AddKeyedChatClientAsync("chat-model");

// Register all D&D agent groups (reusing extension methods from WebUI)
builder.AddStoryWriters("chat-model");
builder.AddDnDGroup("chat-model");
builder.AddWorldBuilder("chat-model");

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<DEMOMAF.DnDPortal.Components.App>()
   .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();
app.Run();

// Expose Program for WebApplicationFactory in tests
public partial class Program { }
