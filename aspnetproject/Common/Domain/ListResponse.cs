namespace aspnetproject.Common.Domain;

public class ListResponse<T> : ApplicationResponse
{
    public List<T> Data       { get; set; } = [];
    public int     TotalCount => Data.Count;

    public void Ok(List<T> data, string? message = null)
    {
        Succeeded = true;
        Data = data;
        Message = message;
    }

    public override void Fail(string? message = null)
    {
        base.Fail(message);
        Data = [];
    }
}