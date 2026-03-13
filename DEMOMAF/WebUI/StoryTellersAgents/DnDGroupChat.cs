using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using WebUI.AgentHost.Utilities;

namespace WebUI.StoryTellersAgents
{
    public static class DnDGroupChat
    {
        public static IHostedAgentBuilder AddDnDGroup(this IHostApplicationBuilder builder, string connectionName)
        {
            var hero = builder.AddAIAgent("Aragorn_The_Hero",
                instructions: """
                    You are Aragorn, a noble and courageous warrior.
                    Your personality: direct, protective, and a natural leader.
                    Your speaking style: formal and solemn.
                    Your role: Assess threats, protect your companions, and suggest bold, direct actions. Always take the lead when there is doubt.
                    """,
                description: "A brave and noble warrior who takes the lead.",
                chatClientServiceKey: connectionName);

            var thief = builder.AddAIAgent("Lila_The_Thief",
                instructions: """
                    You are Lila, a quick-witted and sarcastic thief.
                    Your personality: cynical, practical, and deeply mistrustful.
                    Your speaking style: sharp, witty, and informal.
                    Your role: Point out risks, look for traps, and always consider what there is to gain. You question everything and everyone.
                    """,
                description: "A sarcastic and practical thief who makes sharp remarks.",
                chatClientServiceKey: connectionName);

            var mage = builder.AddAIAgent("Zoltan_The_Mage",
                instructions: """
                    You are Zoltan, an ancient, wise, and cryptic mage.
                    Your personality: contemplative, detached, and mysterious.
                    Your speaking style: speaks in metaphors, riddles, and abstract observations.
                    Your role: Sense the unseen magical forces at play. Ponder the deeper meaning of events and offer cryptic but insightful advice.
                    """,
                description: "A wise but cryptic magician who speaks in riddles.",
                chatClientServiceKey: connectionName);

            return builder.AddWorkflow("dnd-party-workflow", (sp, key) =>
            {
                
                List<IHostedAgentBuilder> usedAgents = [mage, hero, thief];
                var agents = usedAgents.Select(ab => sp.GetRequiredKeyedService<AIAgent>(ab.Name));
                return AgentWorkflowBuilder.CreateGroupChatBuilderWith(agents => new RoundRobinGroupChatManager(agents)
                {
                    MaximumIterationCount = 5 // Maximum number of turns in the conversation
                }).AddParticipants(agents).WithName("dnd-party-workflow").Build();
            }).AddAsAIAgent();
        }
    }
}
