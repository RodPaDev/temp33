﻿using System.IO;
namespace Temp33;


public static class AppConstants {
    public static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Temp33");

    public const string DEVICE_INSTANCE_ID = "USB35INCHIPSV2";

    static AppConstants() {
        Directory.CreateDirectory(SettingsDirectory);
    }

    public const string AppTitle = "Temp33";


}