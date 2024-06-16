using SerialCommunicator;
using System.Windows;
using SystemHardwareInfo;
using Ui = Wpf.Ui.Controls;

namespace UnykachAio240Display {

    public partial class App :Application {

        private MainWindow? _mainWindow;

        public readonly SerialPortCommunicator Spc;
        public readonly HardwareMonitor HardwareMonitor;

        public bool isOpenFail = false;

        public App() {
            this.Spc = new SerialPortCommunicator();
            this.HardwareMonitor = new HardwareMonitor();
            this.Init();
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


    }



}
