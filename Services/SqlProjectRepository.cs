using Microsoft.Data.SqlClient;
using System.Data;
using MoteaManageAPI.Models;

namespace MoteaManageAPI.Services;

public class SqlProjectRepository(IConfiguration configuration) : IProjectRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("MoteaManageSql")
        ?? throw new InvalidOperationException("Connection string 'MoteaManageSql' is missing.");

    public async Task<IReadOnlyList<ProjectDto>> GetAllAsync(string userId, CancellationToken cancellationToken = default)
    {
        var projects = new List<ProjectDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
                           SELECT Id, Title, [Date], Description, Status, Category, LandingImg
                           FROM dbo.Projects
                           WHERE UserId = @UserId
                           ORDER BY [Date] DESC;
                           """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            projects.Add(MapProject(reader));
        }

        return projects;
    }

    public async Task<ProjectDto?> GetByIdAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
                           SELECT Id, Title, [Date], Description, Status, Category, LandingImg
                           FROM dbo.Projects
                           WHERE Id = @Id AND UserId = @UserId;
                           """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@UserId", userId.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapProject(reader);
    }

    public async Task<ProjectDto> CreateAsync(string userId, CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        byte[]? landingImgBytes = null;
        if (!string.IsNullOrWhiteSpace(request.LandingImg))
        {
            try
            {
                landingImgBytes = Convert.FromBase64String(request.LandingImg);
            }
            catch (FormatException)
            {
                throw new ArgumentException("LandingImg must be a valid base64 string.", nameof(request));
            }
        }

        const string sql = """
                           INSERT INTO dbo.Projects (UserId, Title, [Date], Description, Status, Category, LandingImg)
                           OUTPUT INSERTED.Id, INSERTED.Title, INSERTED.[Date], INSERTED.Description, INSERTED.Status, INSERTED.Category, INSERTED.LandingImg
                           VALUES (@UserId, @Title, @Date, @Description, @Status, @Category, @LandingImg);
                           """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId.Trim());
        command.Parameters.AddWithValue("@Title", request.Title.Trim());
        command.Parameters.AddWithValue("@Date", request.Date);
        command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@Status", request.Status.Trim());
        command.Parameters.AddWithValue("@Category", request.Category.Trim());
        var landingImgParameter = command.Parameters.Add("@LandingImg", SqlDbType.VarBinary, -1);
        landingImgParameter.Value = (object?)landingImgBytes ?? DBNull.Value;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        return MapProject(reader);
    }

    private static ProjectDto MapProject(SqlDataReader reader)
    {
        return new ProjectDto(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetDateTime(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.IsDBNull(6) ? null : Convert.ToBase64String((byte[])reader[6]));
    }
}
