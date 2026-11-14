namespace aspnetproject.Common.ProjectConstants.Enums;

// Stored as text. Hidden content is readable only by its author and by administrators; flagged content reads as visible.
// Numbering starts at 1 so that Visible is not the CLR default, which EF Core treats as "unset" when a column has a database default.
public enum ModerationStatus
{
    Visible = 1,
    Flagged,
    Hidden
}
