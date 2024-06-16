using SerialCommunicator;
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

    public class Settings {
        public int updateFrequencySeconds;
        public string? sensorIdentifier;

        public Settings(string? sensorIdentifier, int updateFrequencySeconds = 1) {
            this.updateFrequencySeconds = updateFrequencySeconds;
            this.sensorIdentifier = sensorIdentifier;
        }
    }

    public partial class App :Application {

        private MainWindow? _mainWindow;
        private DispatcherTimer? _timer;
        public Settings _settings;

        public readonly SerialPortCommunicator Spc;
        public readonly HardwareMonitor HardwareMonitor;

        public bool isOpenFail = false;

        public App() {
            this.Spc = new SerialPortCommunicator();
            this.HardwareMonitor = new HardwareMonitor();
            this._settings = new Settings(null);
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
                Interval = TimeSpan.FromSeconds(this._settings.updateFrequencySeconds)
            };
            _timer.Tick += SendSerialUpdate;

            _timer.Start();
        }

        public void UpdateSettings( string sensorIdentifier,int updateFrequencySeconds) {
            this._settings = new Settings(sensorIdentifier, updateFrequencySeconds);
            this.HandleSerialUpdateTimer();
        }   

        private void SendSerialUpdate(object? sender, EventArgs e) {
            if (this._settings?.sensorIdentifier != null) {
                float? value = this.HardwareMonitor.GetMeasurement(this._settings.sensorIdentifier);
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
