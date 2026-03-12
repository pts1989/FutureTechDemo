# Foundry Local Workshop

This workshop walks through the steps needed to build the `DEMOMAF` demo application and to teach participants how to:

1. Install Foundry Local
2. Create an Aspire project (multi‑project .NET solution)
3. Add helper code for Foundry Local clients
4. Configure multiple connection options (Foundry, OpenAI, Ollama, etc.)
5. Add developer UI (DevUI) and agent UI (AGUI)
6. Build agent orchestrations (workflows and group chats)

The finished code for this workshop is available in this repository under `DEMOMAF`.

---

## 1. Install Foundry Local

Participants will need a local Foundry runtime to run models on their machine.  The official installer can be downloaded from the Microsoft Foundry site.  Follow these precise steps:

1. **Download the binary**
   * Visit the Foundry Local download page ([https://aka.ms/foundry-local](https://learn.microsoft.com/en-us/azure/foundry-local/get-started?view=foundry-classic)) and choose the package for your OS.
   * Unpack the ZIP or run the MSI.
2. **Start the service**
   ```powershell
   # verify installation
   foundry-local --version

   # start the daemon (Windows example)
   foundry-local start
   ```
   * On Linux/macOS use `sudo foundry-local start` if necessary.
   * Confirm the process is listening: `curl http://127.0.0.1:8576/v1/models` should return a 200.
3. **Adjust configuration (optional)**
   * The workshop code expects `http://127.0.0.1:8576`.
   * To change it, edit `WebUI/FoundrySetup.cs` and modify `config.Web.Urls = "http://127.0.0.1:8576"`.

> **Tip:** You can run `foundry-local stop` to shut the service down and `foundry-local status` to check health.

---

## 2. Set up an Aspire project

The workshop uses an [Aspire multiplatform solution](https://aka.ms/aspire) with several projects.  You will build something similar step by step:

1. **Create the solution container**
   ```powershell
   dotnet new sln -n WorkshopDemo
   mkdir WorkshopDemo
   cd WorkshopDemo
   ```
2. **Add the WebUI project**
   ```powershell
   dotnet new webapi -n WebUI
   dotnet sln add WorkshopDemo\WebUI\WebUI.csproj 
   ```
3. **Add support libraries**
   * Create a class library for shared settings or host defaults:
     ```powershell
     dotnet new classlib -n Workshop.ServiceDefaults
     dotnet sln add WorkshopDemo\Workshop.ServiceDefaults\Workshop.ServiceDefaults.csproj
     ```
   * Add a second library for the ChatFrontend SPA if desired.
4. **Reference projects**
   * From `WebUI` to `Workshop.ServiceDefaults`:
     ```powershell
     dotnet add WebUI\WebUI.csproj reference Workshop.ServiceDefaults\Workshop.ServiceDefaults.csproj
     ```
5. **Configure Aspire**
   * In each host project (WebUI, ChatFrontend) add the `Aspire.Sdk` package and call `builder.AddServiceDefaults();` early in `Program.cs`.
   * Look at `DEMOMAF.AppHost` and `DEMOMAF.ServiceDefaults/Extensions.cs` for examples.
6. **Open the solution**
   * Launch in Visual Studio: `start WorkshopDemo.slnx` or open the folder in VS Code.

The existing `DEMOMAF.slnx` in the repo is a reference showing how the pieces are arranged; you can fork its contents during the workshop.

---

## 3. Add helpers for Foundry Local

Create a helper class to manage the local Foundry runtime and make it usable from other projects.

1. **Add NuGet packages**
   ```powershell
   cd WebUI
   dotnet add package Microsoft.AI.Foundry.Local
   dotnet add package Microsoft.Extensions.AI
   ```
2. **Copy `FoundrySetup.cs`**
   * Paste the class from the demo into `WebUI/FoundrySetup.cs`.
   * It encapsulates:
     * `FoundryLocalManager` configuration
     * model catalog lookup, download, load
     * `StartChatFoundryService(modelName)` and `StopFoundryAsync()` methods
     * a simple `StartChatClient` factory pointing at the configured URI
3. **Walk through the code**
   * Explain `Configuration` object and how you can tweak `LogLevel` or `Web.Urls`.
   * Show that the model is cached locally and only downloaded once.
4. **Invoke from app startup**
   ```csharp
   await FoundrySetup.StartChatFoundryService("phi-4-mini"); // ensure model availability
   var chatClient = FoundrySetup.StartChatClient();           // get IChatClient
   ```
5. **Cleanup**
   ```csharp
   await FoundrySetup.StopFoundryAsync(); // when shutting down the app
   ```

This style allows participants to launch the local model on demand and use the same `IChatClient` abstraction as when working with cloud endpoints.

---

## 4. Add support for other connections

This section teaches how to make the chat layer provider‑agnostic so you can swap between Foundry, OpenAI, Ollama, etc.

1. **Define the connection-string format**
   * Create `WebUI/Utilities/ChatClientConnectionInfo.cs` (copy from demo).
   * The class parses strings like:
     ```text
     Endpoint=https://api.openai.com;Model=gpt-4;AccessKey=KEY;Provider=OpenAI
     ```
   * It returns a `ClientChatProvider` enum value (`Ollama`, `FoundryLocal`, `OpenAI`, etc.).
2. **Write registration extensions**
   * Add `ChatClientExtensions.cs` and implement `AddChatClientAsync` and `AddKeyedChatClientAsync`.
   * Each switch case uses the provider to build the proper client:
     * `builder.AddOpenAIClient(...)` for OpenAI / AzureOpenAI
     * `builder.AddOllamaClient(...)` for Ollama
     * `builder.AddOpenAIFoundryLocalClientAsync(...)` for FoundryLocal (points at the URI from `FoundrySetup`)
   * Ensure tracing/logging is configured with `UseOpenTelemetry()`.
3. **Configure connection strings**
   * Edit `appsettings.json` / `appsettings.Development.json`:
     ```json
     {
       "ConnectionStrings": {
         "chat-model": "Provider=FoundryLocal;Model=phi-4-mini;AccessKey=ignored"
       }
     }
     ```
   * To try another provider, paste a different string and restart.
4. **Wire it up in `Program.cs`**
   ```csharp
   await builder.AddKeyedChatClientAsync("chat-model");
   // or builder.AddChatClientAsync("chat-model");
   ```
5. **Test switching providers**
   * Start with Foundry, then change to OpenAI and rerun the app.
   * Observe output in `/dev-ui` or `/ag-ui` verifying the model source.

This agnostic adapter layer keeps your top‑level code the same while letting participants experiment with any supported backend.

---

## 5. Add DevUI and AGUI

Give participants live interfaces to inspect and interact with the agents.

1. **Add the packages**
   ```powershell
   cd WebUI
   dotnet add package Microsoft.Agents.AI.DevUI
   dotnet add package Microsoft.Agents.AI.Hosting.AGUI.AspNetCore
   ```
2. **Enable in startup**
   * In `WebUI/Program.cs` after adding chat clients and agents:
     ```csharp
     builder.AddDevUI();
     builder.Services.AddAGUI();
     ```
3. **Map the endpoints**
   * After `var app = builder.Build();` add:
     ```csharp
     app.MapDevUI();
     app.MapAGUI("/ag-ui", fantasyAgent);
     ```
   * `fantasyAgent` is an example agent resolved from DI; you can map any registered agent.
4. **Run and explore**
   * `dotnet run --project WebUI`.
   * Point browser at `http://localhost:5000/dev-ui` to view streaming request/response logs, trace events, and the current chat client configuration.
   * Visit `http://localhost:5000/ag-ui` to talk to an agent through a simple web UI.
5. **Changing agents**
   * Map additional agents by calling `app.MapAGUI` multiple times with different routes.
   * Show how to inspect workflows from DevUI.

These UI layers help workshop attendees see behind the curtain of what the model is doing in real time.

---

## 6. Add Orchestrations

Advanced users will build multi‑agent workflows that coordinate several personalities.

### Examples in the repo
* `WebUI/StoryTellersAgents/TogetherStoryTellingConcurrent.cs`
  * Registers three genre‑writers (horror, sci‑fi, fantasy).
  * Uses `AgentWorkflowBuilder.BuildConcurrent(...)` to run them in parallel.
  * Wraps the workflow as an agent via `.AddAsAIAgent()` so it appears in DevUI/AGUI.
* `WebUI/StoryTellersAgents/DnDGroupChat.cs`
  * Creates a round‑robin group chat using `AgentWorkflowBuilder.CreateGroupChatBuilderWith(...)`.
  * Limits the conversation to 5 iterations.

### Step‑by‑step to author your own
1. **Create the workflow class**
   ```csharp
   public static class MyOrchestration
   {
       public static IHostedAgentBuilder AddMyWorkflow(this IHostApplicationBuilder builder, string connectionName)
       {
           var alice = builder.AddAIAgent("Alice", instructions: "...", chatClientServiceKey: connectionName);
           var bob   = builder.AddAIAgent("Bob", instructions: "...", chatClientServiceKey: connectionName);

           return builder.AddWorkflow("my-workflow", (sp, key) =>
           {
               var agents = new[] { alice, bob }
                            .Select(ab => sp.GetRequiredKeyedService<AIAgent>(ab.Name));
               return AgentWorkflowBuilder.BuildSequential(workflowName: key, agents: agents);
           }).AddAsAIAgent();
       }
   }
   ```
2. **Register it in `Program.cs`**
   ```csharp
   builder.AddMyWorkflow("chat-model");
   ```
3. **Expose in UI if desired**
   ```csharp
   var wfAgent = scope.ServiceProvider.GetRequiredKeyedService<AIAgent>("my-workflow");
   app.MapAGUI("/my-wf", wfAgent);
   ```
4. **Run the workflow**
   * Use DevUI to launch the agent or navigate to its AGUI endpoint.
   * Inspect trace events to see the order of agent invocations.

Encourage participants to modify the workflow type (`BuildConcurrent`, `BuildSequential`, `CreateGroupChatBuilderWith`) and add custom logic between turns to simulate complex coordination.

---

### Running the Workshop

1. Clone the repo and open the solution.
2. Ensure Foundry Local is running and configured (see step 1).
3. Restore NuGet packages:
   ```powershell
   dotnet restore
   ```
4. Configure a connection string in `appsettings.Development.json` for `chat-model`.
   ```json
   "ConnectionStrings": {
     "chat-model": "Provider=FoundryLocal;Model=phi-4-mini;AccessKey=any"
   }
   ```
5. Start the WebUI project:
   ```powershell
   dotnet run --project DEMOMAF\WebUI
   ```
6. Browse to `/dev-ui` and `/ag-ui` to experiment.  Try changing the connection string to an OpenAI or Ollama endpoint and restart.
7. Open the ChatFrontend in a browser (it serves a simple SPA) to interact with the chat agents.

Participants can then extend the code by adding new providers, agents, or workflows.

---

This workshop structure provides a hands‑on path from a fresh Aspire project to a feature‑complete multi‑provider agent demo with developer and agent UIs, as well as orchestration examples.
