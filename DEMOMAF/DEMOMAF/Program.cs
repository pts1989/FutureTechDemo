using Azure.AI.OpenAI;
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using DEMOMAF;
using Microsoft.Agents.AI;
using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI;
using System;
using System.ClientModel;



AIAgent agent = chatClient.CreateAIAgent(
    instructions: "You are good at telling jokes.",
    name: "Joker");
// Invoke the agent and output the text result.
Console.WriteLine(await agent.RunAsync("Tell me a joke about a pirate."));

await FoundrySetup.StopFoundryAsync();
