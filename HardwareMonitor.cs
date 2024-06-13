using LibreHardwareMonitor.Hardware;
using System.Diagnostics;
using System.Linq;

namespace SystemHardwareInfo {

    public class HardwareOption {
        public required string Identifier { get; set; }
        public required string Name { get; set; }
        public required string Type { get; set; }
    }

    public class SensorOption {
        public required string Identifier { get; set; }
        public required string Name { get; set; }

        public required string Type { get; set; }
    }


    public class HardwareMonitor {
        private readonly Computer _computer;
        private readonly Dictionary<string, List<SensorOption>> _hardwareSensors = new Dictionary<string, List<SensorOption>>();
        private readonly List<string> gpuTypes = new() { HardwareType.GpuIntel.ToString(), HardwareType.GpuNvidia.ToString(), HardwareType.GpuAmd.ToString() };

        public static readonly Dictionary<string, int> SensorTypePriority = new (){
            { "Temperature", 1 },
            { "Load", 2 },
            { "Voltage", 3 },
            { "Clock", 4 },
            { "Fan", 5 },
            { "Flow", 6 },
            { "Control", 7 },
            { "Level", 8 },
            { "Factor", 9 },
            { "Power", 10 },
            { "Data", 11 },
            { "SmallData", 12 },
            { "Throughput", 13 }
        };
        public HardwareMonitor() {
            _computer = new Computer {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = false
            };
            _computer.Open();
            MapAvailableHardware();
        }

        private void MapAvailableHardware() {
            foreach (var hardware in _computer.Hardware) {
                hardware.Update();
                var sensorTypes = new List<SensorOption>();
                foreach (var sensor in hardware.Sensors) {
                        sensorTypes.Add(new SensorOption { 
                            Identifier = sensor.Identifier.ToString(), 
                            Name = $"[{sensor.SensorType}] - {sensor.Name}",
                            Type = sensor.SensorType.ToString(), 
                        });
                }
                sensorTypes.Sort((a, b) => {
                    int priorityA = SensorTypePriority.TryGetValue(a.Type, out int pa) ? pa : int.MaxValue;
                    int priorityB = SensorTypePriority.TryGetValue(b.Type, out int pb) ? pb : int.MaxValue;
                    return priorityA.CompareTo(priorityB);
                });

                _hardwareSensors[hardware.Identifier.ToString()] = sensorTypes;
            }
        }

        public IEnumerable<HardwareOption> GetAvailableHardware() {
            return _computer.Hardware.Select(hardware => {
                var type = hardware.HardwareType.ToString();
                if (gpuTypes.Contains(type)) {
                    type = "Gpu";
                }

                return new HardwareOption {
                    Identifier = hardware.Identifier.ToString(),
                    Name = hardware.Name,
                    Type = type.ToUpper(),
                };
            });
        }

        public IEnumerable<SensorOption> GetSensorOptions(string hardwareIdentifier) {
            if (_hardwareSensors.TryGetValue(hardwareIdentifier, out var options)) {
                return options;
            }
            return [];
        }

        public float? GetMeasurement(string sensorIdentifier) {
            foreach (var hardware in _computer.Hardware) {
                hardware.Update();
                var sensor = hardware.Sensors.FirstOrDefault(s => s.Identifier.ToString() == sensorIdentifier);
                if (sensor != null && sensor.Value.HasValue) {
                    return sensor.Value;
                }
            }
            return null;
        }
    }
}