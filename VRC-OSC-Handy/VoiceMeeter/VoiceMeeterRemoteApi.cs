using System;
using System.Runtime.InteropServices;

namespace AtgDev.Voicemeeter
{
    /// <summary>
    /// Minimal VoiceMeeter Remote API wrapper used by VRC-OSC-Handy.
    /// This replaces the legacy .NET Framework-only a-tg.VmrapiDynWrap /
    /// a-tg.UnmanagedLibWrap NuGet packages with a native-loader implementation
    /// that works directly on modern .NET.
    /// </summary>
    public sealed class RemoteApiWrapper : IDisposable
    {
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int VBVMR_Login();

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int VBVMR_Logout();

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int VBVMR_GetVoicemeeterType(out int type);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int VBVMR_IsParametersDirty();

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int VBVMR_GetParameterFloat(IntPtr paramNamePtr, out float value);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int VBVMR_SetParameterFloat(IntPtr paramNamePtr, float value);

        private IntPtr _dllHandle;
        private readonly VBVMR_Login _login;
        private readonly VBVMR_Logout _logout;
        private readonly VBVMR_GetVoicemeeterType _getVoicemeeterType;
        private readonly VBVMR_IsParametersDirty _isParametersDirty;
        private readonly VBVMR_GetParameterFloat _getParameterFloat;
        private readonly VBVMR_SetParameterFloat _setParameterFloat;

        public RemoteApiWrapper(string dllPath)
        {
            if (string.IsNullOrWhiteSpace(dllPath))
                throw new ArgumentException("A VoiceMeeter Remote API DLL path is required.", nameof(dllPath));

            _dllHandle = NativeLibrary.Load(dllPath);
            try
            {
                _login = GetDelegate<VBVMR_Login>("VBVMR_Login");
                _logout = GetDelegate<VBVMR_Logout>("VBVMR_Logout");
                _getVoicemeeterType = GetDelegate<VBVMR_GetVoicemeeterType>("VBVMR_GetVoicemeeterType");
                _isParametersDirty = GetDelegate<VBVMR_IsParametersDirty>("VBVMR_IsParametersDirty");
                _getParameterFloat = GetDelegate<VBVMR_GetParameterFloat>("VBVMR_GetParameterFloat");
                _setParameterFloat = GetDelegate<VBVMR_SetParameterFloat>("VBVMR_SetParameterFloat");
            }
            catch
            {
                ReleaseHandle();
                throw;
            }
        }

        public int Login() => _login();

        public int Logout() => _logout();

        public int GetVoicemeeterType(out int type) => _getVoicemeeterType(out type);

        public int IsParametersDirty() => _isParametersDirty();

        public int GetParameter(string paramName, out float value)
        {
            if (paramName == null)
                throw new ArgumentNullException(nameof(paramName));

            IntPtr paramNamePtr = Marshal.StringToHGlobalAnsi(paramName);
            try
            {
                return _getParameterFloat(paramNamePtr, out value);
            }
            finally
            {
                Marshal.FreeHGlobal(paramNamePtr);
            }
        }

        public int SetParameter(string paramName, float value)
        {
            if (paramName == null)
                throw new ArgumentNullException(nameof(paramName));

            IntPtr paramNamePtr = Marshal.StringToHGlobalAnsi(paramName);
            try
            {
                return _setParameterFloat(paramNamePtr, value);
            }
            finally
            {
                Marshal.FreeHGlobal(paramNamePtr);
            }
        }

        private T GetDelegate<T>(string exportName) where T : Delegate
        {
            IntPtr procedure = NativeLibrary.GetExport(_dllHandle, exportName);
            return Marshal.GetDelegateForFunctionPointer<T>(procedure);
        }

        private void ReleaseHandle()
        {
            if (_dllHandle == IntPtr.Zero)
                return;

            NativeLibrary.Free(_dllHandle);
            _dllHandle = IntPtr.Zero;
        }

        public void Dispose()
        {
            ReleaseHandle();
            GC.SuppressFinalize(this);
        }

        ~RemoteApiWrapper()
        {
            ReleaseHandle();
        }
    }
}
