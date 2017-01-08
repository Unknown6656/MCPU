using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Forms;
using System.Diagnostics;
using System.Windows;
using System;

using IWin32Window = System.Windows.Forms.IWin32Window;

namespace MCPU.IDE
{
    /// <summary>
    /// Exposes the TaskDialog-control defined in the library "comctl32.dll"
    /// </summary>
    public static class TaskDialog
    {
        [DllImport("comctl32.dll", CharSet = CharSet.Unicode, EntryPoint = "TaskDialog")]
        internal static extern int __td(IntPtr hWndParent, IntPtr hInstance, string pszWindowTitle, string pszMainInstruction, string pszContent, int dwCommonButtons, IntPtr pszIcon, out TaskDialogResult pnButton);


        public static TaskDialogResult Show(IntPtr owner, string instruction, string title, string message) =>
            Show(new IWIN32WINDOWCONVERTER(owner) as IWin32Window, instruction, title, message);

        public static TaskDialogResult Show(IntPtr owner, string instruction, string title, string message, TaskDialogButtons buttons) =>
            Show(new IWIN32WINDOWCONVERTER(owner) as IWin32Window, instruction, title, message, buttons);

        public static TaskDialogResult Show(IntPtr owner, string instruction, string title, string message, TaskDialogButtons buttons, TaskDialogIcon icon) =>
            Show(new IWIN32WINDOWCONVERTER(owner) as IWin32Window, instruction, title, message, buttons, icon);

        public static TaskDialogResult Show(IWin32Window owner, string instruction, string title, string message) =>
            Show(owner, instruction, title, message, TaskDialogButtons.Ok);

        public static TaskDialogResult Show(IWin32Window owner, string instruction, string title, string message, TaskDialogButtons buttons) =>
            Show(owner, instruction, title, message, buttons, TaskDialogIcon.Information);

        public static TaskDialogResult Show(IWin32Window owner, string instruction, string title, string message, TaskDialogButtons buttons, TaskDialogIcon icon)
        {
            TaskDialogResult buttonClicked = TaskDialogResult.Cancel;

            __td(owner.Handle, IntPtr.Zero, title, instruction, message, (int)buttons, (IntPtr)icon, out buttonClicked);

            return buttonClicked;
        }
    }

    /// <summary>
    /// Represents an interface and converter between an unmanaged window handle and the COM-IWin32Window interface
    /// </summary>
	[DebuggerStepThrough, DebuggerNonUserCode, Serializable, ComVisible(true)]
    public class IWIN32WINDOWCONVERTER
        : IWin32Window
    {
        /// <summary>
        /// The window handle
        /// </summary>
        public IntPtr Handle { get; private set; }

        /// <summary>
        /// Creates a new instance from the given window handle
        /// </summary>
        /// <param name="hwnd">Window handle</param>
        public IWIN32WINDOWCONVERTER(IntPtr hwnd) => Handle = hwnd;

        /// <summary>
        /// Creates a new instance from the given WinForms instance
        /// </summary>
        /// <param name="frm">WinForms instance</param>
        public IWIN32WINDOWCONVERTER(Form frm)
            : this(frm?.Handle ?? IntPtr.Zero)
        {
        }

        /// <summary>
        /// Creates a new instance from the given WPF Window instance
        /// </summary>
        /// <param name="wnd">WPF Window instance</param>
        public IWIN32WINDOWCONVERTER(Window wnd)
            : this(wnd == null ? IntPtr.Zero : new WindowInteropHelper(wnd).Handle)
        {
        }
        
        public static implicit operator IWIN32WINDOWCONVERTER(Form f) => new IWIN32WINDOWCONVERTER(f);
        public static implicit operator IWIN32WINDOWCONVERTER(Window w) => new IWIN32WINDOWCONVERTER(w);
        public static implicit operator IWIN32WINDOWCONVERTER(IntPtr p) => new IWIN32WINDOWCONVERTER(p);
        public static implicit operator IntPtr(IWIN32WINDOWCONVERTER iwc) => iwc.Handle;
    }

    [Serializable, Flags]
    public enum TaskDialogButtons
        : int
    {
        Ok = 0x0001,
        Cancel = 0x0008,
        Yes = 0x0002,
        No = 0x0004,
        Retry = 0x0010,
        Close = 0x0020
    }

    [Serializable]
    public enum TaskDialogResult
        : int
    {
        Ok = 1,
        Cancel = 2,
        Retry = 4,
        Yes = 6,
        No = 7,
        Close = 8,
        None = 0
    }

    [Serializable, Flags]
    public enum TaskDialogIcon
    {
        Information = ushort.MaxValue - 2,
        Warning = ushort.MaxValue,
        Stop = ushort.MaxValue - 1,
        Question = 0,
        SecurityWarning = ushort.MaxValue - 5,
        SecurityError = ushort.MaxValue - 6,
        SecuritySuccess = ushort.MaxValue - 7,
        SecurityShield = ushort.MaxValue - 3,
        SecurityShieldBlue = ushort.MaxValue - 4,
        SecurityShieldGray = ushort.MaxValue - 8
    }
}
