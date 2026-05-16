namespace MoteaManageAPI.Models;

public record ProjectDto(
    Guid Id,
    string Title,
    DateTime Date,
    string? Description,
    string Status,
    string Category,
    string? LandingImg);

public record CreateProjectRequest(
    string Title,
    DateTime Date,
    string? Description,
    string Status,
    string Category,
    string? LandingImg);
