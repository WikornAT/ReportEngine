namespace Templates.Domain.Enums;

/// <summary>Lifecycle status of a <see cref="ReportTemplates.ReportTemplate"/>.</summary>
public enum TemplateStatus
{
    /// <summary>Template is being authored and is not yet available for rendering.</summary>
    Draft = 0,

    /// <summary>Template is published and available for rendering.</summary>
    Active = 1,

    /// <summary>Template has been retired and is no longer used for new renders.</summary>
    Archived = 2,
}
