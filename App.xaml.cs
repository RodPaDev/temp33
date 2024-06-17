using SerialCommunicator;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Forms;
using System.Drawing;
using SystemHardwareInfo;
using SysWin = System.Windows;
using Ui = Wpf.Ui.Controls;
using System.IO;
using System;
using Microsoft.Win32;

namespace Temp33 {



    public class DataEventArgs :EventArgs {
        public float DataValue { get; }

        public DataEventArgs(float dataValue) {
            DataValue = dataValue;
        }
    }

    public partial class App :SysWin.Application {

        private MainWindow? _mainWindow;
        private DispatcherTimer? _timer;
        public Settings settings;
        private NotifyIcon _notifyIcon;

        public readonly SerialPortCommunicator Spc;
        public readonly HardwareMonitor HardwareMonitor;

        public bool isOpenFail = false;
        private bool isExit = false;
        private bool isFirstLaunch = true;
        private int? prevIntVal = null;

        public App() {
            this.Spc = new SerialPortCommunicator();
            this.HardwareMonitor = new HardwareMonitor();
            this.settings = Settings.Load();
            this.isFirstLaunch = this.settings.HardwareIdentifier == null;
            this.Init();
            this.HandleSerialUpdateTimer();
        }

        public event EventHandler<DataEventArgs> DataUpdated;

        protected virtual void OnDataUpdated(DataEventArgs e) {
            DataUpdated?.Invoke(this, e);
        }

        public void Init() {
            try {
                this.Spc.Open();
            } catch {
                this.isOpenFail = true;
            }
        }

        protected void Quit() {
            this.Spc.Close();
            this.HardwareMonitor.Close();
            Environment.Exit(0);
        }

        public static void SetStartup(bool isEnabled) {
            string appName = AppConstants.AppTitle;
            string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (key != null) {
                if (isEnabled) {
                    key.SetValue(appName, appPath);
                } else {
                    key.DeleteValue(appName, false);
                }
            }
        }

        protected override void OnStartup(SysWin.StartupEventArgs e) {
            base.OnStartup(e);
            this._mainWindow = new MainWindow(this);

            /* Error if there is already an instance of a program running that is trying to connect to the LED screen */
            if (this.isOpenFail) {
                Ui.MessageBox messageBox = new Ui.MessageBox() {
                    Title = "Connection Failed",
                    Content = new Ui.TextBlock {

                        Text = "Connection to LED screen failed. Make sure you only have one program running.",
                        TextWrapping = SysWin.TextWrapping.Wrap,
                    },
                };
                messageBox.ShowDialogAsync();
                this.Quit();
            }

            this.InitNotifyIcon();
            if (isFirstLaunch) {
                this.ShowMainWindow();
                this._mainWindow.SnackbarService.Show(
                    "Minimize Instead of Closing", 
                    "Clicking the 'Close' button will minimize the application to the system tray. To fully exit, right-click the tray icon and select 'Close'.",
                    Ui.ControlAppearance.Secondary, 
                    new Ui.SymbolIcon(Ui.SymbolRegular.Warning12, 14, true), 
                    TimeSpan.FromSeconds(10)
                );
            } 

        }



        private void InitNotifyIcon() {
            /* NotifyIcon */
            if (this._mainWindow != null) {


                this._mainWindow.Closing += MainWindow_Closing;

                Stream iconStream = GetResourceStream(new Uri("pack://application:,,,/Temp33;component/Assets/temp33.ico")).Stream;
                Icon icon = new(iconStream);
                // Set up the notify icon
                _notifyIcon = new NotifyIcon {
                    Icon = icon,
                    ContextMenuStrip = new ContextMenuStrip(),
                    Visible = true
                };
                ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
                contextMenuStrip.Items.Add("Open", null, (s, args) => ShowMainWindow());
                contextMenuStrip.Items.Add("Quit", null, (s, args) => Quit());
                _notifyIcon.ContextMenuStrip = contextMenuStrip;
                _notifyIcon.DoubleClick += (s, args) => ShowMainWindow();
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e) {
            if (!isExit) {
                e.Cancel = true;
                MainWindow.Hide();
            }
        }

        private void MainWindow_Closed(object? sender, System.EventArgs e) {
            isExit = true;
            Quit();
        }

        private void ShowMainWindow() {
            if (MainWindow.IsVisible) {
                if (MainWindow.WindowState == SysWin.WindowState.Minimized) {
                    MainWindow.WindowState = SysWin.WindowState.Normal;
                }
                MainWindow.Activate();
            } else {
                MainWindow.Show();
            }
        }

        private void HandleSerialUpdateTimer() {
            this._timer?.Stop();
            this._timer = null;
            _timer = new() {
                Interval = TimeSpan.FromSeconds(this.settings.UpdateFrequencySeconds)
            };
            _timer.Tick += SendSerialUpdate;

            _timer.Start();
        }

        public void UpdateSettings(string sensorIdentifier, string hardwareIdentifier, int updateFrequencySeconds) {
            this.settings.SensorIdentifier = sensorIdentifier;
            this.settings.HardwareIdentifier = hardwareIdentifier;
            this.settings.UpdateFrequencySeconds = updateFrequencySeconds;
            this.settings.Save();
            this.HandleSerialUpdateTimer();
        }

        private void SendSerialUpdate(object? sender, EventArgs e) {
            if (this.settings?.SensorIdentifier != null) {
                float? value = this.HardwareMonitor.GetMeasurement(this.settings.SensorIdentifier);
                if (value.HasValue) {
                    int intVal = (int)Math.Truncate(value.Value);

                    if (!prevIntVal.HasValue || prevIntVal.Value != intVal) {
                        this.Spc.SendInt(intVal);
                        if (this.MainWindow != null) {
                            OnDataUpdated(new DataEventArgs(value.Value));
                        }

                        prevIntVal = intVal;
                    }
                }
            }
        }
    }



}
