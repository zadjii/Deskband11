// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CmdPal.Ext.WindowWalker.Components;
//using Microsoft.CmdPal.Ext.WindowWalker.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.WindowWalker.Commands;

internal sealed partial class SwitchToWindowCommand : InvokableCommand
{
    private readonly Window? _window;
    //public override string Id => $"window.{_window.Hwnd}";
    public SwitchToWindowCommand(Window? window)
    {
        //Name = Resources.switch_to_command_title;
        _window = window;
        //if (_window is not null)
        //{
        //    try
        //    {
        //        Process? p = Process.GetProcessById((int)_window.Process.ProcessID);
        //        if (p is not null)
        //        {
        //            string? processFileName = p.MainModule?.FileName;
        //            Icon = new IconInfo(processFileName);
        //        }
        //    }
        //    catch
        //    {
        //    }
        //}
    }

    public override ICommandResult Invoke()
    {
        if (_window is null)
        {
            ExtensionHost.LogMessage(new LogMessage() { Message = "Cannot switch to the window, because it doesn't exist." });
            return CommandResult.Dismiss();
        }

        _window.SwitchToWindow();

        return CommandResult.Dismiss();
    }

    public override bool Equals(object? obj)
    {
        return obj is SwitchToWindowCommand other ? other._window.Hwnd == this._window.Hwnd : base.Equals(obj);
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}
