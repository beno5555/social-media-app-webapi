using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories;
using aspnetproject.Infrastructure.Dtos.Reports;
using aspnetproject.Infrastructure.Mappers;
using aspnetproject.Infrastructure.Queries;
using aspnetproject.Infrastructure.Services.BusinessLogic.Base;
using aspnetproject.Infrastructure.Services.Logging;

namespace aspnetproject.Infrastructure.Services.BusinessLogic.Content;

public class ReportService : BaseService
{
    private readonly ReportRepository  _reportRepository;
    private readonly PostRepository    _postRepository;
    private readonly CommentRepository _commentRepository;
    private readonly UserRepository    _userRepository;

    public ReportService(
        ReportRepository  reportRepository,
        PostRepository    postRepository,
        CommentRepository commentRepository,
        UserRepository    userRepository,
        DatabaseLogger    dbLogger
        ) : base(dbLogger)
    {
        _reportRepository  = reportRepository;
        _postRepository    = postRepository;
        _commentRepository = commentRepository;
        _userRepository    = userRepository;
    }

    // The target must be one the caller may read, so hidden content reads as missing, and must not be the caller or theirs.
    public async Task<ApplicationResponse<StandardReportDto>> CreateReportAsync(int reporterId, bool isAdmin, CreateReportDto createReportDto)
    {
        var response   = new ApplicationResponse<StandardReportDto>();
        var targetType = createReportDto.TargetType!.Value;
        var targetId   = createReportDto.TargetId!.Value;

        var targetCheck = await ValidateTargetAsync(reporterId, isAdmin, targetType, targetId);

        if (targetCheck.Succeeded)
        {
            bool noPendingReport = !await _reportRepository.HasPendingReportAsync(reporterId, targetType, targetId);

            if (noPendingReport)
            {
                var report = ReportMapper.ToEntity(reporterId, createReportDto);
                await _reportRepository.AddAsync(report);

                response.Ok(ReportMapper.ToStandardDisplay(report), ResponseMessages.ReportCreated);
                await LogResultAsync(response.Succeeded, nameof(CreateReportAsync), nameof(Report), $"Reported {targetType} {targetId}", report.Id);
            }
            else
            {
                response.Fail(ResponseMessages.ReportAlreadyPending);
                await LogResultAsync(response.Succeeded, nameof(CreateReportAsync), nameof(Report), $"Could not report {targetType} {targetId}. {response.Message}", null);
            }
        }
        else
        {
            response.Fail(targetCheck.Message);
            await LogResultAsync(response.Succeeded, nameof(CreateReportAsync), nameof(Report), $"Could not report {targetType} {targetId}. {response.Message}", null);
        }

        return response;
    }

    // A report's target has no foreign key, so the page's targets are loaded afterwards: one query per target type.
    // A target id that finds nothing is a target that no longer exists.
    public async Task<ListResponse<AdminReportDto>> GetReportsAsync(ReportListQuery reportQuery)
    {
        var response = new ListResponse<AdminReportDto>();

        var reports = await _reportRepository.GetReportsAsync(reportQuery);

        var postIds    = TargetIdsOf(reports, ReportTargetType.Post);
        var commentIds = TargetIdsOf(reports, ReportTargetType.Comment);
        var userIds    = TargetIdsOf(reports, ReportTargetType.User);

        var posts    = (await _postRepository.GetSnapshotsByIdsAsync(postIds)).ToDictionary(post => post.Id);
        var comments = (await _commentRepository.GetSnapshotsByIdsAsync(commentIds)).ToDictionary(comment => comment.Id);
        var users    = (await _userRepository.GetByIdsNoFilterAsync(userIds)).ToDictionary(user => user.Id);

        var reportDtos = reports
            .Select(report => ReportMapper.ToAdminDisplay(
                report,
                report.TargetType == ReportTargetType.Post    ? posts.GetValueOrDefault(report.TargetId)    : null,
                report.TargetType == ReportTargetType.Comment ? comments.GetValueOrDefault(report.TargetId) : null,
                report.TargetType == ReportTargetType.User    ? users.GetValueOrDefault(report.TargetId)    : null))
            .ToList();

        response.Ok(reportDtos, ResponseMessages.ReportsRetrieved);

        return response;
    }

    private List<int> TargetIdsOf(List<Report> reports, ReportTargetType targetType)
    {
        return reports
            .Where(report => report.TargetType == targetType)
            .Select(report => report.TargetId)
            .Distinct()
            .ToList();
    }

    private async Task<ApplicationResponse> ValidateTargetAsync(int reporterId, bool isAdmin, ReportTargetType targetType, int targetId)
    {
        var response = new ApplicationResponse();

        if (targetType == ReportTargetType.Post)
        {
            var  post        = await _postRepository.GetReadablePostAsync(targetId, reporterId, isAdmin);
            bool postFound   = post is not null;
            bool isOwnPost   = post?.UserId == reporterId;
            response = CheckTargetOwnership(postFound, isOwnPost, ResponseMessages.PostNotFound, ResponseMessages.CannotReportOwnContent);
        }
        else if (targetType == ReportTargetType.Comment)
        {
            var  comment      = await _commentRepository.GetCommentByIdAsync(targetId, reporterId, isAdmin);
            bool commentFound = comment is not null;
            bool isOwnComment = comment?.CommenterUserId == reporterId;
            response = CheckTargetOwnership(commentFound, isOwnComment, ResponseMessages.CommentNotFound, ResponseMessages.CannotReportOwnContent);
        }
        else
        {
            bool isSelf     = targetId == reporterId;
            bool userExists = isSelf || await _userRepository.ExistsByIdAsync(targetId);
            response = CheckTargetOwnership(userExists, isSelf, ResponseMessages.UserNotFound, ResponseMessages.CannotReportOneself);
        }

        return response;
    }

    private ApplicationResponse CheckTargetOwnership(bool targetFound, bool belongsToReporter, string notFoundMessage, string ownTargetMessage)
    {
        var response = new ApplicationResponse();

        bool isReportable = targetFound && !belongsToReporter;

        if (isReportable)
        {
            response.Ok();
        }
        else if (targetFound)
        {
            response.Fail(ownTargetMessage);
        }
        else
        {
            response.Fail(notFoundMessage);
        }

        return response;
    }
}
