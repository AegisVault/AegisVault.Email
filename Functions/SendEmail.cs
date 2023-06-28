using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using AegisVault.Email.Helpers;
using AegisVault.Email.Models;
using Azure;
using Azure.Communication.Email;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Mjml.Net;
using Newtonsoft.Json;

namespace AegisVault.Email.Functions;

public static class SendEmail
{
    
    [FunctionName("SendEmail")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "Post", Route = null)]
        HttpRequest req, ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        sendEmailRequest emailRequest;
        try
        {
            // Deserialize requestBody into RootObject
            emailRequest = JsonConvert.DeserializeObject<sendEmailRequest>(requestBody);
            if (emailRequest == null)
            {
                throw new Exception("Invalid JSON");
            }
            
        }
        catch (Exception e)
        {
            log.LogError(e, "Failed to parse request body");
            return new BadRequestObjectResult("Invalid request body");
        }

        try
        {
            log.LogInformation("Starting Email Generation...");
            var generatedEmailContent = await GenerateTemplate.GenerateEmailTemplate(requestBody);
            log.LogInformation("Completed Email Generation...");

            try
            {
                log.LogInformation("Preparing and sending Email ...");
                await PrepareEmail.SendEmail(emailRequest, generatedEmailContent, log, requestBody);
                log.LogInformation("EmailSent!");
                return new OkResult();
            }
            catch (Exception e)
            {
                log.LogError(e, "Failed to send email");
                return new BadRequestResult();
            }
        }
        catch (Exception e)
        {
            log.LogError(e, "Failed to generate email template");
            return new BadRequestResult();
        }
    }
}