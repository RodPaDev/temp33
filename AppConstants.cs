using System.IO;
namespace Temp33;


public static class AppConstants {
    public const string AppTitle = "Temp33";
    public const string DEVICE_INSTANCE_ID = "USB35INCHIPSV2";
    public static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppTitle);


    static AppConstants() {
        Directory.CreateDirectory(SettingsDirectory);
    }



}