using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace AtgDev.Voicemeeter.Utils
{
    /// <summary>
    /// Locates the VoiceMeeter Remote API DLL installed by VoiceMeeter.
    /// </summary>
    public static class PathHelper
    {
        private const string VmKey = "VB:Voicemeeter {17359A74-1236-5467}";
        private const string RegistryHead = @"HKEY_LOCAL_MACHINE\SOFTWARE\";
        private const string RegistryTail = @"Microsoft\Windows\CurrentVersion\Uninstall\" + VmKey;
        private const string RegistryWow6432Node = @"WOW6432Node\";
        private const string UninstallValueName = "UninstallString";
        private const string VmSubPath = @"VB\Voicemeeter";

        private static string GetDllName()
        {
            return Environment.Is64BitProcess ? "VoicemeeterRemote64.dll" : "VoicemeeterRemote.dll";
        }

        private static string GetProgramFolderFromEnvironment()
        {
            string[] variables = { "ProgramFiles(x86)", "ProgramFiles", "ProgramW6432" };
            foreach (string variable in variables)
            {
                string programFiles = Environment.GetEnvironmentVariable(variable);
                if (string.IsNullOrWhiteSpace(programFiles))
                    continue;

                string vmPath = Path.Combine(programFiles, VmSubPath);
                if (Directory.Exists(vmPath))
                    return vmPath;
            }

            return string.Empty;
        }

        public static string GetProgramFolder()
        {
            string path = GetProgramFolderFromEnvironment();
            if (!string.IsNullOrWhiteSpace(path))
                return path;

            string registryPath = RegistryHead + RegistryTail;
            object result = Registry.GetValue(registryPath, UninstallValueName, null);
            if (result is string uninstallString && !string.IsNullOrWhiteSpace(uninstallString))
                return Path.GetDirectoryName(uninstallString) ?? throw new DirectoryNotFoundException("Unable to determine the VoiceMeeter install folder.");

            registryPath = RegistryHead + RegistryWow6432Node + RegistryTail;
            result = Registry.GetValue(registryPath, UninstallValueName, null);
            if (result is string wowUninstallString && !string.IsNullOrWhiteSpace(wowUninstallString))
                return Path.GetDirectoryName(wowUninstallString) ?? throw new DirectoryNotFoundException("Unable to determine the VoiceMeeter install folder.");

            throw new DirectoryNotFoundException(
                $"Unable to locate VoiceMeeter. Registry path checked: {registryPath}");
        }

        public static string GetDllPath()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("VoiceMeeter is only supported on Windows.");

            return Path.Combine(GetProgramFolder(), GetDllName());
        }

        public static bool TryGetDllPath(ref string path)
        {
            try
            {
                path = GetDllPath();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryGetProgramFolder(ref string path)
        {
            try
            {
                path = GetProgramFolder();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
