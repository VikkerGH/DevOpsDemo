namespace DevOpsDemo.Models;

// Den interne model er adskilt fra API-kontrakten, så lageret kan skiftes senere.
internal sealed record Book(int Id, string Title, string Author, bool IsRead);
