using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
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

namespace AegisVault.Email.Functions;

public static class PrepareEmail
{
    
    public static async Task<bool> SendEmail(sendEmailRequest requestDetails, string mjmlContent, ILogger log, string requestBody)
    {

        log.LogInformation("MJMLContent: " + mjmlContent);
        string connectionString =
            "endpoint=https://aegisvault-commservice.communication.azure.com/;accesskey=j28Ke6xqXDrI3rmnhP4WpvI2FFoj6mWYut3Z8g+u1rBa6NfMQUBnItAGN449N5NaA78nPynyt0tFqYLR97IBtQ==";
        EmailClient emailClient = new EmailClient(connectionString);

        string mjmlTemplate = mjmlContent;

        var mjmlRenderer = new MjmlRenderer();
        string htmlContent = "";
        bool isSuccessful = false;
        int retries = 3; // Define how many times you want to retry

        while (!isSuccessful && retries > 0)
        {
            try
            {
                // Attempt to render the template
                log.LogInformation("Rendering template...");
                log.LogInformation("MJMLTemplate: " + mjmlTemplate);
                htmlContent = mjmlRenderer.Render(mjmlTemplate, new MjmlOptions { Beautify = false }).Html;
                isSuccessful = true;
            }
            catch (Exception e)
            {
                // If an error occurs, log the error and regenerate the template
                log.LogInformation("Error rendering template: " + e.Message);
                log.LogInformation("Retrying...");
                retries--;

                // Regenerate the mjml template
                mjmlTemplate = await GenerateTemplate.GenerateEmailTemplate(requestBody);
            }
        }

        if (!isSuccessful)
        {
            log.LogInformation("Failed to render the template after 3 attempts.");
            return false;
        }


        log.LogInformation("HTMLcontent: " + htmlContent);
        
        var subjectBuilder = new StringBuilder("AegisVault Document");

        if (!string.IsNullOrEmpty(requestDetails.brand.brandname))
        {
            subjectBuilder.Append(" - " + requestDetails.brand.brandname);
        }

        if (!string.IsNullOrEmpty(requestDetails.accountNumber))
        {
            subjectBuilder.Append(" - " + requestDetails.accountNumber);
        }

        var subject = subjectBuilder.ToString();
        log.LogInformation("subject: " + subject);
        
        //var htmlContent = "<html><body><h1>Quick send email test</h1><br/><h4>This email message is sent from Azure Communication Service Email.</h4><p>This mail was sent using .NET SDK!!</p></body></html>";
        var senderList = new List<string> {"Zeus@aegisvault.dev","Athena@aegisvault.dev"};
        var random = new Random();
        var sender = senderList[random.Next(senderList.Count)];
        log.LogInformation("Sender that won: " + sender);
        var recipient = requestDetails.email;
        log.LogInformation("recipient" + recipient);

        try
        {
            log.LogInformation("Sending email...");
            EmailSendOperation emailSendOperation = await emailClient.SendAsync(
                Azure.WaitUntil.Completed,
                sender,
                recipient,
                subject,
                htmlContent);
            EmailSendResult statusMonitor = emailSendOperation.Value;
    
            log.LogInformation($"Email Sent. Status = {emailSendOperation.Value.Status}");

            /// Get the OperationId so that it can be used for tracking the message for troubleshooting
            string operationId = emailSendOperation.Id;
            log.LogInformation($"Email operation id = {operationId}");
            
            return true;
        }
        catch (RequestFailedException ex)
        {
            /// OperationID is contained in the exception message and can be used for troubleshooting purposes
            log.LogInformation($"Email send operation failed with error code: {ex.ErrorCode}, message: {ex.Message}");
            return false;
        }
        
    }
}