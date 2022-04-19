using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Foundation;

namespace VS2019.GlowWindow.Controls
{
    public class GlowWindowEx : GlowWindow
    {
        #region CustomWindowChrome

        private static readonly PropertyInfo? CriticalHandlePropertyInfo = typeof(Window).GetProperty("CriticalHandle", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly object[] EmptyObjectArray = Array.Empty<object>();

        private const SET_WINDOW_POS_FLAGS SwpFlags = SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOOWNERZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (PresentationSource.FromDependencyObject(this) is not HwndSource hWnd) return;
            hWnd.AddHook(WndProc);
            if (hWnd.CompositionTarget != null)
                hWnd.CompositionTarget.BackgroundColor = System.Windows.Media.Colors.Transparent;
            MARGINS dwmMargin = new();
            PInvoke.DwmExtendFrameIntoClientArea((HWND)hWnd.Handle, in dwmMargin);
            PInvoke.SetWindowPos((HWND)hWnd.Handle, (HWND)IntPtr.Zero, 0, 0, 0, 0, SwpFlags);
        }

        protected virtual IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case (int)PInvoke.WM_NCCALCSIZE:
                    if (wParam != IntPtr.Zero)
                    {
                        handled = true;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        protected void Header_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.ClickCount)
            {
                case 1:
                    {
                        e.Handled = true;

                        VerifyAccess();
                        _ = PInvoke.ReleaseCapture();

                        if (CriticalHandlePropertyInfo != null)
                        {
                            var criticalHandle = CriticalHandlePropertyInfo.GetValue(this, EmptyObjectArray);
                            var wpfPoint = PointToScreen(Mouse.GetPosition(this));
                            var x = (int)wpfPoint.X;
                            var y = (int)wpfPoint.Y;
                            if (criticalHandle != null)
                            {
                                _ = PInvoke.SendMessage((HWND)(IntPtr)criticalHandle, PInvoke.WM_NCLBUTTONDOWN, (UIntPtr)PInvoke.HTCAPTION, new IntPtr(x | (y << 16)));
                            }
                        }

                        break;
                    }
                case 2 when ResizeMode != ResizeMode.NoResize:
                    {
                        e.Handled = true;

                        if (WindowState == WindowState.Normal && ResizeMode != ResizeMode.NoResize && ResizeMode != ResizeMode.CanMinimize)
                        {
                            SystemCommands.MaximizeWindow(this);
                        }
                        else
                        {
                            SystemCommands.RestoreWindow(this);
                        }

                        break;
                    }
            }
        }

        protected void Header_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var point = PointToScreen(e.GetPosition(this));
            SystemCommands.ShowSystemMenu(this, point);
        }

        protected void Icon_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.ClickCount)
            {
                case 1:
                    {
                        if (sender is FrameworkElement icon)
                        {
                            var point = icon.TransformToAncestor(this).Transform(new Point(0, icon.ActualHeight));
                            SystemCommands.ShowSystemMenu(this, point);
                        }

                        break;
                    }
                case 2:
                    Close();
                    break;
            }
        }

        #endregion

        #region SystemCommands

        protected override void OnInitialized(EventArgs e)
        {
            _ = CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, OnCloseWindow));
            _ = CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, OnMaximizeWindow, OnCanResizeWindow));
            _ = CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, OnMinimizeWindow, OnCanMinimizeWindow));
            _ = CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, OnRestoreWindow, OnCanResizeWindow));

            base.OnInitialized(e);
        }

        private void OnCanMinimizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ResizeMode != ResizeMode.NoResize;
        }

        private void OnCanResizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip;
        }

        private void OnCloseWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void OnMaximizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(this);
        }

        private void OnMinimizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void OnRestoreWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(this);
        }

        #endregion
    }
}
