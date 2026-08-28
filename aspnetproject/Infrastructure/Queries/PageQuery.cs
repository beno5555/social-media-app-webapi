namespace aspnetproject.Infrastructure.Queries;

public class PageQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize   { get; set; } = 10;
}