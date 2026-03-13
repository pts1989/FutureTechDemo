using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using WebUI.AgentHost.Utilities;

namespace WebUI.StoryTellersAgents
{
    public static class DnDHandoffChat
    {
        public static IHostedAgentBuilder AddDnDHandoffChat(this IHostApplicationBuilder builder, string connectionName)
        {
            var storyteller = builder.AddAIAgent("Storyteller",
                 instructions :"""
                    You are a router. Your sole responsibility is to analyze the user's request and determine the appropriate specialist agent.
                    - For requests about story, background, or lore, output ONLY the name: Lore_Master
                    - For requests about the quest's objective or goal, output ONLY the name: Goal_Setter
                    - For requests about creating a creature or monster, output ONLY the name: Monster_Creator
                    - For requests about a puzzle or riddle, output ONLY the name: Riddle_Maker
                    Your output must be a single word, the name of the agent. Do not add any other text, explanation, or punctuation.
                    """,
                description :"Acts as a triage agent, determining which specialist is needed for the next step.",
                chatClientServiceKey: connectionName);

            var loreMaster = builder.AddAIAgent("Lore_Master",
                description : "World-builder who crafts the background story and name for a quest.",
                instructions : "You are a master world-builder and historian. Based on the user's request and the conversation history, craft a rich, compelling background story for a fantasy quest. Conclude with a fitting and evocative name for the quest. The tone should be mysterious and epic.",
                chatClientServiceKey: connectionName);

            // Specialist Agent: Defines the quest's main goal.
            var goalSetter = builder.AddAIAgent("Goal_Setter",
                description : "Quest designer who defines the final objective of a quest.",
                instructions : "You are a master quest designer. Using the established lore from the conversation history, define a clear, challenging, and rewarding final objective for the quest. The goal should be specific and actionable.",
                chatClientServiceKey: connectionName);

            // Specialist Agent: Designs a monster for the quest.
            var monsterCreator = builder.AddAIAgent("Monster_Creator",
                description : "Creature designer who creates a unique monster fitting for the quest.",
                instructions : "You are a legendary creature designer. Based on the quest's theme and lore from the conversation history, design a unique and fearsome monster. Describe its appearance, abilities, and why it guards a crucial path or location within the quest.",
                chatClientServiceKey: connectionName);

            // Specialist Agent: Creates a riddle for the quest.
            var riddleMaker = builder.AddAIAgent("Riddle_Maker",
                description : "Puzzle master who writes a clever riddle fitting for the quest.",
                instructions : "You are a master of puzzles and enigmas. Drawing inspiration from the established quest lore in the conversation history, write a clever and thematic riddle. The riddle will be used to unlock a magical barrier or a hidden secret. Provide the riddle and the answer separately.",
                chatClientServiceKey: connectionName);

            return builder.AddWorkflow("dnd-handoff-workflow", (sp, key) =>
            {
                
                List<IHostedAgentBuilder> usedAgents = [storyteller];
                var agents = usedAgents.Select(ab => sp.GetRequiredKeyedService<AIAgent>(ab.Name));
                List<IHostedAgentBuilder> Handoffs = [riddleMaker,loreMaster,goalSetter,monsterCreator];
                var handoffsAgents = usedAgents.Select(ab => sp.GetRequiredKeyedService<AIAgent>(ab.Name));
                var workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(agents.First()).WithHandoffInstructions("""
                    Handoff to Lore_Master to develop the story, background, or world-building elements.
                    Handoff to Goal_Setter to define a clear and compelling quest objective.
                    Handoff to Monster_Creator to design a unique creature for the quest.
                    Handoff to Riddle_Maker to create a thematic puzzle or enigma.
                    """).WithHandoffs(agents.First(), handoffsAgents).Build();
                
                return workflow;

            }).AddAsAIAgent();
        }
    }
}
