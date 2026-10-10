using System.Runtime.InteropServices;
using System.IO;

namespace SnapMark.Export;

public static class RecycleBin
{
    // Windows 8+ explicitly requests recycling rather than permanent deletion.
    // https://learn.microsoft.com/windows/win32/api/shobjidl_core/nf-shobjidl_core-ifileoperation-setoperationflags
    public static Task SendAsync(IReadOnlyList<string> paths)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            IFileOperation? operation = null; var items = new List<IShellItem>();
            try
            {
                operation = (IFileOperation)Activator.CreateInstance(Type.GetTypeFromCLSID(new("3ad05575-8857-4850-9277-11b85bdb8e09"), true)!)!;
                operation.SetOperationFlags(0x00080000 | 0x00100000 | 0x0040 | 0x0010 | 0x0004 | 0x0400);
                foreach (var path in paths)
                {
                    var iid = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe");
                    Marshal.ThrowExceptionForHR(SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out var item));
                    items.Add(item); operation.DeleteItem(item, IntPtr.Zero);
                }
                operation.PerformOperations(); operation.GetAnyOperationsAborted(out var aborted);
                if (aborted) throw new IOException("Windows could not send every selected history file to Recycle Bin.");
                completion.SetResult();
            }
            catch (Exception ex) { completion.SetException(ex); }
            finally { foreach (var item in items) Marshal.FinalReleaseComObject(item); if (operation is not null) Marshal.FinalReleaseComObject(operation); }
        }) { IsBackground = true, Name = "SnapMark Recycle Bin" };
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); return completion.Task;
    }
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    private static extern int SHCreateItemFromParsingName(string path, IntPtr bindContext, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out IShellItem item);
    [ComImport, Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem { }
    [ComImport, Guid("947aab5f-0a5c-4c13-b4d6-4bf7836fc9f8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileOperation
    {
        void Advise(IntPtr sink, out uint cookie); void Unadvise(uint cookie);
        void SetOperationFlags(uint flags); void SetProgressMessage([MarshalAs(UnmanagedType.LPWStr)] string message);
        void SetProgressDialog(IntPtr dialog); void SetProperties(IntPtr properties); void SetOwnerWindow(uint owner);
        void ApplyPropertiesToItem(IntPtr item); void ApplyPropertiesToItems(IntPtr items);
        void RenameItem(IntPtr item, [MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr sink); void RenameItems(IntPtr items, [MarshalAs(UnmanagedType.LPWStr)] string name);
        void MoveItem(IntPtr item, IntPtr folder, [MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr sink); void MoveItems(IntPtr items, IntPtr folder);
        void CopyItem(IntPtr item, IntPtr folder, [MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr sink); void CopyItems(IntPtr items, IntPtr folder);
        void DeleteItem(IShellItem item, IntPtr sink); void DeleteItems(IntPtr items);
        void NewItem(IntPtr folder, uint attributes, [MarshalAs(UnmanagedType.LPWStr)] string name, [MarshalAs(UnmanagedType.LPWStr)] string template, IntPtr sink);
        void PerformOperations(); void GetAnyOperationsAborted([MarshalAs(UnmanagedType.Bool)] out bool aborted);
    }
}
