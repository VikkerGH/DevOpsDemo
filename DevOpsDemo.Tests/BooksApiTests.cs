using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DevOpsDemo.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DevOpsDemo.Tests;

// Integrationstests kalder de rigtige HTTP-endpoints, ikke blot controller-metoder.
public sealed class BooksApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });

    [Fact]
    public async Task BookCanBeCreatedRetrievedUpdatedAndDeleted()
    {
        using var client = CreateClient();
        var request = new CreateBookRequest { Title = "Min bog", Author = "Forfatter" };

        using var created = await client.PostAsJsonAsync("/api/books", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var book = await created.Content.ReadFromJsonAsync<BookResponse>();
        Assert.NotNull(book);
        Assert.NotNull(created.Headers.Location);
        Assert.Equal($"/api/books/{book.Id}", created.Headers.Location.AbsolutePath);
        Assert.Equal(book, await client.GetFromJsonAsync<BookResponse>(created.Headers.Location));

        using var updated = await client.PutAsJsonAsync(created.Headers.Location,
            new UpdateBookRequest { Title = "Ny titel", Author = "Ny forfatter", IsRead = true });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(new BookResponse(book.Id, "Ny titel", "Ny forfatter", true),
            await updated.Content.ReadFromJsonAsync<BookResponse>());

        using var deleted = await client.DeleteAsync(created.Headers.Location);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal("", await deleted.Content.ReadAsStringAsync());
        using var missing = await client.GetAsync(created.Headers.Location);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task GetAllReturnsJsonArray()
    {
        using var client = CreateClient();
        using var response = await client.GetAsync("/api/books");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var books = await response.Content.ReadFromJsonAsync<List<BookResponse>>();
        Assert.NotNull(books);
        Assert.Contains(books, book => book.Id == 1);
    }

    [Theory]
    [InlineData("", "Forfatter")]
    [InlineData("   ", "Forfatter")]
    [InlineData("Titel", "")]
    [InlineData("Titel", "   ")]
    public async Task InvalidCreateReturnsValidationProblem(string title, string author)
    {
        using var client = CreateClient();
        using var response = await client.PostAsJsonAsync("/api/books",
            new CreateBookRequest { Title = title, Author = author });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.NotEmpty(problem.Errors);
    }

    [Theory]
    [InlineData(201, 120)]
    [InlineData(200, 121)]
    public async Task TooLongFieldsReturnBadRequest(int titleLength, int authorLength)
    {
        using var client = CreateClient();
        using var response = await client.PostAsJsonAsync("/api/books",
            new CreateBookRequest { Title = new string('T', titleLength), Author = new string('A', authorLength) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task UnknownBookReturnsNotFoundProblem(string method)
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), "/api/books/2147483647");
        if (method == "PUT")
        {
            request.Content = JsonContent.Create(new UpdateBookRequest { Title = "Titel", Author = "Forfatter", IsRead = false });
        }

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Bogen findes ikke.", problem.Title);
    }

    [Theory]
    [InlineData("{ \"author\": \"Forfatter\" }")]
    [InlineData("{ \"title\": \"Titel\", \"author\": null }")]
    [InlineData("{ invalid json }")]
    public async Task MissingFieldsAndMalformedJsonReturnBadRequest(string json)
    {
        using var client = CreateClient();
        using var response = await client.PostAsync("/api/books", new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutRequiresReadStatus()
    {
        using var client = CreateClient();
        using var response = await client.PutAsJsonAsync("/api/books/1", new { title = "Titel", author = "Forfatter" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InvalidPutDoesNotChangeBook()
    {
        using var client = CreateClient();
        var original = await client.GetFromJsonAsync<BookResponse>("/api/books/1");

        using var response = await client.PutAsJsonAsync("/api/books/1",
            new UpdateBookRequest { Title = " ", Author = "Forfatter", IsRead = true });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(original, await client.GetFromJsonAsync<BookResponse>("/api/books/1"));
    }

    [Theory]
    [InlineData("/", "text/html")]
    [InlineData("/app.js", "text/javascript")]
    [InlineData("/styles.css", "text/css")]
    public async Task FrontendAssetsAreServed(string path, string mediaType)
    {
        using var client = CreateClient();
        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(mediaType, response.Content.Headers.ContentType?.MediaType);
        Assert.NotEmpty(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task OpenApiDescribesAllBookRoutes()
    {
        using var client = CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");

        Assert.True(paths.GetProperty("/api/books").TryGetProperty("get", out _));
        Assert.True(paths.GetProperty("/api/books").TryGetProperty("post", out _));
        var single = paths.GetProperty("/api/books/{id}");
        Assert.True(single.TryGetProperty("get", out _));
        Assert.True(single.TryGetProperty("put", out _));
        Assert.True(single.TryGetProperty("delete", out _));
    }

    [Fact]
    public async Task HealthCheckReturnsOk()
    {
        using var client = CreateClient();
        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
