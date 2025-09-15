// ------------------------------------------------------------
//
// Copyright (c) Jiří Polášek. All rights reserved.
//
// ------------------------------------------------------------

using System.Runtime.InteropServices;
using Windows.ApplicationModel;
//using JPSoftworks.MediaControlsExtension.Interop;

namespace JPSoftworks.MediaControlsExtension.Helpers;

internal static class ModernAppHelper
{
    public static AppInfo? Get(string appUserModelId)
    {
        try
        {
            return AppInfo.GetFromAppUserModelId(appUserModelId);
        }
        // TODO! MIKE CHANGED THIS
        //catch (COMException ex) when ((uint)ex.ErrorCode == (uint)HRESULT.ERROR_NOT_FOUND)
        //{
        //    return null;
        //}
        catch (COMException)
        {
            return null;
        }
    }
}