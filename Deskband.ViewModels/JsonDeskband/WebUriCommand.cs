using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.System;

namespace Deskband.ViewModels.JsonDeskband;

/// <summary>
/// A command that opens a web URI when invoked
/// </summary>
internal partial class WebUriCommand : InvokableCommand
{
    private readonly string _uri;

    public WebUriCommand(string id, string name, string? iconPath, string uri)
    {
        Id = id;
        Name = name;
        _uri = uri;

        if (!string.IsNullOrEmpty(iconPath))
        {
            // Handle web URLs (like favicon.ico) vs local paths vs Unicode symbols
            if (iconPath.StartsWith("http://") || iconPath.StartsWith("https://"))
            {
                // For web icons, we'll use a generic web icon for now
                // In a full implementation, you might want to download and cache these
                Icon = new IconInfo("\uE774"); // Globe icon
            }
            else if (iconPath.StartsWith("\\u") && iconPath.Length == 6)
            {
                // Handle Unicode escape sequences like "\uE701"
                Icon = int.TryParse(iconPath.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out int unicodeValue)
                    ? new IconInfo(((char)unicodeValue).ToString())
                    : new IconInfo(iconPath);
            }
            else
            {
                // Local file path or direct Unicode character
                Icon = new IconInfo(iconPath);
            }
        }
        else
        {
            Icon = new IconInfo("\uE774"); // Default web icon
        }
    }

    public override ICommandResult Invoke()
    {
        try
        {
            // Launch the URI in the default browser
            _ = Launcher.LaunchUriAsync(new Uri(_uri));
        }
        catch (Exception)
        {
            // Silently handle launch failures
            // In a production app, you might want to log this or show a notification
        }

        return CommandResult.KeepOpen();
    }
}