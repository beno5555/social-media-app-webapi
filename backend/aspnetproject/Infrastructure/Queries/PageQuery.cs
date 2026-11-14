using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Infrastructure.Queries;

public class PageQuery
{
    [Range(Constants.FirstPageNumber, int.MaxValue)]
    public int PageNumber { get; set; } = Constants.FirstPageNumber;

    [Range(1, Constants.MaxPageSize)] 
    public int PageSize { get; set; } = Constants.DefaultPageSize;
}