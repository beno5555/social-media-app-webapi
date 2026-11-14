using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Infrastructure.Queries;

public class PageQuery
{
    [Range(Constants.FirstPageNumber, Constants.MaxPageNumber)]
    public int PageNumber { get; set; } = Constants.FirstPageNumber;

    [Range(Constants.MinPageSize, Constants.MaxPageSize)]
    public int PageSize { get; set; } = Constants.DefaultPageSize;
}