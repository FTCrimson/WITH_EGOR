using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Content.Back.Authorization
{
    internal static class NativeAuth
    {
        private const string DllName = "uaslib_unity"; // Expected native wrapper DLL name

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void AuthResponseCallback([MarshalAs(UnmanagedType.LPStr)] string token,
            [MarshalAs(UnmanagedType.LPStr)] string playerId, int expiresInSeconds);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "UAS_SetProjectId")]
        public static extern void SetProjectId([MarshalAs(UnmanagedType.LPStr)] string projectId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "UAS_SetEnvironment")]
        public static extern void SetEnvironment([MarshalAs(UnmanagedType.LPStr)] string environmentName);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "UAS_SignInAnonymously")]
        public static extern void SignInAnonymously(AuthResponseCallback callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "UAS_RefreshToken")]
        public static extern void RefreshToken(AuthResponseCallback callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "UAS_BeginAutoRefresh")]
        public static extern void BeginAutoRefresh(AuthResponseCallback callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "UAS_EndAutoRefresh")]
        public static extern void EndAutoRefresh();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "UAS_SessionTokenExists")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool SessionTokenExists();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "UAS_ClearSessionToken")]
        public static extern void ClearSessionToken();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "UAS_GetToken")]
        public static extern IntPtr GetToken();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "UAS_GetPlayerId")]
        public static extern IntPtr GetPlayerId();
#else
        // Stubs for non-Windows platforms
        public static void SetProjectId(string projectId) { Debug.LogWarning("NativeAuth not available on this platform."); }
        public static void SetEnvironment(string environmentName) { }
        public static void SignInAnonymously(AuthResponseCallback callback) { }
        public static void RefreshToken(AuthResponseCallback callback) { }
        public static void BeginAutoRefresh(AuthResponseCallback callback) { }
        public static void EndAutoRefresh() { }
        public static bool SessionTokenExists() => false;
        public static void ClearSessionToken() { }
        public static IntPtr GetToken() => IntPtr.Zero;
        public static IntPtr GetPlayerId() => IntPtr.Zero;
#endif

        public static bool IsSupportedPlatform => Application.platform == RuntimePlatform.WindowsPlayer ||
                                                   Application.platform == RuntimePlatform.WindowsEditor;
    }
}
