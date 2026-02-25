using System;
using HidSharp;
using HidSharp.Platform.Linux;


var devices = HidSharp.DeviceList.Local.GetHidDevices();

Console.WriteLine();
Console.WriteLine("showing `DeviceList.Local.GetHidDevices()`:");
Console.WriteLine();

foreach (var device in devices) {
	Console.WriteLine(device);
}

Console.WriteLine();
