using System.Management;
using System.IO.Ports;
using System.Diagnostics;
namespace SerialCommunicator;

public class SerialPortCommunicator {

    public struct EncodedBytes {
        public byte[] Bytes;

        public EncodedBytes(int size) {
            Bytes = new byte[size];
        }

        public byte this[int index] {
            get { return Bytes[index]; }
            set { Bytes[index] = value; }
        }

    }

    public readonly SerialPort port;
    const string DEVICE_INSTANCE_ID = "USB35INCHIPSV2"; // This is what it appears to be not sure if always true

    static EncodedBytes EncodeIntToBuffer(int intToDisplay, int controlCode = 130) {
        // not sure what these do, but keeping them separate for future referncing
        int A_2 = 0, A_3 = 0, A_4 = 0;

        EncodedBytes buffer = new (6);

        buffer[0] = (byte)(intToDisplay >> 2);
        buffer[1] = (byte)(((intToDisplay & 3) << 6) + (A_2 >> 4));
        buffer[2] = (byte)(((A_2 & 15) << 4) + (A_3 >> 6));
        buffer[3] = (byte)(((A_3 & 63) << 2) + (A_4 >> 8));
        buffer[4] = (byte)(A_4 & 255);
        buffer[5] = (byte)controlCode;


        return buffer;
    }

    public SerialPortCommunicator() {

        string? comPort = FindDevice();

        if (comPort == null) {
            throw new Exception("DEVICE_NOT_FOUND");
        }

        this.port = new SerialPort(comPort) {
            DtrEnable = true,
            RtsEnable = true,
            ReadTimeout = 1000,
            BaudRate = 115200,
            DataBits = 8,
            StopBits = StopBits.One,
            Parity = Parity.None
        };
    }

    public static string? FindDevice() {
        using var searcher = new ManagementObjectSearcher("SELECT * FROM WIN32_SerialPort");
        foreach (ManagementObject queryObj in searcher.Get().Cast<ManagementObject>()) {
            if (queryObj["PNPDeviceID"] is string pnpDeviceId) {
                if (pnpDeviceId.Contains(DEVICE_INSTANCE_ID)) {
                    if (queryObj["DeviceID"] is string deviceId) {
                        return deviceId;
                    } 
                }
            }
        }
        return null;
    }


    public void Open() {
        if (!this.port.IsOpen) {
            this.port.Open();
            Trace.WriteLine("Port Opened");
        }
    }

    public void Close() {
        if (this.port.IsOpen) {
            this.port.Close();
        }
    }

    public void SendInt(int intToDisplay) {
        if (this.port.IsOpen) {
            EncodedBytes data = EncodeIntToBuffer(intToDisplay);
            port.Write(data.Bytes, 0, data.Bytes.Length);
        }
    }



}

