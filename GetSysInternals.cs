// Copyright (c) 2021 by Larry Smith

// For admin privleges, the following might be helpful. Or not...
//	https://stackoverflow.com/questions/42159066/dotnet-core-app-run-as-administrator


using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using Microsoft.Win32;

#pragma warning disable CA1416 // Validate platform compatibility

namespace GetSysinternals {
	class GetSysInternals {
		static int Main(string[] args) {
			string TargetDir;
			try {
				TargetDir = GetTargetDir(args);
			} catch (Exception ex) {
				Console.WriteLine(ex.Message);
				return 1;
			}
			GetSysinternals();
			var zf = ZipFile.Open(GetSysinternals(), ZipArchiveMode.Read);
			foreach (var item in zf.Entries) {
				ProcessEntry(TargetDir, item);
			}
			return 0;
		}

//---------------------------------------------------------------------------------------

		private static string GetTargetDir(string[] args) { 
			if (args.Length != 1) {
				throw new Exception(@"Usage: GetSysintnals <DirectoryName>
	   where <DirectoryName> is the parent directory of where you have a ""Sysinternals""" +
	   " directory. If <DirectoryName>\\Sysinternals doesn't exist, it will be created.");
			}
			Directory.CreateDirectory(args[0]);		// NOP if already exists
			return args[0];
		}

//---------------------------------------------------------------------------------------

		/// <summary>
		/// Processes one entry from the downloaded .zip file. If it doesn't exist in the
		/// target directory, just copy it in. If it does exist, check the Last Written
		/// time stamp. If this entry is newer, replace the file on disk. Else ignore it.
		/// </summary>
		/// <param name="targetDir">The directory where the Sysinternals files are stored</param>
		/// <param name="entry">The entry in the .zip file</param>
		private static void ProcessEntry(string targetDir, ZipArchiveEntry entry) {
			string target = Path.Combine(targetDir, entry.Name);
			var ItemInfo = FileVersionInfo.GetVersionInfo(target);
			if (! File.Exists(target)) {
				Console.WriteLine($"Creating {entry.Name} version {ItemInfo.ProductVersion}");
				entry.ExtractToFile(target);
			} else {
				var LastSaved = entry.LastWriteTime;
				var fi = new FileInfo(target);
				if (LastSaved > fi.LastWriteTime) {
					entry.ExtractToFile(target, true);
					var fiNew = new FileInfo(target);
					var NewItemInfo = FileVersionInfo.GetVersionInfo(target);
					Console.WriteLine($"Updating {entry.Name} from version {ItemInfo.ProductVersion} to {NewItemInfo.ProductVersion}");
				} 
			}
		}

		//---------------------------------------------------------------------------------------

		/// <summary>
		/// Downloads the SysinternalsSuite.zip file from Microsoft into the Downloads
		/// directory, overwriting the current file if necessary.
		/// </summary>
		/// <returns>The name of the downloaded file on disk</returns>
		private static string  GetSysinternals() {
			string filename = Path.Combine(GetDownloadDirectoryName(), "SysinternalsSuite.zip");
#if DEBUG
			if (File.Exists(filename)) { return filename; }	// Don't download unnecessarily
#endif
			var wc = new WebClient();
			wc.DownloadFile("https://download.sysinternals.com/files/SysinternalsSuite.zip", filename);
			return filename;
		}

//---------------------------------------------------------------------------------------

		/// <summary>
		/// Find the downloads directory from the registry. DO NOT assume it's C:\Downloads
		/// </summary>
		/// <returns>The name of the Downloads directory</returns>
		static string GetDownloadDirectoryName() {
			var reg = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppContainer\Storage\microsoft.microsoftedge_8wekyb3d8bbwe\MicrosoftEdge\Main");
			return (string)reg.GetValue("Default Download Directory");
		}
	}
}
