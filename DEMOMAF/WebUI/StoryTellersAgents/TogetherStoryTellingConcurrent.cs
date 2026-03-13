using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;

namespace WebUI.StoryTellersAgents
{

    public static class TogetherStoryTellingConcurrent
    {
        public static IHostedAgentBuilder AddStoryWriters(this IHostApplicationBuilder builder, string connectionName)
        {
            var horrorExpert = builder.AddAIAgent("Horror_Expert",
                instructions: """
                    You are a master of psychological horror. Your task is to write a deeply unsettling opening scene.
                    Build suspense not with jump scares, but with a palpable sense of dread.
                    Focus on unsettling details: a silence that is too deep, a shadow that moves wrong, a sound that doesn't belong.
                    """,
                description: "A purveyor of nightmares who crafts tales of suspense and dread.",
                chatClientServiceKey: connectionName);

            var scifiExpert = builder.AddAIAgent("SciFi_Expert",
                instructions: """
                    You are an expert in hard science fiction. Your task is to write a compelling opening scene.
                    Describe a futuristic, high-tech environment. Focus on specific details like shimmering data streams, the hum of anti-gravity engines, or cybernetic enhancements.
                    Create a sense of technological awe or alienation.
                    """,
                description: "A futurist who envisions worlds of advanced technology and cybernetic wonders.",
                chatClientServiceKey: connectionName);

            var fantasyExpert = builder.AddAIAgent("Fantasy_Expert",
                instructions: """
                    You are a master of epic fantasy. Your task is to write a captivating opening scene.
                    Focus on a sense of ancient wonder, mythical landscapes, and the subtle hum of forgotten magic.
                    Hint at a larger destiny or a looming prophecy.
                    """,
                description: "A master storyteller who weaves tales of ancient magic and epic destinies.",
                chatClientServiceKey: connectionName);
            return builder.AddWorkflow("dnd-story-workflow", (sp, key) =>
            {
                List<IHostedAgentBuilder> usedAgents = [fantasyExpert, horrorExpert, scifiExpert];
                var agents = usedAgents.Select(ab => sp.GetRequiredKeyedService<AIAgent>(ab.Name));
                return AgentWorkflowBuilder.BuildConcurrent(workflowName: key, agents: agents);
            }).AddAsAIAgent();


           
        }
    }
}
