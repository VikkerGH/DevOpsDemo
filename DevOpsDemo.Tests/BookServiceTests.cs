using System.ComponentModel.DataAnnotations;
using DevOpsDemo.Contracts;
using DevOpsDemo.Services;

namespace DevOpsDemo.Tests;

public sealed class BookServiceTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    [Fact]
    public void Create_TrimsInputAndAssignsId()
    {
        // Arrange: hver test har sit eget lager, så tests ikke påvirker hinanden.
        var service = new BookService();
        var request = new CreateBookRequest { Title = "  Min bog  ", Author = "  En forfatter  " };

        var book = service.Create(request, Ct);

        Assert.Equal(3, book.Id);
        Assert.Equal("Min bog", book.Title);
        Assert.Equal("En forfatter", book.Author);
        Assert.False(book.IsRead);
        Assert.Equal(book, service.GetById(book.Id, Ct));
    }

    [Theory]
    [InlineData("", "Forfatter")]
    [InlineData("   ", "Forfatter")]
    [InlineData("Titel", "")]
    [InlineData("Titel", "   ")]
    public void Create_RejectsEmptyFieldsWithoutChangingCatalogue(string title, string author)
    {
        var service = new BookService();
        var request = new CreateBookRequest { Title = title, Author = author };

        Assert.Throws<ValidationException>(() => service.Create(request, Ct));
        Assert.Equal(2, service.GetAll(Ct).Count);
    }

    [Theory]
    [InlineData(201, 120)]
    [InlineData(200, 121)]
    public void Create_RejectsFieldsExceedingMaximumLength(int titleLength, int authorLength)
    {
        var service = new BookService();
        var request = new CreateBookRequest { Title = new string('T', titleLength), Author = new string('A', authorLength) };

        Assert.Throws<ValidationException>(() => service.Create(request, Ct));
    }

    [Fact]
    public void Create_AcceptsMaximumLengths()
    {
        var service = new BookService();
        var request = new CreateBookRequest { Title = new string('T', 200), Author = new string('A', 120) };

        var book = service.Create(request, Ct);

        Assert.Equal(200, book.Title.Length);
        Assert.Equal(120, book.Author.Length);
    }

    [Fact]
    public void Update_ReplacesAllFieldsButKeepsId()
    {
        var service = new BookService();
        var request = new UpdateBookRequest { Title = "  Ny titel  ", Author = "  Ny forfatter  ", IsRead = true };

        var updated = service.Update(1, request, Ct);

        Assert.Equal(new BookResponse(1, "Ny titel", "Ny forfatter", true), updated);
        Assert.Equal(updated, service.GetById(1, Ct));
        Assert.Equal(2, service.GetAll(Ct).Count);
    }

    [Fact]
    public void Update_InvalidInputDoesNotChangeBook()
    {
        var service = new BookService();
        var original = service.GetById(1, Ct);
        var request = new UpdateBookRequest { Title = " ", Author = "Forfatter", IsRead = true };

        Assert.Throws<ValidationException>(() => service.Update(1, request, Ct));
        Assert.Equal(original, service.GetById(1, Ct));
    }

    [Fact]
    public void UnknownIdsDoNotCreateResources()
    {
        var service = new BookService();
        var request = new UpdateBookRequest { Title = "Titel", Author = "Forfatter", IsRead = false };

        Assert.Null(service.GetById(999, Ct));
        Assert.Null(service.Update(999, request, Ct));
        Assert.False(service.Delete(999, Ct));
        Assert.Equal(2, service.GetAll(Ct).Count);
    }

    [Fact]
    public void Delete_RemovesBookAndDoesNotReuseId()
    {
        var service = new BookService();
        var request = new CreateBookRequest { Title = "Titel", Author = "Forfatter" };
        var first = service.Create(request, Ct);

        Assert.True(service.Delete(first.Id, Ct));
        Assert.Null(service.GetById(first.Id, Ct));
        Assert.False(service.Delete(first.Id, Ct));
        Assert.True(service.Create(request, Ct).Id > first.Id);
    }

    [Fact]
    public void GetAll_ReturnsSnapshotRatherThanLiveStore()
    {
        var service = new BookService();
        var snapshot = service.GetAll(Ct);

        service.Delete(1, Ct);

        Assert.Equal(2, snapshot.Count);
        Assert.Single(service.GetAll(Ct));
    }

    [Fact]
    public async Task ConcurrentCreatesHaveUniqueIds()
    {
        var service = new BookService();
        var request = new CreateBookRequest { Title = "Titel", Author = "Forfatter" };

        var results = await Task.WhenAll(Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => service.Create(request, Ct))));

        Assert.Equal(100, results.Select(book => book.Id).Distinct().Count());
        Assert.Equal(102, service.GetAll(Ct).Count);
    }

    [Fact]
    public void CancelledRequestDoesNotCreateBook()
    {
        var service = new BookService();
        var request = new CreateBookRequest { Title = "Titel", Author = "Forfatter" };

        Assert.Throws<OperationCanceledException>(() => service.Create(request, new CancellationToken(true)));
        Assert.Equal(2, service.GetAll(Ct).Count);
    }
}
