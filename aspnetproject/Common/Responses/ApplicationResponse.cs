namespace aspnetproject.Common.Responses;

public class ApplicationResponse
{
    public bool    Succeeded { get; set; } = true;
    public string? Message { get; set; }

    public void Ok(string? message = null)
    {
        Succeeded = true;
        Message = message;
    }

    public virtual void Fail(string? message = null)
    {
        Succeeded = false;
        Message = message;
    }
}
public class ApplicationResponse<T> : ApplicationResponse
{
    public T? Data { get; set; }
    
    public void Ok(T data, string? message = null)
    {
        Succeeded = true;
        Message = message;
        Data = data;
    }

    public override void Fail(string? message = null)
    {
        base.Fail(message);
        Data = default;
    }
}
