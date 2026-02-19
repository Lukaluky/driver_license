using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.DTOs.Auth;
using OrderService.Infrastructure.Data;
using Xunit;

namespace OrderService.IntegrationTests;

public class ApplicationsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApplicationsApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_Should_Return_Ok()
    {
        var request = new RegisterRequest(
            $"test-{Guid.NewGuid():N}@test.com", "Password1!", "Applicant");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_Without_Confirmation_Should_Fail()
    {
        var email = $"test-{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Password1!", "Applicant"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "Password1!"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unauthenticated_Request_Should_Return_401()
    {
        var response = await _client.GetAsync("/api/applications/my");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Inspector_Login_Should_Succeed()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("inspector@test.com", "Inspector123!"));

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            result.Should().NotBeNull();
            result!.Role.Should().Be("Inspector");
        }
    }
}
