using BimmerStudio.Domain.Diagnostics;

namespace BimmerStudio.App.ViewModels;

/// <summary>
/// One name/value pair from a result set.
/// </summary>
public sealed class ResultRowViewModel(ResultValue value)
{
    public string Name => value.Name;

    public string Value => value.ToDisplayString();

    public string Kind => value.Kind.ToString();

    /// <summary>
    /// Binary results are shown as hex, so they get a monospaced face to keep byte pairs aligned.
    /// </summary>
    public bool IsMonospaced => value.Kind is ResultValueKind.Binary;
}
