namespace Reporting.Domain.RenderLogs;

/// <summary>Terminal status of an inline render attempt.</summary>
public enum RenderLogStatus
{
    /// <summary>Render is currently in progress.</summary>
    Running = 0,

    /// <summary>Render completed successfully.</summary>
    Completed = 1,

    /// <summary>Render failed with an error.</summary>
    Failed = 2,
}
