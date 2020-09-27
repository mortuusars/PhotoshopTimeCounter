﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Text.Json;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;

namespace PhotoshopTimeCounter
{
    public class TimeCounter
    {
        private const string JSON_FILENAME = "files.json";
        private string JSON_FILEPATH = App.APP_FOLDER_PATH + JSON_FILENAME;

        #region Last Input Time

        [DllImport("user32.dll", SetLastError = false)]
        private static extern bool GetLastInputInfo(ref Lastinputinfo plii);
        private static readonly DateTime SystemStartup = DateTime.Now.AddMilliseconds(-Environment.TickCount);

        [StructLayout(LayoutKind.Sequential)]
        private struct Lastinputinfo
        {
            public uint cbSize;
            public readonly int dwTime;
        }

        public static DateTime LastInput => SystemStartup.AddMilliseconds(LastInputTicks);
        public static TimeSpan IdleTime => DateTime.Now.Subtract(LastInput);

        private static int LastInputTicks
        {
            get {
                var lastInputInfo = new Lastinputinfo { cbSize = (uint)Marshal.SizeOf(typeof(Lastinputinfo)) };
                GetLastInputInfo(ref lastInputInfo);
                return lastInputInfo.dwTime;
            }
        }

        #endregion

        #region Get Active Process

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        #endregion

        public event EventHandler<int> SummaryChanged;

        /// <summary>
        /// Contains opened files.
        /// </summary>
        public ObservableCollection<PsFileInfo> Files;

        /// <summary>
        /// Time of all files combined.
        /// </summary>
        public int SummarySeconds;

        PsFileInfo currentlyOpenedFile;

        public TimeCounter() {
            
            var existingList = RestorePreviousRecord();
            
            if (existingList != null)
                Files = new ObservableCollection<PsFileInfo>(existingList);
            else 
                Files = new ObservableCollection<PsFileInfo>();

        }


        /// <summary>
        /// Start collecting and writing data about opened files.
        /// </summary>
        public async void Start() {
            while (true) {

                if (IdleTime.TotalSeconds < 10) {
                    CollectInfo();
                    if (DateTimeOffset.Now.Second % 5 == 0)
                        WriteToFile(Files);
                }

                await Task.Delay(1000);

            }
        }


        /// <summary>
        /// 
        /// </summary>
        private void CollectInfo() {

            Process process;

            // Get active window process
            try {
                IntPtr hWnd = GetForegroundWindow();
                uint processId = 0;
                GetWindowThreadProcessId(hWnd, out processId);
                process = Process.GetProcessById((int)processId);
            }
            catch (Exception ex) {
                MessageBox.Show($"Can't get active window process: {ex.Message}", App.APP_NAME, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (process.ProcessName != "Photoshop" || process == null)
                return;


            // Get filename from PS window title
            string fileName = Regex.Match(process.MainWindowTitle, @".*\s@").Value;
            fileName = fileName.Replace(" @", "");

            if (string.IsNullOrWhiteSpace(fileName))
                return;


            // Find current filename in list, if it was opened before
            // Add filename to list if it's new
            var listWithNeededFile = Files.Where(f => f.FileName == fileName);

            currentlyOpenedFile = listWithNeededFile.Count() == 0 ? null : listWithNeededFile?.First();

            if (currentlyOpenedFile == null) {
                currentlyOpenedFile = new PsFileInfo() { FileName = fileName, FirstOpenTime = DateTimeOffset.Now };
                Files.Add(currentlyOpenedFile);
            }

            // Add seconds
            currentlyOpenedFile.SecondsActive++;
            CalculateSummarySeconds();
        }

        public void CalculateSummarySeconds() {
            SummarySeconds = 0;

            foreach (var fileEntry in Files) {
                SummarySeconds += fileEntry.SecondsActive;
            }

            SummaryChanged?.Invoke(this, SummarySeconds);
        }


        #region Reading-Writing

        /// <summary>
        /// Save current list to file.
        /// </summary>
        /// <param name="listOfFiles"></param>
        private void WriteToFile(ICollection<PsFileInfo> listOfFiles) {            

            string json = JsonSerializer.Serialize(listOfFiles, new JsonSerializerOptions() { WriteIndented = true });

            try {
                File.WriteAllText(JSON_FILEPATH, json);
            }
            catch (Exception ex) {
                MessageBox.Show($"Error writing to file: {ex.Message}", App.APP_NAME, MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        /// <summary>
        /// Read list from file.
        /// </summary>
        /// <returns></returns>
        private ICollection<PsFileInfo> RestorePreviousRecord() {

            ICollection<PsFileInfo> restoredList = null;

            Exception exception = null;

            try {
                string jsonString = File.ReadAllText(JSON_FILEPATH);
                restoredList = JsonSerializer.Deserialize<ICollection<PsFileInfo>>(jsonString);
            }
            catch (Exception ex) {
                exception = ex;
            }

            if (restoredList == null || restoredList.Count == 0)
                return null;

            var result = MessageBox.Show("Restore previous record?", App.APP_NAME, MessageBoxButton.YesNo, MessageBoxImage.Question,
                MessageBoxResult.No, MessageBoxOptions.None);

            if (result == MessageBoxResult.Yes) {
                if (exception != null)
                    MessageBox.Show($"Restoring previous record failed. {exception.Message}");
                return restoredList;
            }
            else
                return null;
        }

        #endregion
    }
}