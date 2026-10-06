using DevOpsDemo.Contracts;
using DevOpsDemo.Services;
using Microsoft.AspNetCore.Mvc;

namespace DevOpsDemo.Controllers;

[ApiController] // Validerer automatisk JSON-input og returnerer 400 ved ugyldige data.
[Route("api/books")]
public sealed class BooksController(IBookService books) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Hent alle bøger i kataloget.")]
    [ProducesResponseType<IReadOnlyList<BookResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<BookResponse>> GetAll(CancellationToken cancellationToken) =>
        Ok(books.GetAll(cancellationToken));

    [HttpGet("{id:int}")]
    [EndpointSummary("Hent en bog ud fra dens id.")]
    [ProducesResponseType<BookResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<BookResponse> GetById(int id, CancellationToken cancellationToken)
    {
        var book = books.GetById(id, cancellationToken);
        return book is null ? BookNotFound() : Ok(book);
    }

    [HttpPost]
    [EndpointSummary("Opret en bog. Serveren tildeler et id.")]
    [ProducesResponseType<BookResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<BookResponse> Create(CreateBookRequest request, CancellationToken cancellationToken)
    {
        var book = books.Create(request, cancellationToken);
        // 201 Created og Location fortæller klienten, hvor den nye bog kan hentes.
        return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Erstat titel, forfatter og læsestatus for en eksisterende bog.")]
    [ProducesResponseType<BookResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<BookResponse> Update(int id, UpdateBookRequest request, CancellationToken cancellationToken)
    {
        var book = books.Update(id, request, cancellationToken);
        return book is null ? BookNotFound() : Ok(book);
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Slet en eksisterende bog.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult Delete(int id, CancellationToken cancellationToken) =>
        books.Delete(id, cancellationToken) ? NoContent() : BookNotFound();

    private ObjectResult BookNotFound() =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Bogen findes ikke.");
}
