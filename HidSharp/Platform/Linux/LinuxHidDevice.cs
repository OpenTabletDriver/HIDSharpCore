#region License
/* Copyright 2012-2015, 2017 James F. Bellinger <http://www.zer7.com/software/hidsharp>

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing,
   software distributed under the License is distributed on an
   "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
   KIND, either express or implied.  See the License for the
   specific language governing permissions and limitations
   under the License. */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using HidSharp.Exceptions;

namespace HidSharp.Platform.Linux
{
    using static HidSharp.Platform.Linux.NativeMethods;

    sealed class LinuxHidDevice : HidDevice
    {
        object _getInfoLock;
        string _manufacturer;
        string _productName;
        string _serialNumber;
        byte[] _reportDescriptor;
        int _vid, _pid, _version;
        int _maxInput, _maxOutput, _maxFeature;
        bool _reportsUseID;
        string _path, _fileSystemName;

        LinuxHidDevice()
        {
            _getInfoLock = new object();
        }

        internal static LinuxHidDevice TryCreate(string path)
        {
            var d = new LinuxHidDevice() { _path = path };

            IntPtr udev = NativeMethodsLibudev.Instance.udev_new();
            if (IntPtr.Zero != udev)
            {
                try
                {
                    IntPtr device = NativeMethodsLibudev.Instance.udev_device_new_from_syspath(udev, d._path);
                    if (device != IntPtr.Zero)
                    {
                        string d_manufacturer = NativeMethodsLibudev.Instance.udev_device_get_sysattr_value(device, "manufacturer");
                        string d_productName = NativeMethodsLibudev.Instance.udev_device_get_sysattr_value(device, "product");
                        string d_serialNumber = NativeMethodsLibudev.Instance.udev_device_get_sysattr_value(device, "serial");
                        string d_idVendor = NativeMethodsLibudev.Instance.udev_device_get_sysattr_value(device, "idVendor");
                        string d_idProduct = NativeMethodsLibudev.Instance.udev_device_get_sysattr_value(device, "idProduct");
                        string d_bcdDevice = NativeMethodsLibudev.Instance.udev_device_get_sysattr_value(device, "bcdDevice");
                        Console.WriteLine();
                        string syspath = NativeMethodsLibudev.Instance.udev_device_get_syspath(device);
                        string devnode = NativeMethodsLibudev.Instance.udev_device_get_devnode(device);
                        Console.WriteLine("device " + devnode + " found device with " + d_manufacturer + d_productName + d_idVendor + d_idProduct);
                        try
                        {
                            if (devnode != null)
                            {
                                d._fileSystemName = devnode;
                                IntPtr parent = IntPtr.Zero;
                                string parent_subsystem = null;

                                //if (NativeMethodsLibudev.Instance.udev_device_get_is_initialized(device) > 0)
                                {
                                    /// show information for all parent devices
                                    IntPtr p = device;
                                    string devtype = "";
                                    string subsystem = "";
                                    string p_syspath = "";
                                    string p_devnode = "";
                                    while (p != IntPtr.Zero) {
                                        Console.WriteLine("device " + devnode + " parent " + p + " getting parent of: " + p);
                                        p = NativeMethodsLibudev.Instance.udev_device_get_parent(p);
                                        devtype = NativeMethodsLibudev.Instance.udev_device_get_devtype(p);
                                        subsystem = NativeMethodsLibudev.Instance.udev_device_get_subsystem(p);
                                        p_syspath = NativeMethodsLibudev.Instance.udev_device_get_syspath(p);
                                        p_devnode = NativeMethodsLibudev.Instance.udev_device_get_devnode(p);
                                        // Console.WriteLine("device " + devnode + " parent " + p + " p_syspath is " + p_syspath);
                                        // Console.WriteLine("device " + devnode + " parent " + p + " p_devnode is " + p_devnode);
                                        Console.WriteLine("device " + devnode + " parent " + p + " subsystem/devtpye is " + subsystem + "/" + devtype);
                                        // IntPtr entry = NativeMethodsLibudev.Instance.udev_device_get_properties_list_entry(p);
                                        // while (entry != IntPtr.Zero) {
                                        //     string name = NativeMethodsLibudev.Instance.udev_list_entry_get_name(entry);
                                        //     string value = NativeMethodsLibudev.Instance.udev_list_entry_get_value(entry);
                                        //     entry = NativeMethodsLibudev.Instance.udev_list_entry_get_next(entry);
                                        //     Console.WriteLine("device " + devnode + " parent " + p + " properties " + name + " = " + value);
                                        // }
                                    }


                                    Console.WriteLine("device " + devnode + " parent is " + parent + " with subsystem " + parent_subsystem);

                                    // aquire a parent of the hid subsystem, which my pth-660 has when connected to my on my lenovo x13 via bluetooth
                                    // maybe take the one that comes first when recursing into the parents
                                    IntPtr parent_hid = NativeMethodsLibudev.Instance.udev_device_get_parent_with_subsystem(device, "hid");
                                    IntPtr parent_usb = NativeMethodsLibudev.Instance.udev_device_get_parent_with_subsystem_devtype(device, "usb", "usb_device");

                                    Console.WriteLine("device " + devnode + " parent_hid is " + parent_hid);
                                    Console.WriteLine("device " + devnode + " parent_usb is " + parent_usb);

                                    string manufacturer = "unknown";
                                    string productName = "unknown";
                                    string serialNumber = "unknown";
                                    string idVendor = "0";
                                    string idProduct = "0";
                                    string bcdDevice = "0";

                                    if (IntPtr.Zero != parent_usb) {
                                        manufacturer = NativeMethodsLibudev.Instance.udev_device_get_sysattr_value(parent_usb, "manufacturer");
                                        productName = NativeMethodsLibudev.Instance.udev_device_get_sysattr_value(parent_usb, "product");
                                        serialNumber = NativeMethodsLibudev.Instance.udev_device_get_sysattr_value(parent_usb, "serial");
                                        idVendor = NativeMethodsLibudev.Instance.udev_device_get_sysattr_value(parent_usb, "idVendor");
                                        idProduct = NativeMethodsLibudev.Instance.udev_device_get_sysattr_value(parent_usb, "idProduct");
                                        bcdDevice = NativeMethodsLibudev.Instance.udev_device_get_sysattr_value(parent_usb, "bcdDevice");
                                    }
                                    // add information to bluetooth devices
                                    if (IntPtr.Zero != parent_hid) {
                                        IntPtr entry2 = NativeMethodsLibudev.Instance.udev_device_get_properties_list_entry(parent_hid);
                                        while (entry2 != IntPtr.Zero) {
                                            string name = NativeMethodsLibudev.Instance.udev_list_entry_get_name(entry2);
                                            string value = NativeMethodsLibudev.Instance.udev_list_entry_get_value(entry2);
                                            entry2 = NativeMethodsLibudev.Instance.udev_list_entry_get_next(entry2);
                                            Console.WriteLine("device " + devnode + " parent " + p + " properties " + name + " = " + value);
                                            switch (name) {
                                                case "HID_NAME":
                                                    if (productName == "unknown") {
                                                        productName = value;
                                                    }
                                                break;
                                                case "HID_ID":
                                                    string[] idParts = value.Split(":");
                                                    if (idVendor == "0") {
                                                        idVendor = idParts[1];
                                                    }
                                                    if (idProduct == "0") {
                                                        idProduct = idParts[2];
                                                    }
                                                break;
                                            }
                                        }
                                    }
                                    if (IntPtr.Zero != parent_hid || IntPtr.Zero != parent_usb) {
                                        int vid, pid, version;
                                        if (NativeMethods.TryParseHex(idVendor, out vid) &&
                                            NativeMethods.TryParseHex(idProduct, out pid) &&
                                            NativeMethods.TryParseHex(bcdDevice, out version))
                                        {
                                            d._vid = vid;
                                            d._pid = pid;
                                            d._version = version;
                                            d._manufacturer = manufacturer;
                                            d._productName = productName;
                                            d._serialNumber = serialNumber;

                                            return d;
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            NativeMethodsLibudev.Instance.udev_device_unref(device);
                        }
                    }
                }
                finally
                {
                    NativeMethodsLibudev.Instance.udev_unref(udev);
                }
            }

            return null;
        }

        protected override DeviceStream OpenDeviceDirectly(OpenConfiguration openConfig)
        {
            RequiresGetInfo();

            var stream = new LinuxHidStream(this);
            try { stream.Init(_path); return stream; }
            catch { stream.Close(); throw; }
        }

        public override string GetManufacturer()
        {
            if (_manufacturer == null) { throw DeviceException.CreateIOException(this, "Unnamed manufacturer."); }
            return _manufacturer;
        }

        public override string GetProductName()
        {
            if (_productName == null) { throw DeviceException.CreateIOException(this, "Unnamed product."); }
            return _productName;
        }

        public override string GetSerialNumber()
        {
            if (_serialNumber == null) { throw DeviceException.CreateIOException(this, "No serial number."); }
            return _serialNumber;
        }

        public override int GetMaxInputReportLength()
        {
            RequiresGetInfo();
            return _maxInput;
        }

        public override int GetMaxOutputReportLength()
        {
            RequiresGetInfo();
            return _maxOutput;
        }

        public override int GetMaxFeatureReportLength()
        {
            RequiresGetInfo();
            return _maxFeature;
        }

        public override byte[] GetRawReportDescriptor()
        {
            RequiresGetInfo();
            return (byte[])_reportDescriptor.Clone();
        }

        public override unsafe string GetDeviceString(int index)
        {
            static ushort string_index(byte index)
            {
                return (ushort)((((byte)DESCRIPTOR_TYPE.STRING) << 8) | index);
            }

            // Setup the packet for retrieving supported langId
            usbfs_ctrltransfer setup = new usbfs_ctrltransfer
            {
                bRequestType = (byte)ENDPOINT_DIRECTION.IN,
                bRequest = (byte)STANDARD_REQUEST.GET_DESCRIPTOR,
                wValue = string_index(0),
                wIndex = 0,
                wLength = 255
            };

            string usbPath = GetUsbPath();

            fixed (char* sbuf = new char[255])
            {
                setup.data = sbuf;

                // Send packet
                int usbHandle = open(usbPath, oflag.NONBLOCK | oflag.RDWR);

                try
                {
                    if (ioctl(usbHandle, USBDEVFS_CONTROL, ref setup) < 0)
                    {
                        close(usbHandle);
                        var err = (error)Marshal.GetLastWin32Error();
                        throw new DeviceIOException(this, $"Unable to retrieve device's supported langId: {err}");
                    }

                    // Retrieve langId
                    var buf = (byte*)setup.data;
                    ushort langId = (ushort)(buf[2] | buf[3] << 8);

                    for (int i = 0; i < 255; i++)
                    {
                        buf[i] = 0;
                    }

                    // Retrieve string
                    setup.wIndex = langId;
                    setup.wValue = string_index((byte)index);
                    if (ioctl(usbHandle, USBDEVFS_CONTROL, ref setup) < 0)
                    {
                        var err = (error)Marshal.GetLastWin32Error();
                        throw new DeviceIOException(this, $"Unable to retrieve device string at index {index}: {err}");
                    }
                }
                finally
                {
                    close(usbHandle);
                }

                var deviceString = new StringBuilder(255);
                var ssbuf = (char*)setup.data;
                for (int i = 1; i < 255; i++)
                {
                    var c = ssbuf[i];
                    if (c == 0)
                        break;
                    else
                        deviceString.Append(c);
                }
                return deviceString.ToString();
            }
        }

        bool TryParseReportDescriptor(out Reports.ReportDescriptor parser, out byte[] reportDescriptor)
        {
            parser = null; reportDescriptor = null;

            int handle;
            try { handle = LinuxHidStream.DeviceHandleFromPath(_path, this, NativeMethods.oflag.NONBLOCK); }
            catch (FileNotFoundException) { throw DeviceException.CreateIOException(this, "Failed to read report descriptor."); }

            try
            {
                uint descsize;
                if (NativeMethods.ioctl(handle, NativeMethods.HIDIOCGRDESCSIZE, out descsize) < 0) { return false; }
                if (descsize > NativeMethods.HID_MAX_DESCRIPTOR_SIZE) { return false; }

                var desc = new NativeMethods.hidraw_report_descriptor() { size = descsize };
                if (NativeMethods.ioctl(handle, NativeMethods.HIDIOCGRDESC, ref desc) < 0) { return false; }

                Array.Resize(ref desc.value, (int)descsize);
                parser = new Reports.ReportDescriptor(desc.value);
                reportDescriptor = desc.value; return true;
            }
            finally
            {
                NativeMethods.retry(() => NativeMethods.close(handle));
            }
        }

        void RequiresGetInfo()
        {
            lock (_getInfoLock)
            {
                if (_reportDescriptor != null) { return; }

                Reports.ReportDescriptor parser; byte[] reportDescriptor;
                if (!TryParseReportDescriptor(out parser, out reportDescriptor))
                {
                    throw DeviceException.CreateIOException(this, "Failed to read report descriptor.");
                }

                _maxInput = parser.MaxInputReportLength;
                _maxOutput = parser.MaxOutputReportLength;
                _maxFeature = parser.MaxFeatureReportLength;
                _reportsUseID = parser.ReportsUseID;
                _reportDescriptor = reportDescriptor;
            }
        }

        unsafe string GetUsbPath()
        {
            using (var udev = new SafeUdevHandle(NativeMethodsLibudev.Instance.udev_new()))
            {
                var handle = NativeMethodsLibudev.Instance.udev_device_new_from_syspath(udev.DangerousGetHandle(), _path);
                using (var parent = new SafeUdevDeviceHandle(NativeMethodsLibudev.Instance.udev_device_get_parent_with_subsystem_devtype(handle, "usb", "usb_device")))
                {
                    var parentPtr = parent.DangerousGetHandle();

                    string devNum = NativeMethodsLibudev.Instance.udev_device_get_sysattr_value(parentPtr, "devnum");
                    string busNum = NativeMethodsLibudev.Instance.udev_device_get_sysattr_value(parentPtr, "busnum");

                    return $"/dev/bus/usb/{int.Parse(busNum):D3}/{int.Parse(devNum):D3}";
                }
            }
        }

        public override string GetFileSystemName()
        {
            return _fileSystemName;
        }

        public override bool HasImplementationDetail(Guid detail)
        {
            return base.HasImplementationDetail(detail) || detail == ImplementationDetail.Linux || detail == ImplementationDetail.HidrawApi;
        }

        public override bool IsSibling(HidDevice device)
        {
            if (device is not LinuxHidDevice linuxDevice) { return false; }

            try
            {
                return GetUsbPath() == linuxDevice.GetUsbPath();
            }
            catch
            {
                return false;
            }
        }

        public override string DevicePath
        {
            get { return _path; }
        }

        public override int VendorID
        {
            get { return _vid; }
        }

        public override int ProductID
        {
            get { return _pid; }
        }

        public override int ReleaseNumberBcd
        {
            get { return _version; }
        }

        public override bool CanOpen => true;

        internal bool ReportsUseID
        {
            get { return _reportsUseID; }
        }
    }
}
