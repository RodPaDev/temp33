using SerialCommunicator;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Threading;
using SystemHardwareInfo;
using Wpf.Ui.Controls;
using SysWin = System.Windows;
using SysWinCtrl = System.Windows.Controls;


namespace UnykachAio240Display {
    public interface IWindow {
        event SysWin.RoutedEventHandler Loaded;
        void Show();
    }

    public class HardwareSelection {
        private string _hardwareType;
        public HardwareSelection(string hardwareType) {
            this._hardwareType = hardwareType;
        }

        public Tuple<string, string> GetName(string sensorName, string sensorType) {
            return Tuple.Create($"{this._hardwareType} {sensorType}", sensorType);
        }
    }

    public class Metadata {
        public string Type { get; set; }
        public Metadata(string type) {
            Type = type;
        }
    }

    public partial class MainWindow :IWindow, INotifyPropertyChanged {
        private App _app;
        private DispatcherTimer? _timer;

        private HardwareSelection? hardwareSelection;

        public event PropertyChangedEventHandler? PropertyChanged;

        /* Combo Boxes */
        public ObservableCollection<SysWinCtrl.ComboBoxItem> HardwareItems { get; private set; }
        private SysWinCtrl.ComboBoxItem? _backingSelectedHardwareItem;
        public SysWinCtrl.ComboBoxItem? SelectedHardwareItem {
            get => _backingSelectedHardwareItem;
            set {
                if (_backingSelectedHardwareItem != value) {
                    _backingSelectedHardwareItem = value;
                    OnPropertyChanged(nameof(SelectedHardwareItem));
                    this.BindSensorItems();
                    this.UpdateMeasurementNameText();
                    this.HandleDisplayTimer();
                }
            }
        }

        public ObservableCollection<SysWinCtrl.ComboBoxItem> SensorItems { get; private set; }
        private SysWinCtrl.ComboBoxItem? _backingSelectedSensorItem;
        public SysWinCtrl.ComboBoxItem? SelectedSensorItem {
            get => _backingSelectedSensorItem;
            set {
                if (_backingSelectedSensorItem != value) {
                    _backingSelectedSensorItem = value;
                    OnPropertyChanged(nameof(SelectedSensorItem));
                }
            }
        }

        /* Text Block */
        private string? _backingDisplayValueText;
        public string? DisplayValueText {
            get => _backingDisplayValueText;
            set {
                if (_backingDisplayValueText != value) {
                    _backingDisplayValueText = value;
                    OnPropertyChanged(nameof(DisplayValueText));
                }
            }
        }

        private string? _backingSensorValueText;
        public string? SensorValueText {
            get => _backingSensorValueText;
            set {
                if (_backingSensorValueText != value) {
                    _backingSensorValueText = value;
                    OnPropertyChanged(nameof(SensorValueText));
                }
            }
        }

        private string? _backingMeasurementNameText;
        public string? MeasurementNameText {
            get => _backingMeasurementNameText;
            set {
                if (_backingMeasurementNameText != value) {
                    _backingMeasurementNameText = value;
                    OnPropertyChanged(nameof(MeasurementNameText));
                }
            }
        }

        private int? _backingUpdateFrequencyValue;
        public int? UpdateFrequencyValue {
            get => _backingUpdateFrequencyValue;
            set {
                if (_backingUpdateFrequencyValue != value) {
                    _backingUpdateFrequencyValue = value;
                    OnPropertyChanged(nameof(UpdateFrequencyValue));
                }
            }
        }

        public MainWindow(App app) {
            /* Combo Boxes */
            this._app = app;

            this.SelectedHardwareItem = null;
            this.HardwareItems = [];
            this.SelectedSensorItem = null;
            this.SensorItems = [];
            /* Text Block */
            this.DisplayValueText = "33";
            this.UpdateFrequencyValue = 5;


            this.BindHardwareItems();
            this.BindSensorItems();
            this.DataContext = this;
            InitializeComponent();
        }

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



        private void BindHardwareItems() {
            var hardwareOptions = this._app.HardwareMonitor.GetAvailableHardware();
            HardwareItems.Clear();
            foreach (var hardwareOption in hardwareOptions) {
                var item = new SysWinCtrl.ComboBoxItem {
                    Content = $"[{hardwareOption.Type}] - {hardwareOption.Name}",
                    Uid = hardwareOption.Identifier,
                    Tag = new Metadata(hardwareOption.Type)
                };
                this.HardwareItems.Add(item);
                if (SelectedHardwareItem == null) {
                    this.SelectedHardwareItem = item;
                }
            }
        }

        private void BindSensorItems() {
            if (SelectedHardwareItem != null) {
                var sensorOptions = this._app.HardwareMonitor.GetSensorOptions(SelectedHardwareItem.Uid);
                this.SelectedSensorItem = null;
                this.SensorItems.Clear();
                foreach (var sensorOption in sensorOptions) {
                    var item = new SysWinCtrl.ComboBoxItem {
                        Content = sensorOption.Name,
                        Uid = sensorOption.Identifier,
                        Tag = new Metadata(sensorOption.Type)
                    };
                    this.SensorItems.Add(item);
                    if (SelectedSensorItem == null) {
                        this.SelectedSensorItem = item;
                    }
                }
            }
        }

        private void UpdateMeasurementNameText() {
            string hardwareType = String.Empty;
            string sensorType = String.Empty;
            if (this.SelectedHardwareItem?.Tag is Metadata hardwareMetadata) {
                hardwareType = hardwareMetadata.Type;
            }
            if (this.SelectedSensorItem?.Tag is Metadata sensorMetadata) {
                sensorType = sensorMetadata.Type;
            }

            this.MeasurementNameText = $"{hardwareType} {sensorType}";
        }

        private void HandleDisplayTimer() {
            this._timer?.Stop();
            this._timer = null;
            _timer = new() {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += UpdateDisplayValueText;

            _timer.Start();
        }

        private void UpdateDisplayValueText(object? sender, EventArgs e) {
            if (this.SelectedSensorItem != null) {
                float? value = this._app.HardwareMonitor.GetMeasurement(SelectedSensorItem.Uid);
                if (value.HasValue) {
                    string text = Math.Truncate(value.Value).ToString();
                    if (text.Length == 1) {
                        text = "0" + text;
                    }

                    DisplayValueText = text[..2];
                    SensorValueText = value.Value.ToString("F2");
                }
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e) {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void SaveChanges__Click(object sender, EventArgs e) {

            // TODO: Send this by update frequency. Delegate task to background worker maybe??
            if (this.SelectedSensorItem != null) {
                float? value = this._app.HardwareMonitor.GetMeasurement(SelectedSensorItem.Uid);
                if (value.HasValue) {
                 
                    int intVal = (int)Math.Truncate(value.Value);
                    this._app.Spc.SendInt(intVal);

                    
                }
            }
        }

    }


}
