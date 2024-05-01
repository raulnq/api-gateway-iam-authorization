using Amazon.Extensions.NETCore.Setup;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Aws4RequestSigner;
using Microsoft.AspNetCore.Mvc;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonSecurityTokenService>();
var app = builder.Build();
var endpoint = builder.Configuration.GetValue<string>("AWS_LAMBDA_ENDPOINT")!;
var role = builder.Configuration.GetValue<string>("AWS_ROLE_TO_ASSUME")!;
var region = builder.Configuration.GetValue<string>("AWS_REGION")!;
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/proxy", async ([FromServices] IAmazonSecurityTokenService stsClient) =>
{
    var assumeRoleRequest = new AssumeRoleRequest
    {
        RoleArn = role,
        RoleSessionName = Guid.NewGuid().ToString(),
        DurationSeconds = 3600
    };

    var assumeRoleResponse = await stsClient.AssumeRoleAsync(assumeRoleRequest);
    var credentials = assumeRoleResponse.Credentials;

    var signer = new AWS4RequestSigner(credentials.AccessKeyId, credentials.SecretAccessKey);
    var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
    var request = new HttpRequestMessage
    {
        Method = HttpMethod.Get,
        RequestUri = new Uri(endpoint),
        Content = content
    };
    request.Headers.TryAddWithoutValidation("X-Amz-Security-Token", credentials.SessionToken);
    request = await signer.Sign(request, "execute-api", region);
    var client = new HttpClient();
    var response = await client.SendAsync(request);
    return await response.Content.ReadAsStringAsync();
})
.WithOpenApi();

app.Run();