using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Infrastructure.Queries;

public class PageQuery
{
    public int PageNumber { get; set; } = 1;
    
    [MaxLength(Constants.MaxPageSize)]
    public int PageSize   { get; set; } = Constants.DefaultPageSize;
}