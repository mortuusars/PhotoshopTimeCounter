﻿using System.Windows.Input;
using PSTimeTracker.Services;

namespace PSTimeTracker.ViewModels
{
    internal class ConfigViewModel
    {
        public Config Config { get; set; } = ConfigManager.Config;

        public ICommand SaveConfigCommand { get; }
        public ICommand CloseCommand { get; }

        private readonly IViewManager _viewManager;

        public ConfigViewModel(IViewManager viewManager)
        {
            _viewManager = viewManager;

            //SaveConfigCommand = new RelayCommand(_viewManager => )
            CloseCommand = new RelayCommand(_ => _viewManager.CloseConfigView());
        }
    }
}