using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using WebUI.AgentHost.Utilities;

namespace WebUI.StoryTellersAgents
{
    public static class WorldBuilderSequential
    {
        public static IHostedAgentBuilder AddWorldBuilder(this IHostApplicationBuilder builder, string connectionName)
        {
            var worldBuilder =  builder.AddAIAgent("World_Builder",
                description : "Builds a rich, atmospheric scene based on the initial prompt.",
                instructions :
                     """
                    You are a master world-builder. Based on the user's input, describe a rich, atmospheric environment using vivid sensory details (sights, sounds, smells).
                    Do NOT include any characters or plot events. Your focus is solely on setting the scene.
                    """,
            chatClientServiceKey: connectionName);

            var characterCreator = builder.AddAIAgent("Character_Creator",
                description : "Creates a memorable character that fits within the previously established environment.",
                instructions :
                    """
                    You are a master character designer. Based on the environment described in the preceding text, introduce a single, memorable character.
                    Describe their appearance, their immediate goal or motivation, and a simple action they are performing within that scene. Ensure the character feels like a natural part of the world.
                    """,
                chatClientServiceKey: connectionName);

            var plotGenerator = builder.AddAIAgent("Plot_Generator",
                description : "Introduces a conflict or inciting incident into the scene.",
                instructions :
                    """
                    You are a master of suspense. The scene and character are set. Now, introduce a single, unexpected event or an immediate conflict that complicates the character's situation.
                    Your goal is to create tension and leave a question in the reader's mind about what will happen next. Build directly upon the established setting and character actions.
                    """,
                chatClientServiceKey: connectionName);

            return builder.AddWorkflow("dnd-worldbuilder-workflow", (sp, key) =>
            {
                
                List<IHostedAgentBuilder> usedAgents = [worldBuilder, characterCreator, plotGenerator];
                var agents = usedAgents.Select(ab => sp.GetRequiredKeyedService<AIAgent>(ab.Name));
                return AgentWorkflowBuilder.BuildSequential("dnd-worldbuilder-workflow", agents);
            }).AddAsAIAgent();
        }
    }
}
