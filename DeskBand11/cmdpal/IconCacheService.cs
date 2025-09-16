// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CmdPal.Core.Control.Converters;
using Microsoft.CommandPalette.Extensions.Toolkit;
//using Microsoft.CmdPal.Core.ViewModels;
//using Microsoft.Terminal.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using Windows.Storage.Streams;

namespace Microsoft.CmdPal.UI.Helpers;

public sealed class IconCacheService(DispatcherQueue dispatcherQueue)
{
    public Task<IconSource?> GetIconSource(IconData icon) =>

        // todo: actually implement a cache of some sort
        IconToSource(icon);

    private async Task<IconSource?> IconToSource(IconData icon)
    {
        try
        {
            if (!string.IsNullOrEmpty(icon.Icon))
            {
                IconSource? source = IconPathConverter.IconSource(icon.Icon, false, null/*icon.FontFamily*/);
                return source;
            }
            else if (icon.Data is not null)
            {
                try
                {
                    return await StreamToIconSource(icon.Data);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Failed to load icon from stream: " + ex);
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private async Task<IconSource?> StreamToIconSource(IRandomAccessStreamReference iconStreamRef)
    {
        if (iconStreamRef is null)
        {
            return null;
        }

        BitmapImage bitmap = await IconStreamToBitmapImageAsync(iconStreamRef);
        ImageIconSource icon = new() { ImageSource = bitmap };
        return icon;
    }

    private async Task<BitmapImage> IconStreamToBitmapImageAsync(IRandomAccessStreamReference iconStreamRef)
    {
        // Return the bitmap image via TaskCompletionSource. Using WCT's EnqueueAsync does not suffice here, since if
        // we're already on the thread of the DispatcherQueue then it just directly calls the function, with no async involved.
        return await TryEnqueueAsync(dispatcherQueue, async () =>
        {
            using IRandomAccessStreamWithContentType bitmapStream = await iconStreamRef.OpenReadAsync();
            BitmapImage itemImage = new();
            await itemImage.SetSourceAsync(bitmapStream);
            return itemImage;
        });
    }

    private static Task<T> TryEnqueueAsync<T>(DispatcherQueue dispatcher, Func<Task<T>> function)
    {
        TaskCompletionSource<T> completionSource = new();

        bool enqueued = dispatcher.TryEnqueue(DispatcherQueuePriority.Normal, async void () =>
        {
            try
            {
                T? result = await function();
                completionSource.SetResult(result);
            }
            catch (Exception ex)
            {
                completionSource.SetException(ex);
            }
        });

        if (!enqueued)
        {
            completionSource.SetException(new InvalidOperationException("Failed to enqueue the operation on the UI dispatcher"));
        }

        return completionSource.Task;
    }
}
