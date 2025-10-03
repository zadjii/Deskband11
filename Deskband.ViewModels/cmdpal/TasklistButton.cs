// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace Microsoft.CmdPal.Common.Helpers;

/// <summary>
/// Represents a button on the Windows taskbar.
/// </summary>
public sealed record TasklistButton
{
    /// <summary>
    /// Gets the name/automation ID of the taskbar button.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the X coordinate of the button.
    /// </summary>
    public int X { get; init; }

    /// <summary>
    /// Gets the Y coordinate of the button.
    /// </summary>
    public int Y { get; init; }

    /// <summary>
    /// Gets the width of the button.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Gets the height of the button.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// Gets the assigned key number for the button (1-10).
    /// </summary>
    public int KeyNum { get; init; }
}

/// <summary>
/// Provides functionality to interact with and retrieve information about Windows taskbar buttons.
/// </summary>
public sealed partial class Tasklist : IDisposable
{
    private IUIAutomation? _automation;
    private IUIAutomationElement? _element;
    private IUIAutomationCondition? _trueCondition;
    private bool _disposed;

    /// <summary>
    /// Updates the internal references to the Windows taskbar.
    /// </summary>
    public void Update()
    {
        ThrowIfDisposed();

        // Get HWND of the tasklist by walking the window hierarchy
        HWND tasklistHwnd = PInvoke.FindWindow("Shell_TrayWnd", null);
        if (tasklistHwnd.IsNull)
        {
            return;
        }

        tasklistHwnd = PInvoke.FindWindowEx(tasklistHwnd, HWND.Null, "ReBarWindow32", null);
        if (tasklistHwnd.IsNull)
        {
            return;
        }

        tasklistHwnd = PInvoke.FindWindowEx(tasklistHwnd, HWND.Null, "MSTaskSwWClass", null);
        if (tasklistHwnd.IsNull)
        {
            return;
        }

        tasklistHwnd = PInvoke.FindWindowEx(tasklistHwnd, HWND.Null, "MSTaskListWClass", null);
        if (tasklistHwnd.IsNull)
        {
            return;
        }

        // Initialize UI Automation if not already done
        if (_automation == null)
        {
            _automation = (IUIAutomation)new CUIAutomation();
            _trueCondition = _automation.CreateTrueCondition();
        }

        // Get the automation element for the taskbar
        _element = _automation.ElementFromHandle(tasklistHwnd);
    }

    /// <summary>
    /// Updates the provided list with current taskbar buttons.
    /// </summary>
    /// <param name="buttons">The list to populate with taskbar buttons.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public bool UpdateButtons(List<TasklistButton> buttons)
    {
        ThrowIfDisposed();

        if (_automation == null || _element == null || _trueCondition == null)
        {
            return false;
        }

        try
        {
            IUIAutomationElementArray elements = _element.FindAll(TreeScope.TreeScope_Children, _trueCondition);
            if (elements == null)
            {
                return false;
            }

            int count = elements.Length;
            List<TasklistButton> foundButtons = new(count);

            for (int i = 0; i < count; i++)
            {
                IUIAutomationElement child = elements.GetElement(i);
                if (child == null)
                {
                    continue;
                }

                TasklistButton? button = CreateTasklistButton(child);
                if (button != null)
                {
                    foundButtons.Add(button);
                    // Debug.WriteLine($"  button: {button.Name}");
                }

                Marshal.ReleaseComObject(child);
            }

            Marshal.ReleaseComObject(elements);

            // Assign key numbers and filter buttons
            AssignKeyNumbers(foundButtons, buttons);
            return true;
        }
        catch (COMException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the current taskbar buttons.
    /// </summary>
    /// <returns>A list of taskbar buttons.</returns>
    public List<TasklistButton> GetButtons()
    {
        List<TasklistButton> buttons = new();
        UpdateButtons(buttons);
        return buttons;
    }

    /// <summary>
    /// Creates a TasklistButton from a UI automation element.
    /// </summary>
    /// <param name="element">The UI automation element.</param>
    /// <returns>A TasklistButton if successful, null otherwise.</returns>
    private static TasklistButton? CreateTasklistButton(IUIAutomationElement element)
    {
        try
        {
            // Get bounding rectangle
            object boundingRect = element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_BoundingRectanglePropertyId);
            double[]? rectArray = boundingRect as double[];
            if (rectArray is null)
            {
                return null;
            }
            //if (boundingRect == null || !boundingRect.IsArray)
            //    return null;

            //var rectArray = (double[])boundingRect;
            if (rectArray.Length < 4)
            {
                return null;
            }

            // Get automation ID (name)

            string automationId = element.CurrentAutomationId.ToString() ?? string.Empty;

            return new TasklistButton
            {
                Name = automationId,
                X = (int)rectArray[0],
                Y = (int)rectArray[1],
                Width = (int)rectArray[2],
                Height = (int)rectArray[3],
                KeyNum = 0 // Will be assigned later
            };
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Assigns key numbers to buttons and filters the result.
    /// </summary>
    /// <param name="foundButtons">The buttons found via automation.</param>
    /// <param name="buttons">The output list to populate.</param>
    private static void AssignKeyNumbers(List<TasklistButton> foundButtons, List<TasklistButton> buttons)
    {
        buttons.Clear();

        foreach (TasklistButton button in foundButtons)
        {
            if (buttons.Count == 0)
            {
                buttons.Add(button with { KeyNum = 1 });
            }
            else
            {
                TasklistButton lastButton = buttons[^1];

                // Skip buttons on second row (lower Y coordinate or significantly left of previous)
                if (button.X < lastButton.X || button.Y < lastButton.Y)
                {
                    break;
                }

                // Skip buttons from the same app (same name)
                if (button.Name == lastButton.Name)
                {
                    continue;
                }

                int nextKeyNum = lastButton.KeyNum + 1;
                buttons.Add(button with { KeyNum = nextKeyNum });

                // Limit to 10 buttons
                if (nextKeyNum == 10)
                {
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Throws an ObjectDisposedException if the object has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(Tasklist));
        }
    }

    /// <summary>
    /// Releases the COM objects used by this instance.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            if (_trueCondition != null)
            {
                Marshal.ReleaseComObject(_trueCondition);
                _trueCondition = null;
            }

            if (_element != null)
            {
                Marshal.ReleaseComObject(_element);
                _element = null;
            }

            if (_automation != null)
            {
                Marshal.ReleaseComObject(_automation);
                _automation = null;
            }

            _disposed = true;
        }
    }
}

/// <summary>
/// COM class for UI Automation.
/// </summary>
[ComImport]
[Guid("ff48dba4-60ef-4201-aa87-54103eef594e")]
internal class CUIAutomation
{
}