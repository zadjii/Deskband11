# Deskband 11

A modern resurrection of the Windows <=10 Deskbands, written in WinUI 3 with WASDK. 

> [!WARN]
> 🏗️ under construction. Hackathon-tier code 🏗️

This works by creating a transparent always-on-top WinUI3 window, and parenting
it to the taskbar. Then we add a clip region to the window to match the
content's size, so it doesn't interfere with other taskbar interactions.

It's literally just a window on top of the taskbar.

I've given it a tray icon too, so you can close it. 

## `ITaskbarItem` extensibility

Inspired by the Command Palette APIs, there's a simple `TaskbarItemViewModel`
you can extend to create your own deskband. Stick those into the `Bands` object
in `BandsItemsControl`, and we'll display that set of deskbands.

Currently it shows three deskbands:
* A test with buttons
* A hello world label
* the Media Controls deskband, as a recreation of the old
  [AudioBand](https://github.com/AudioBand/AudioBand)

If we wouldn't just totally conflict with windows in the taskbar, I'd add that
straight to the cmdpal API. 

## Direct modification

If you want to put whatever into the taskbar, you can yank out the
`BandsItemsControl` and stick whatever you want into `MainContent` in
`MainWindow.xaml`. The `MainContent` control is what we'll use to clip, so make
sure all your content is inside that.

```xaml
<ContentControl x:Name="MainContent">
    <BandsItemsControl />
    <!-- Replace this ^ with your own content -->
</ContentControl>
```

## Building

You'll need the [Windows App
SDK](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/) installed,
and a recent Visual Studio 2022 with the WinUI workload. Then just build and
run. You should see a new window appear on the taskbar.
