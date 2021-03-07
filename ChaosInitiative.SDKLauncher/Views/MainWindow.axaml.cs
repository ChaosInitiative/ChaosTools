﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ChaosInitiative.SDKLauncher.Util;
using ChaosInitiative.SDKLauncher.ViewModels;
using ReactiveUI;

namespace ChaosInitiative.SDKLauncher.Views
{
    public class MainWindow : ReactiveWindow<MainWindowViewModel>
    {

        protected Button EditProfileButton => this.FindControl<Button>("EditProfileButton");
        
        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
            this.AttachDevTools();

            this.WhenActivated(disposables =>
            {
                ViewModel.OnClickEditProfile.Subscribe(_ => EditProfile()).DisposeWith(disposables);
                ViewModel.OnClickOpenHammer.Subscribe(_ => LaunchTool("hammer", 
                                                                      "-nosteam -nop4", 
                                                                      true))
                         .DisposeWith(disposables);
                ViewModel.OnClickOpenModelViewer.Subscribe(_ => 
                                                               LaunchTool("hlmv", 
                                                                          $"-game {ViewModel.CurrentProfile.Mod.Mount.PrimarySearchPath}", 
                                                                          true, 
                                                                          ViewModel.CurrentProfile.Mod.Mount.MountPath))
                         .DisposeWith(disposables);
                ViewModel.ShowNotification = ReactiveCommand.Create<string>(ShowNotification).DisposeWith(disposables);
            });
            
            Closing += OnClosing;
        }

        private void ShowNotification(string message)
        {
            NotificationDialog dialog = new NotificationDialog(message);
            dialog.ShowDialog(this);
        }

        private void OnClosing(object? sender, CancelEventArgs e)
        {
            ViewModel.Config.Save();
        }

        private void EditProfile()
        {
            ProfileConfigWindow profileConfigWindow = new ProfileConfigWindow
            {
                DataContext = new ProfileConfigViewModel(ViewModel.CurrentProfile)
            };
            profileConfigWindow.ShowDialog(this);
        }
        
        private void LaunchTool(string executableName, string args = "", bool windowsOnly = false, string workingDir = null)
        {
            if (windowsOnly && !OperatingSystem.IsWindows())
            {
                ViewModel.ShowNotification?.Execute("This tool is windows-only");
                return;
            }
            
            string binDir = ViewModel.CurrentProfile.Mod.Mount.BinDirectory;

            if (string.IsNullOrWhiteSpace(binDir))
                return; 
            
            string extension = OperatingSystem.IsWindows() ? ".exe" : "";
            string executablePath = $"{binDir}/{executableName}{extension}";

            if (!File.Exists(executablePath))
            {
                ShowNotification($"Unable to find tool binary '{executablePath}'");
                return;
            }
            
            var hammerProcessStartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = workingDir ?? binDir,
                Arguments = args
            };
            
            var process = Process.Start(hammerProcessStartInfo);
        }
    }
}
