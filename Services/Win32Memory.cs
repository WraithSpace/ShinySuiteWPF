using System.Runtime.InteropServices;

namespace ShinySuite.Services;

internal static class Win32Memory
{
    internal const uint PROCESS_VM_READ           = 0x0010;
    internal const uint PROCESS_QUERY_INFORMATION = 0x0400;

    internal const uint MEM_COMMIT    = 0x1000;
    internal const uint MEM_PRIVATE   = 0x20000;  // heap/stack — not image or mapped file
    internal const uint PAGE_READONLY       = 0x02;
    internal const uint PAGE_READWRITE      = 0x04;
    internal const uint PAGE_WRITECOPY      = 0x08;
    internal const uint PAGE_EXECUTE_READ   = 0x20;
    internal const uint PAGE_EXECUTE_READWRITE = 0x40;

    // 64-bit layout (48 bytes).  PartitionId (Windows 10+) sits in the
    // padding after AllocationProtect — we don't need it, so we skip it.
    [StructLayout(LayoutKind.Explicit, Size = 48)]
    internal struct MEMORY_BASIC_INFORMATION
    {
        [FieldOffset( 0)] public nint  BaseAddress;
        [FieldOffset( 8)] public nint  AllocationBase;
        [FieldOffset(16)] public uint  AllocationProtect;
        // bytes 20-23: PartitionId (u16) + 2 bytes pad  — intentionally skipped
        [FieldOffset(24)] public nint  RegionSize;
        [FieldOffset(32)] public uint  State;
        [FieldOffset(36)] public uint  Protect;
        [FieldOffset(40)] public uint  Type;
        // bytes 44-47: trailing alignment pad
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern nint OpenProcess(uint dwDesiredAccess,
        bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool CloseHandle(nint hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool ReadProcessMemory(nint hProcess,
        nint lpBaseAddress, byte[] lpBuffer, nint nSize,
        out nint lpNumberOfBytesRead);

    [DllImport("kernel32.dll")]
    internal static extern nint VirtualQueryEx(nint hProcess,
        nint lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer,
        uint dwLength);
}
