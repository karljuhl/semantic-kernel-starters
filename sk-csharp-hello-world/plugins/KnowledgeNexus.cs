// SK plugin which calls on Custom QnA and Conversational Language Understanding for asynchronous data gathering in order to construct a response built in a context heavy environment 

// SK Plugin

name: KnowledgeNexus
template: |
{{#message role="system"}}
  ## Instructions
  Gather the data returned to form the best answer you reasonably can 

  ## Rules
  - Do not fabricate any information
  - Other rules
  {{/message}}

{{#message role="user"}}
{{user_query}}
{{/message}}

{{ IF CLU value returned: }}
Here is some context of the user's input that can help you understand their query better: {CLU Intent, CLU Entities}
{{/if}}

{{ IF RAG context pulled: }}
Here is some useful information that could be relevant to answer the user query: {RAG Context Chunks}
{{/if}}

{{ IF CQA answer returned: }}
Here is an existing answer to the user's question, that you may use to help guide your response: {CQA Answer}
{{/if}}

template_format: handlebars
description: A function that combines data outputs from RAG, CLU, and CQA resources to create the best possible answer to a users query
input_variables:
  - name: messages
    type: ChatHistory
    description: The history of the chat.
    is_required: true
output_variable:
    type: string
    description: The response from the assistant.
execution_settings:
  - model_id_pattern: ^gpt-4
    temperature: 0.7
  - model_id_pattern: ^gpt-3\.?5-turbo
    temperature: 0.3





        
// CQA_KN.cs
// Asynchoronous fetching of related answers from a CQA resource using azure sdk for .net
        
using Azure.Core;
using Azure.AI.Language.QuestionAnswering;

Uri endpoint = new Uri("https://myaccount.cognitiveservices.azure.com/");
AzureKeyCredential credential = new AzureKeyCredential("{api-key}");

QuestionAnsweringClient client = new QuestionAnsweringClient(endpoint, credential);

string projectName = "{ProjectName}";
string deploymentName = "{DeploymentName}";
QuestionAnsweringProject project = new QuestionAnsweringProject(projectName, deploymentName);
Response<AnswersResult> response = await client.GetAnswersAsync("How long should my Surface battery last?", project);

foreach (KnowledgeBaseAnswer answer in response.Value.Answers)
{
    Console.WriteLine($"({answer.Confidence:P2}) {answer.Answer}");
    Console.WriteLine($"Source: {answer.Source}");
    Console.WriteLine();
}







// CLU_KN.cs
// Asynchoronous fetching of related CLU entities and intents from the azure sdk for .net

using Azure.Core;
using Azure.Core.Serialization;
using Azure.AI.Language.Conversations;

Uri endpoint = new Uri("https://myaccount.cognitiveservices.azure.com");
AzureKeyCredential credential = new AzureKeyCredential("{api-key}");

ConversationAnalysisClient client = new ConversationAnalysisClient(endpoint, credential);

string projectName = "Menu";
string deploymentName = "production";

var data = new
{
    AnalysisInput = new
    {
        ConversationItem = new
        {
            Text = "Send an email to Carol about tomorrow's demo",
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

Response response = await client.AnalyzeConversationAsync(RequestContent.Create(data, JsonPropertyNames.CamelCase));

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
