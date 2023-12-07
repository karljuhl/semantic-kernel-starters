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

Response response = await client.AnalyzeConversationAsync(RequestContent.Create(data, JsonPropertyNames.CamelCase));
