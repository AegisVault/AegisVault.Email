using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AegisVault.Email.Models;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AegisVault.Email.Helpers;

public static class GenerateTemplate
{
    private static readonly HttpClient httpClient = new();
    private static readonly string url = "https://api.openai.com/v1/chat/completions";
    private static readonly string apiKey = "get your own";

    [FunctionName("GenerateTemplate")]
    public static async Task<string> GenerateEmailTemplate(string requestBody)
    {
        //Just so openai doesnt try to piss about all day.
        if (httpClient.Timeout != TimeSpan.FromMinutes(3)) httpClient.Timeout = TimeSpan.FromMinutes(3);


        const string systemContext = @"
        You are a document generator that outputs email documents in MJML outputs, based on the following input data:
        {
            brand: {
                brandname: ,
                brandlogoURL: ,
                brandPrimaryColor: ,
                brandSecondaryColor:
            },
            documentType: ,
            requiredContent: ,
            fullDocumentLink: ,
            name: ,
        }

        It is important to not put these variable names in but to put the variable values given on the user message in the correct place in the MJML and attributes, etc.

        Brandname and brandlogoURL, doument type need to be in The same section header section everytime, try not to make the logo too big.
        For the body section;
        Address it to the name value.
        The document type is the email theme.
        There should also be a professional style sentiment applied to the required content here.
        The required content should be communicated in a friendly professional manner.
        Can you please put a bunch of legal jargon in the footer.
        If any of the brand colors are missing just fill in the blanks with complementing color schemes, if they are all empty
        just randomly choose a color scheme.
        Try to add some nice design practices into it.";

        // Prepare API request
        var apiRequest = new OpenAIAPIRequest
        {
            model = "gpt-4",
            messages = new List<Message>
            {
                new() { role = "system", content = systemContext }, // The rest of your system message
                new() { role = "user", content = requestBody }
            },
            temperature = 0,
            max_tokens = 2048
        };

        var responseFromOpenAI = await SendPostRequestAsync(apiRequest);

        if (string.IsNullOrEmpty(responseFromOpenAI))
            throw new Exception("OpenAI API returned an empty response.");

        var jsonObject = JObject.Parse(responseFromOpenAI);
        var assistantResponseContent = jsonObject["choices"][0]["message"]["content"].ToString();

        return assistantResponseContent;
    }

    private static async Task<string> SendPostRequestAsync(OpenAIAPIRequest apiRequest)
    {
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var jsonContent = JsonConvert.SerializeObject(apiRequest, Formatting.Indented);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(url, httpContent);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }

        return null;
    }
}