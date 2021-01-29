using System;
using System.Runtime.InteropServices;

namespace D2dControl {
    internal static class NativeMethods {
        [DllImport( "user32.dll", SetLastError = false )]
        internal static extern IntPtr GetDesktopWindow();
    }
}