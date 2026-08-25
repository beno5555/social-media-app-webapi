namespace aspnetproject.Common.Responses;

public class ListResponse<T>
{
    public List<T> Data       { get; set; } = [];
    public int     TotalCount => Data.Count;
}