﻿using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OpenBullet2.Web.Models.Errors;
using OpenBullet2.Web.Tests.Utils;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit.Abstractions;

namespace OpenBullet2.Web.Tests.Integration;

[Collection("IntegrationTests")]
public class IntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new();
    private readonly IServiceScope _serviceScope;
    protected WebApplicationFactory<Program> Factory { get; }
    
    protected IntegrationTests(ITestOutputHelper testOutputHelper)
    {
        Factory = new WebApplicationFactory<Program>();
        _testOutputHelper = testOutputHelper;
        
        var enumConverter = new JsonStringEnumConverter(JsonNamingPolicy.CamelCase);
        _jsonSerializerOptions.Converters.Add(enumConverter);
        _jsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        
        // Override the user data folder and connection string
        // to avoid conflicts with other tests
        var userDataFolder = Path.Combine(Path.GetTempPath(), $"OB2_UserData_{Guid.NewGuid():N}");
        Environment.SetEnvironmentVariable("Settings__UserDataFolder", userDataFolder);
        
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection",
            $"Data Source={userDataFolder}/OpenBullet.db;");
        
        _serviceScope = Factory.Services.CreateScope();
    }
    
    protected T GetRequiredService<T>() where T : notnull =>
        _serviceScope.ServiceProvider.GetRequiredService<T>();
    
    protected Task<Result<T, ApiErrorResponse>> GetJsonAsync<T>(HttpClient client, string url)
        => GetJsonAsync<T>(client, new Uri(url, UriKind.Relative));
 
    protected async Task<Result<T, ApiErrorResponse>> GetJsonAsync<T>(HttpClient client, Uri url)
    {
        var response = await client.GetAsync(url);
        var jsonResponse = await response.Content.ReadAsStringAsync();
 
        if (!response.IsSuccessStatusCode)
        {
            _testOutputHelper.WriteLine($"API status code: {response.StatusCode}");
            _testOutputHelper.WriteLine($"API response: {jsonResponse}");
 
            try
            {
                return new ApiErrorResponse
                {
                    Content = JsonSerializer.Deserialize<ApiError>(jsonResponse, _jsonSerializerOptions)!,
                    Response = response
                }!;
            }
            catch (JsonException)
            {
                return new ApiErrorResponse
                {
                    Response = response
                }!;
            }
        }
 
        return JsonSerializer.Deserialize<T>(jsonResponse, _jsonSerializerOptions)!;
    }
 
    protected async Task<ApiErrorResponse?> PostJsonAsync(HttpClient client, string url, object dto)
        => await PostJsonAsync(client, new Uri(url, UriKind.Relative), dto);
 
    protected async Task<ApiErrorResponse?> PostJsonAsync(HttpClient client, Uri url, object dto)
    {
        var json = JsonContent.Create(dto, MediaTypeHeaderValue.Parse("application/json"), _jsonSerializerOptions);
        var response = await client.PostAsync(url, json);
        var jsonResponse = await response.Content.ReadAsStringAsync();
 
        if (!response.IsSuccessStatusCode)
        {
            _testOutputHelper.WriteLine($"API status code: {response.StatusCode}");
            _testOutputHelper.WriteLine($"API response: {jsonResponse}");
 
            try
            {
                return new ApiErrorResponse
                {
                    Content = JsonSerializer.Deserialize<ApiError>(jsonResponse, _jsonSerializerOptions)!,
                    Response = response
                };
            }
            catch (JsonException)
            {
                return new ApiErrorResponse
                {
                    Response = response
                };
            }
        }
 
        return null;
    }
 
    protected async Task<Result<T, ApiErrorResponse>> PostJsonAsync<T>(HttpClient client, string url, object dto)
        => await SendJsonAsync<T>(client, new Uri(url, UriKind.Relative), dto, HttpMethod.Post);
 
    protected async Task<Result<T, ApiErrorResponse>> PutJsonAsync<T>(HttpClient client, string url, object dto)
        => await SendJsonAsync<T>(client, new Uri(url, UriKind.Relative), dto, HttpMethod.Put);
    
    protected async Task<Result<T, ApiErrorResponse>> PatchJsonAsync<T>(HttpClient client, string url, object dto)
        => await SendJsonAsync<T>(client, new Uri(url, UriKind.Relative), dto, HttpMethod.Patch);
    
    protected async Task<Result<T, ApiErrorResponse>> SendJsonAsync<T>(HttpClient client, Uri url, object dto,
        HttpMethod method)
    {
        var json = JsonContent.Create(dto, MediaTypeHeaderValue.Parse("application/json"), _jsonSerializerOptions);
        var request = new HttpRequestMessage(method, url) { Content = json };
        var response = await client.SendAsync(request);
        var jsonResponse = await response.Content.ReadAsStringAsync();
 
        if (!response.IsSuccessStatusCode)
        {
            _testOutputHelper.WriteLine($"API status code: {response.StatusCode}");
            _testOutputHelper.WriteLine($"API response: {jsonResponse}");
 
            try
            {
                return new ApiErrorResponse
                {
                    Content = JsonSerializer.Deserialize<ApiError>(jsonResponse, _jsonSerializerOptions)!,
                    Response = response
                }!;
            }
            catch (JsonException)
            {
                return new ApiErrorResponse
                {
                    Response = response
                }!;
            }
        }
 
        return JsonSerializer.Deserialize<T>(jsonResponse, _jsonSerializerOptions)!;
    }

    protected async Task<ApiErrorResponse?> DeleteAsync(HttpClient client, string url)
        => await DeleteAsync(client, new Uri(url, UriKind.Relative));
 
    protected async Task<ApiErrorResponse?> DeleteAsync(HttpClient client, Uri url)
    {
        var response = await client.DeleteAsync(url);
        var jsonResponse = await response.Content.ReadAsStringAsync();
 
        if (!response.IsSuccessStatusCode)
        {
            _testOutputHelper.WriteLine($"API status code: {response.StatusCode}");
            _testOutputHelper.WriteLine($"API response: {jsonResponse}");
 
            try
            {
                return new ApiErrorResponse
                {
                    Content = JsonSerializer.Deserialize<ApiError>(jsonResponse, _jsonSerializerOptions)!,
                    Response = response
                };
            }
            catch (JsonException)
            {
                return new ApiErrorResponse
                {
                    Response = response
                };
            }
        }
 
        return null;
    }

    public void Dispose()
    {
        _serviceScope.Dispose();
        Factory.Dispose();
    }
}
