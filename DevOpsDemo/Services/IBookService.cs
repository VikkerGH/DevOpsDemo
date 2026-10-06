using DevOpsDemo.Contracts;

namespace DevOpsDemo.Services;

// Controlleren bruger denne kontrakt uden at kende den konkrete lagring.
public interface IBookService
{
    IReadOnlyList<BookResponse> GetAll(CancellationToken cancellationToken);
    BookResponse? GetById(int id, CancellationToken cancellationToken);
    BookResponse Create(CreateBookRequest request, CancellationToken cancellationToken);
    BookResponse? Update(int id, UpdateBookRequest request, CancellationToken cancellationToken);
    bool Delete(int id, CancellationToken cancellationToken);
}
