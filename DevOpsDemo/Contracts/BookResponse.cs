namespace DevOpsDemo.Contracts;

/// <summary>De bogoplysninger, klienten modtager som JSON.</summary>
public sealed record BookResponse(int Id, string Title, string Author, bool IsRead);
