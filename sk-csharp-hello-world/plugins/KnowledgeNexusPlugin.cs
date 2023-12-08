using Azure.Core;
using Azure.AI.Language.QuestionAnswering;
using Azure.AI.Language.Conversations;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using System.ComponentModel;
using System.Threading.Tasks;
using System;
using System.Text.Json;
using System.Linq;


namespace Plugins;

/// <summary>
/// A Semantic Kernel plugin that pulls CQA, CLU response context 
/// and returns them to be used by the RAG assistant prompt .yaml
/// </summary>

public class KnowledgeNexusPlugin
{
    private readonly AzureKeyCredential credential;
    private readonly Uri cqaEndpoint;
    private readonly Uri cluEndpoint;
    private readonly string cqaProjectName;
    private readonly string cqaDeploymentName;
    private readonly string cluProjectName;
    private readonly string cluDeploymentName;
    private readonly QuestionAnsweringClient cqaClient;
    private readonly ConversationAnalysisClient cluClient;

    public KnowledgeNexusPlugin()
    {
        credential = new AzureKeyCredential("{api-key}");
        cqaEndpoint = new Uri("https://hckthn.cognitiveservices.azure.com/");
        cluEndpoint = new Uri("https://hckthn.cognitiveservices.azure.com/");
        cqaProjectName = "<YOUR CQA PROJECT NAME>";
        cqaDeploymentName = "<YOUR CQA DEPLOYMENT NAME>";
        cluProjectName = "<YOUR CLU PROJECT NAME>";
        cluDeploymentName = "<YOUR CLU DEPLOYMENT NAME>";

        cqaClient = new QuestionAnsweringClient(cqaEndpoint, credential);
        cluClient = new ConversationAnalysisClient(cluEndpoint, credential);
    }

    [KernelFunction]
    // [Description("Gets contextual information to help answer the user query.")]
    [Description("Gathers responses from CQA and CLU services to build the best answer for a user's query.")]
    [SKOutputDescription("The responses from CQA and CLU services.")]
    public async Task<string> HandleRequestAsync(
        [Description("The user's query to process.")] string userQuery)
    {
        // Asynchronously call CQA and CLU services
        var cqaResponse = await GetCqaResponseAsync(userQuery);
        var cluResponse = await GetCluResponseAsync(userQuery);

        // Process responses
        return ProcessResponses(cqaResponse, cluResponse);
    }

    private async Task<string> GetCqaResponseAsync(string query)
    {
        // Call CQA service and get response
        QuestionAnsweringProject project = new QuestionAnsweringProject(cqaProjectName, cqaDeploymentName);
        Response<AnswersResult> response = await cqaClient.GetAnswersAsync(query, project);

        // Minimum confidence score threshold
        double confidenceThreshold = 0.6;
        int topAnswersCount = 5;

        // Filter answers to get top 5 answers with confidence score >= 0.6
        var topAnswers = response.Value.Answers
            .Where(answer => answer.Confidence >= confidenceThreshold) // Filter answers by confidence
            .OrderByDescending(answer => answer.Confidence) // Order by confidence, highest first
            .Take(topAnswersCount) // Take only the top 5
            .ToList();

        // Convert results to JSON
        string json = JsonSerializer.Serialize(topAnswers);

        return json;
    }



    private async Task<string> GetCluResponseAsync(string query)
    {
        string projectName = "testing_clu_qa";
        string deploymentName = "clu_basic";

        var data = new
        {
            AnalysisInput = new
            {
                ConversationItem = new
                {
                    Text = "I wanna play a racket sport man, how is tomorrow at 2pm, for like 10 hours??",
                    Id = "1",
                    ParticipantId = "1",
                }
            },
            Parameters = new
            {
                ProjectName = projectName,
                DeploymentName = deploymentName,

                // Use Utf16CodeUnit for strings in .NET.
                StringIndexType = "Utf16CodeUnit",
            },
            Kind = "Conversation",
        };

        Response response = client.AnalyzeConversation(RequestContent.Create(data, JsonPropertyNames.CamelCase));

        dynamic conversationalTaskResult = response.Content.ToDynamicFromJson(JsonPropertyNames.CamelCase);
        dynamic conversationPrediction = conversationalTaskResult.Result.Prediction;

        Console.WriteLine($"Top intent: {conversationPrediction.TopIntent}");

        Console.WriteLine("Intents:");
        foreach (dynamic intent in conversationPrediction.Intents)
        {
            Console.WriteLine($"Category: {intent.Category}");
            Console.WriteLine($"Confidence: {intent.ConfidenceScore}");
            Console.WriteLine();
        }

        Console.WriteLine("Entities:");
        foreach (dynamic entity in conversationPrediction.Entities)
        {
            Console.WriteLine($"Category: {entity.Category}");
            Console.WriteLine($"Text: {entity.Text}");
            Console.WriteLine($"Offset: {entity.Offset}");
            Console.WriteLine($"Length: {entity.Length}");
            Console.WriteLine($"Confidence: {entity.ConfidenceScore}");
            Console.WriteLine();

            if (entity.Resolutions is not null)
            {
                foreach (dynamic resolution in entity.Resolutions)
                {
                    if (resolution.ResolutionKind == "DateTimeResolution")
                    {
                        Console.WriteLine($"Datetime Sub Kind: {resolution.DateTimeSubKind}");
                        Console.WriteLine($"Timex: {resolution.Timex}");
                        Console.WriteLine($"Value: {resolution.Value}");
                        Console.WriteLine();
                    }
                }
            }
}
        // Call CLU service and process response

        // Implement the logic to call CLU service with the query and process the response
    }

    private string ProcessResponses(string cqaResponse, string cluResponse)
    {
        // Combine insights from CQA and CLU
        // Implement the logic to process and combine the responses from CQA and CLU services

        // Output a json object - CLU response, and CQA response
    }
}
