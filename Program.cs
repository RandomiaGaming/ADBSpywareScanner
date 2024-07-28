using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace ABDSpywareScanner
{
    public static class Program
    {
        public static readonly string[] TrustedVendorIDS = new string[] {
            "5700313618786177705", //Google LLC
			"5200379633052405703", //Samsung Electronics Co., Ltd.
			"8677746436809616031", //OnePlus Ltd.
		};
        public static readonly string[] ADBDirectoryPaths = new string[] { "D:\\Utilities\\AndroidPlatformTools", "C:\\Users\\RandomiaGaming\\Desktop\\AndroidPlatformTools" };
        public static string ADBPath { get; private set; } = LocateADB();
        public static int Main()
        {
            Package[] packageDatabase = GetPackageDatabase();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Saving package database.");
            File.WriteAllText("PackageDatabase.json", JsonConvert.SerializeObject(packageDatabase));

            Console.WriteLine();

            packageDatabase = packageDatabase.OrderByDescending(x => (byte)x.TrustLevel).ToArray();

            foreach (Package package in packageDatabase)
            {
                if (package.TrustLevel is PackageTrustLevel.TrustedVendor)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(package);
                }
                else if (package.TrustLevel is PackageTrustLevel.CommunityTrusted)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(package);
                }
                else if (package.TrustLevel is PackageTrustLevel.CommunityMixed)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(package);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(package);
                }
            }

            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Press any key to exit...");
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (true)
            {
                Console.ReadKey();
                if (stopwatch.ElapsedTicks > 5000000)
                {
                    break;
                }
            }
            return 0;
        }
        public static Package[] GetPackageDatabase(bool restartDaemon = true)
        {
            if (restartDaemon)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Stopping daemon server.");
                RunADBCommand("kill-server");

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Starting daemon server.");
                RunADBCommand("start-server");
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Getting package information.");
            string packageDatabaseString = RunADBCommand("shell cmd package list packages -f -U -u -i");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Getting disabled packages.");
            string disabledDatabaseString = RunADBCommand("shell cmd package list packages -d -u");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Getting system packages.");
            string systemDatabaseString = RunADBCommand("shell cmd package list packages -s -u");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Getting uninstalled packages.");
            string installedDatabaseString = RunADBCommand("shell cmd package list packages");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Parsing package information.");
            List<Package> packageDatabase = new List<Package>();

            string[] packageDatabaseSplit = StringHelper.TrimLeadingWhitespace(StringHelper.TrimTrailingWhitespace(StringHelper.FixLineEndings(packageDatabaseString))).Split('\n');
            string[] disabledDatabaseSplit = StringHelper.TrimLeadingWhitespace(StringHelper.TrimTrailingWhitespace(StringHelper.FixLineEndings(disabledDatabaseString))).Replace("package:", "").Split('\n');
            string[] systemDatabaseSplit = StringHelper.TrimLeadingWhitespace(StringHelper.TrimTrailingWhitespace(StringHelper.FixLineEndings(systemDatabaseString))).Replace("package:", "").Split('\n');
            string[] installedDatabaseSplit = StringHelper.TrimLeadingWhitespace(StringHelper.TrimTrailingWhitespace(StringHelper.FixLineEndings(installedDatabaseString))).Replace("package:", "").Split('\n');

            for (int i = 0; i < packageDatabaseSplit.Length; i++)
            {
                string packageInfo = StringHelper.TrimLeadingWhitespace(packageDatabaseSplit[i]);

                if (packageInfo.StartsWith("package:"))
                {
                    packageInfo = packageInfo.Substring(8);
                }

                packageInfo = StringHelper.TrimTrailingWhitespace(packageInfo);
                ushort UID = ushort.Parse(StringHelper.SelectAfterFirst(packageInfo, "uid:"));
                packageInfo = packageInfo.Substring(0, StringHelper.LastIndexOf(packageInfo, "uid:"));

                packageInfo = StringHelper.TrimTrailingWhitespace(packageInfo);
                string Installer = StringHelper.SelectAfterFirst(packageInfo, "installer=");
                if (Installer == "null")
                {
                    Installer = null;
                }
                packageInfo = packageInfo.Substring(0, StringHelper.LastIndexOf(packageInfo, "installer="));

                packageInfo = StringHelper.TrimTrailingWhitespace(packageInfo);
                int equalsIndex = StringHelper.LastIndexOf(packageInfo, "=");
                string FilePath = packageInfo.Substring(0, equalsIndex);

                Package package = new Package(packageInfo.Substring(equalsIndex + 1));
                package.UID = UID;
                package.Installer = Installer;
                package.FilePath = FilePath;

                package.Disabled = false;
                foreach (string disabledPackage in disabledDatabaseSplit)
                {
                    if (package.Name == disabledPackage)
                    {
                        package.Disabled = true;
                        break;
                    }
                }

                package.System = false;
                foreach (string systemPackage in systemDatabaseSplit)
                {
                    if (package.Name == systemPackage)
                    {
                        package.System = true;
                        break;
                    }
                }

                package.Uninstalled = true;
                foreach (string installedPackage in installedDatabaseSplit)
                {
                    if (package.Name == installedPackage)
                    {
                        package.Uninstalled = false;
                        break;
                    }
                }

                if (package.Installer is "com.android.vending")
                {
                    package.PlaystoreApp = true;
                }

                packageDatabase.Add(package);
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Downloading information from play.google.com.");

            WebClient client = new WebClient();
            for (int i = 0; i < packageDatabase.Count; i++)
            {
                if (packageDatabase[i].PlaystoreApp)
                {
                    try
                    {
                        string storePage = client.DownloadString("https://play.google.com/store/apps/details?id=" + packageDatabase[i].Name);

                        string jsobjectpayload = "{key: 'ds:5'" + StringHelper.SelectBeforeFirst(StringHelper.SelectAfterLast(storePage, "'ds:5'"), "</script>");
                        jsobjectpayload = jsobjectpayload.Substring(0, jsobjectpayload.Length - 2);

                        packageDatabase[i].PlaystoreRemoved = false;
                        packageDatabase[i].PlaystoreStars = 4.0;
                        packageDatabase[i].PlaystoreDownloads = 10000000;
                        packageDatabase[i].PlaystoreAge = 2 * 365 * 24 * 60 * 60;
                        packageDatabase[i].PlaystoreVendor = "com.YourMom.Yeet";
                    }
                    catch (WebException ex)
                    {
                        if (!packageDatabase[i].System)
                        {

                        }
                        packageDatabase[i].PlaystoreRemoved = true;
                        packageDatabase[i].PlaystoreStars = 0.0;
                        packageDatabase[i].PlaystoreDownloads = 0;
                        packageDatabase[i].PlaystoreAge = 0;
                        packageDatabase[i].PlaystoreVendor = null;
                    }
                }
                else
                {
                    packageDatabase[i].PlaystoreRemoved = false;
                    packageDatabase[i].PlaystoreStars = 0.0;
                    packageDatabase[i].PlaystoreDownloads = 0;
                    packageDatabase[i].PlaystoreAge = 0;
                    packageDatabase[i].PlaystoreVendor = null;
                }
            }

            return packageDatabase.ToArray();
        }

        public static string RunADBCommand(string adbCommand)
        {
            Process process = new Process();
            process.StartInfo.FileName = ADBPath;
            process.StartInfo.Arguments = adbCommand;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }
        public static string LocateADB()
        {
            string ADBPath = "adb.exe";

            string workingDirectory = Directory.GetCurrentDirectory();
            ADBPath = Path.Combine(workingDirectory, "adb.exe");
            if (File.Exists(ADBPath))
            {
                return ADBPath;
            }

            string processPath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Process);
            foreach (string path in processPath.Split(';'))
            {
                ADBPath = Path.Combine(path, "adb.exe");
                if (File.Exists(ADBPath))
                {
                    return ADBPath;
                }
            }

            string userPath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);
            foreach (string path in userPath.Split(';'))
            {
                ADBPath = Path.Combine(path, "adb.exe");
                if (File.Exists(ADBPath))
                {
                    return ADBPath;
                }
            }

            string machinePath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);
            foreach (string path in machinePath.Split(';'))
            {
                ADBPath = Path.Combine(path, "adb.exe");
                if (File.Exists(ADBPath))
                {
                    return ADBPath;
                }
            }

            foreach (string userFolder in Directory.GetDirectories("C:\\Users"))
            {
                ADBPath = Path.Combine(userFolder, "AppData\\Local\\Android\\Sdk\\platform-tools\\adb.exe");
                if (File.Exists(ADBPath))
                {
                    return ADBPath;
                }

                ADBPath = Path.Combine(userFolder, "AppData\\LocalLow\\Android\\Sdk\\platform-tools\\adb.exe");
                if (File.Exists(ADBPath))
                {
                    return ADBPath;
                }

                ADBPath = Path.Combine(userFolder, "AppData\\Roaming\\Android\\Sdk\\platform-tools\\adb.exe");
                if (File.Exists(ADBPath))
                {
                    return ADBPath;
                }
            }

            foreach (string possiblePath in ADBDirectoryPaths)
            {
                if (File.Exists(Path.Combine(possiblePath, "adb.exe")))
                {
                    return Path.Combine(possiblePath, "adb.exe");
                }
            }

            return "D:\\Utilities\\Android Platform Tools\\adb.exe";
        }
        public enum PackageTrustLevel : byte
        {
            TrustedVendor = 3, //Given to apps with the system tag, to apps in the system folder, or to Google Play Store apps from verified trusted vendors. - Blue
            CommunityTrusted = 2, //Given to Google Play Store apps with at least 10 million downloads, at least 2 years of age, and at least 4.0 stars. - Green
            CommunityMixed = 1, //Given to Google Play Store apps with less than 10 million downloads, less than 2 years of age, or less than 4.0 stars. - Yellow
            Untrusted = 0, //Given to side loaded apps, apps which have been deleted from the Google Play Store, or apps who's trust level cannot be verified.
        }
        public sealed class Package
        {
            //App Info
            public string Name = "com.UnknownVendor.UnknownPackage";
            public string FilePath = null;
            public string Installer = null;
            public ushort UID = 0;
            public bool System = false;
            public bool Disabled = false;
            public bool Uninstalled = false;
            //Playstore Info
            public bool PlaystoreApp = false;
            public bool PlaystoreRemoved = false;
            public double PlaystoreStars = 0.0;
            public ulong PlaystoreDownloads = 0;
            public ulong PlaystoreAge = 0;
            public string PlaystoreVendor = null;
            //Trust Level
            public PackageTrustLevel TrustLevel => GetTrustLevel(this);
            public override string ToString()
            {
                return Name;
            }
            public static bool operator ==(Package a, Package b)
            {
                return a.Name == b.Name;
            }
            public static bool operator !=(Package a, Package b)
            {
                return a.Name != b.Name;
            }
            public Package(string name)
            {
                Name = name;
            }
        }
        public static PackageTrustLevel GetTrustLevel(Package package)
        {
            //TrustedVendor
            if (package.System || (!(package.FilePath is null) && (package.FilePath.StartsWith("/system") || package.FilePath.StartsWith("system"))))
            {
                return PackageTrustLevel.TrustedVendor;
            }
            if (package.PlaystoreApp && !package.PlaystoreRemoved)
            {
                foreach (string trustedVendor in TrustedVendorIDS)
                {
                    if (package.PlaystoreVendor == trustedVendor)
                    {
                        return PackageTrustLevel.TrustedVendor;
                    }
                }
            }
            //CommunityTrusted
            if (package.PlaystoreApp && !package.PlaystoreRemoved)
            {
                if (package.PlaystoreDownloads >= 10000000 && package.PlaystoreAge >= 63072000 && package.PlaystoreStars >= 4.0)
                {
                    return PackageTrustLevel.CommunityTrusted;
                }
            }
            //CommunityMixed
            if (package.PlaystoreApp && !package.PlaystoreRemoved)
            {
                return PackageTrustLevel.CommunityMixed;
            }
            //Untrusted
            return PackageTrustLevel.Untrusted;
        }
    }
}
