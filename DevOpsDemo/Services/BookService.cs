using System.ComponentModel.DataAnnotations;
using DevOpsDemo.Contracts;
using DevOpsDemo.Models;

namespace DevOpsDemo.Services;

public sealed class BookService : IBookService
{
    private readonly Dictionary<int, Book> _books = new()
    {
        [1] = new Book(1, "Den lille havfrue", "H. C. Andersen", false),
        [2] = new Book(2, "Pride and Prejudice", "Jane Austen", true)
    };
    private int _nextId = 3;
    private readonly object _gate = new();

    public IReadOnlyList<BookResponse> GetAll(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Flere HTTP-kald kan komme samtidigt. lock beskytter det delte lager.
        lock (_gate)
        {
            return _books.Values.OrderBy(book => book.Id).Select(ToResponse).ToArray();
        }
    }

    public BookResponse? GetById(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return _books.TryGetValue(id, out var book) ? ToResponse(book) : null;
        }
    }

    public BookResponse Create(CreateBookRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Validate(request);
        lock (_gate)
        {
            var book = new Book(_nextId++, request.Title.Trim(), request.Author.Trim(), request.IsRead);
            _books.Add(book.Id, book);
            return ToResponse(book);
        }
    }

    public BookResponse? Update(int id, UpdateBookRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Validate(request);
        lock (_gate)
        {
            if (!_books.ContainsKey(id))
            {
                return null; // Controlleren oversætter "ikke fundet" til HTTP 404.
            }

            var book = new Book(id, request.Title.Trim(), request.Author.Trim(), request.IsRead);
            _books[id] = book;
            return ToResponse(book);
        }
    }

    public bool Delete(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return _books.Remove(id);
        }
    }

    private static void Validate(object request)
    {
        // Samme annotations bruges af API'et og ved direkte kald fra unit tests.
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
    }

    private static BookResponse ToResponse(Book book) =>
        new(book.Id, book.Title, book.Author, book.IsRead);
}
