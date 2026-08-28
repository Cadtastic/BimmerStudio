using BimmerStudio.Application.Localization;

namespace BimmerStudio.App.Localization;

/// <summary>
/// Static access point to the one localizer, for XAML.
/// </summary>
/// <remarks>
/// Markup extensions have no route to the DI container, so the composition root parks the same
/// instance it registered here. This is the only static service access in the application.
/// </remarks>
public static class LocalizerHost
{
    private static ILocalizer? _current;

    public static ILocalizer Current =>
        _current ?? throw new InvalidOperationException("The localizer has not been initialised.");

    public static void Attach(ILocalizer localizer) => _current = localizer;
}
