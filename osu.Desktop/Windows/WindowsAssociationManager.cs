// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Game.Localisation;

namespace osu.Desktop.Windows
{
    [SupportedOSPlatform("windows")]
    public static class WindowsAssociationManager
    {
        public const string SOFTWARE_CLASSES = @"Software\Classes";

        /// <summary>
        /// Sub key for setting the icon.
        /// https://learn.microsoft.com/en-us/windows/win32/com/defaulticon
        /// </summary>
        public const string DEFAULT_ICON = @"DefaultIcon";

        /// <summary>
        /// Sub key for setting the command line that the shell invokes.
        /// https://learn.microsoft.com/en-us/windows/win32/com/shell
        /// </summary>
        public const string SHELL_OPEN_COMMAND = @"Shell\Open\Command";

        public static readonly string EXE_PATH = Path.ChangeExtension(typeof(WindowsAssociationManager).Assembly.Location, ".exe");

        /// <summary>
        /// Program ID prefix used for file associations. Should be relatively short since the full program ID has a 39 character limit,
        /// see https://learn.microsoft.com/en-us/windows/win32/com/-progid--key.
        /// </summary>
        public const string PROGRAM_ID_PREFIX = "osu";

        private static readonly FileAssociation[] file_associations =
        {
            new FileAssociation(@".osz", WindowsAssociationManagerStrings.OsuBeatmap, Icons.Lazer),
            new FileAssociation(@".olz", WindowsAssociationManagerStrings.OsuBeatmap, Icons.Lazer),
            new FileAssociation(@".osr", WindowsAssociationManagerStrings.OsuReplay, Icons.Lazer),
            new FileAssociation(@".osk", WindowsAssociationManagerStrings.OsuSkin, Icons.Lazer),
        };

        private static readonly UriAssociation[] uri_associations =
        {
            new UriAssociation(@"osu", WindowsAssociationManagerStrings.OsuProtocol, Icons.Lazer),
            new UriAssociation(@"osump", WindowsAssociationManagerStrings.OsuMultiplayer, Icons.Lazer),
        };

        public static void InstallAssociations(LocalisationManager? localisation)
        {
            try
            {
                using (var classes = Registry.CurrentUser.OpenSubKey(SOFTWARE_CLASSES, writable: true))
                {
                    if (classes == null)
                        return;

                    foreach (var association in file_associations)
                        association.Install(classes, EXE_PATH, PROGRAM_ID_PREFIX);

                    foreach (var association in uri_associations)
                        association.Install(classes, EXE_PATH);
                }

                updateDescriptions(localisation);
            }
            catch (Exception e)
            {
                Logger.Log(@$"Failed to install file and URI associations: {e.Message}");
            }
        }

        private static void updateDescriptions(LocalisationManager? localisation)
        {
            try
            {
                using var classes = Registry.CurrentUser.OpenSubKey(SOFTWARE_CLASSES, true);
                if (classes == null)
                    return;

                foreach (var association in file_associations)
                    association.UpdateDescription(classes, PROGRAM_ID_PREFIX, getLocalisedString(association.Description));

                foreach (var association in uri_associations)
                    association.UpdateDescription(classes, getLocalisedString(association.Description));

                NotifyShellUpdate();
            }
            catch (Exception e)
            {
                Logger.Log($@"Failed to update file and URI associations: {e.Message}");
            }

            string getLocalisedString(LocalisableString s)
            {
                if (localisation == null)
                    return s.ToString();

                var b = localisation.GetLocalisedBindableString(s);
                b.UnbindAll();
                return b.Value;
            }
        }

        public static void UninstallAssociations()
        {
            try
            {
                using var classes = Registry.CurrentUser.OpenSubKey(SOFTWARE_CLASSES, true);
                if (classes == null)
                    return;

                foreach (var association in file_associations)
                    association.Uninstall(classes, PROGRAM_ID_PREFIX);

                foreach (var association in uri_associations)
                    association.Uninstall(classes);

                NotifyShellUpdate();
            }
            catch (Exception e)
            {
                Logger.Log($@"Failed to uninstall file and URI associations: {e.Message}");
            }
        }

        internal static void NotifyShellUpdate() => SHChangeNotify(EventId.SHCNE_ASSOCCHANGED, Flags.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);

        #region Native interop

        [DllImport("Shell32.dll")]
        private static extern void SHChangeNotify(EventId wEventId, Flags uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private enum EventId
        {
            /// <summary>
            /// A file type association has changed. <see cref="Flags.SHCNF_IDLIST"/> must be specified in the uFlags parameter.
            /// dwItem1 and dwItem2 are not used and must be <see cref="IntPtr.Zero"/>. This event should also be sent for registered protocols.
            /// </summary>
            SHCNE_ASSOCCHANGED = 0x08000000
        }

        private enum Flags : uint
        {
            SHCNF_IDLIST = 0x0000
        }

        #endregion

        private record FileAssociation(string Extension, LocalisableString Description, string IconPath)
        {
            private string getProgramId(string prefix) => $@"{prefix}.File{Extension}";

            /// <summary>
            /// Installs a file extenstion association in accordance with https://learn.microsoft.com/en-us/windows/win32/com/-progid--key
            /// </summary>
            public void Install(RegistryKey classes, string exePath, string programIdPrefix)
            {
                string programId = getProgramId(programIdPrefix);

                // register a program id for the given extension
                using (var programKey = classes.CreateSubKey(programId))
                {
                    using (var defaultIconKey = programKey.CreateSubKey(DEFAULT_ICON))
                        defaultIconKey.SetValue(null, IconPath);

                    using (var openCommandKey = programKey.CreateSubKey(SHELL_OPEN_COMMAND))
                        openCommandKey.SetValue(null, $@"""{exePath}"" ""%1""");
                }

                using (var extensionKey = classes.CreateSubKey(Extension))
                {
                    // set ourselves as the default program
                    extensionKey.SetValue(null, programId);

                    // add to the open with dialog
                    // https://learn.microsoft.com/en-us/windows/win32/shell/how-to-include-an-application-on-the-open-with-dialog-box
                    using (var openWithKey = extensionKey.CreateSubKey(@"OpenWithProgIds"))
                        openWithKey.SetValue(programId, string.Empty);
                }
            }

            public void UpdateDescription(RegistryKey classes, string programIdPrefix, string description)
            {
                using (var programKey = classes.OpenSubKey(getProgramId(programIdPrefix), true))
                    programKey?.SetValue(null, description);
            }

            public void Uninstall(RegistryKey classes, string programIdPrefix)
            {
                string programId = getProgramId(programIdPrefix);

                // importantly, we don't delete the default program entry because some other program could have taken it.

                using (var extensionKey = classes.OpenSubKey($@"{Extension}\OpenWithProgIds", true))
                    extensionKey?.DeleteValue(programId, throwOnMissingValue: false);

                classes.DeleteSubKeyTree(programId, throwOnMissingSubKey: false);
            }
        }

        private record UriAssociation(string Protocol, LocalisableString Description, string IconPath)
        {
            /// <summary>
            /// "The <c>URL Protocol</c> string value indicates that this key declares a custom pluggable protocol handler."
            /// See https://learn.microsoft.com/en-us/previous-versions/windows/internet-explorer/ie-developer/platform-apis/aa767914(v=vs.85).
            /// </summary>
            public const string URL_PROTOCOL = @"URL Protocol";

            /// <summary>
            /// Registers an URI protocol handler in accordance with https://learn.microsoft.com/en-us/previous-versions/windows/internet-explorer/ie-developer/platform-apis/aa767914(v=vs.85).
            /// </summary>
            public void Install(RegistryKey classes, string exePath)
            {
                using (var protocolKey = classes.CreateSubKey(Protocol))
                {
                    protocolKey.SetValue(URL_PROTOCOL, string.Empty);

                    using (var defaultIconKey = protocolKey.CreateSubKey(DEFAULT_ICON))
                        defaultIconKey.SetValue(null, IconPath);

                    using (var openCommandKey = protocolKey.CreateSubKey(SHELL_OPEN_COMMAND))
                        openCommandKey.SetValue(null, $@"""{exePath}"" ""%1""");
                }
            }

            public void UpdateDescription(RegistryKey classes, string description)
            {
                using (var protocolKey = classes.OpenSubKey(Protocol, true))
                    protocolKey?.SetValue(null, $@"URL:{description}");
            }

            public void Uninstall(RegistryKey classes)
            {
                classes.DeleteSubKeyTree(Protocol, throwOnMissingSubKey: false);
            }
        }
    }
}
