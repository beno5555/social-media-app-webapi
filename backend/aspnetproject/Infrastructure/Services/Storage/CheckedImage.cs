namespace aspnetproject.Infrastructure.Services.Storage;

// An upload that passed every check, with the extension its content (not its name) calls for.
public record CheckedImage(IFormFile File, string Extension);
