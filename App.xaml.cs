using SerialCommunicator;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using SystemHardwareInfo;
using Ui = Wpf.Ui.Controls;

namespace UnykachAio240Display {



    public class DataEventArgs :EventArgs {
        public float DataValue { get; }

        public DataEventArgs(float dataValue) {
            DataValue = dataValue;
        }
    }

    public partial class App :Application {

        private MainWindow? _mainWindow;
        private DispatcherTimer? _timer;
        public Settings settings;

        public readonly SerialPortCommunicator Spc;
        public readonly HardwareMonitor HardwareMonitor;

        public bool isOpenFail = false;

        public App() {
            this.Spc = new SerialPortCommunicator();
            this.HardwareMonitor = new HardwareMonitor();
            this.settings = Settings.Load();
            Trace.WriteLine($"Loaded settings: {this.settings.SensorIdentifier}, {this.settings.HardwareIdentifier} {this.settings.UpdateFrequencySeconds}");
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

        protected void Close() {
            this.Spc.Close();
            this.HardwareMonitor.Close();
            Environment.Exit(0);
        }

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            this._mainWindow = new MainWindow(this);

            if(this.isOpenFail) {
                Ui.MessageBox messageBox = new Ui.MessageBox() {
                    Title = "Connection Failed",
                    Content = new Ui.TextBlock {

                        Text = "Connection to LED screen failed. Make sure you only have one program running.",
                        TextWrapping = TextWrapping.Wrap,
                    },
                };
                messageBox.ShowDialogAsync();
                this.Close();
            }
            this._mainWindow.Show();
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
                    this.Spc.SendInt(intVal);
                    if(this.MainWindow != null) {
                        OnDataUpdated(new DataEventArgs(value.Value));
                    }

                }
            }
        }
    }



}
