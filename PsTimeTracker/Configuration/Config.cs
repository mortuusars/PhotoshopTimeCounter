﻿using System.ComponentModel;
using PSTimeTracker.PsTracking;

namespace PSTimeTracker.Configuration
{
    public class Config : INotifyPropertyChanged
    {
        #pragma warning disable 0067
        public event PropertyChangedEventHandler PropertyChanged;
        #pragma warning restore 0067

        public bool AlwaysOnTop { get; set; }
        public Sorting SortBy { get; set; }
        public bool IgnoreWindowState { get; set; }
        public bool IgnoreAFKTimer { get; set; }
        public int NumberOfRecordsToKeep { get; set; } = 6;
        public bool DisplayErrorMessage { get; set; } = true;
        public bool UseLegacyTrackingMethod { get; set; } = false;
        public bool CheckForUpdates { get; set; } = true;
    }
}
