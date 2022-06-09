﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Globalization;
using Bonsai.Expressions;
using System.Threading;
using Tinkerforge;

namespace Bonsai.Tinkerforge
{
    internal class UidConverter : StringConverter
    {
        // Cache for discovered Brick / Bricklet devices
        private Dictionary<string, DeviceData> devices;

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        // From display string --> Uid string
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var text = value as string;
            return text != null
                ? text.Split(':')[1]
                : base.ConvertFrom(context, culture, value);
        }

        // From Uid string --> display string
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (devices == null) LookupDevices(context);
            var text = value as string;
            return destinationType == typeof(string) && text != null
                ? devices[text].ToString()
                : base.ConvertTo(context, culture, value, destinationType);
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            // Get the currently available device names
            return LookupDevices(context);
        }

        // For Bricklets, we have to specify a UID to connect to. To get that, we need to know what IP connections are present,
        // and what UIDs are present on those connections
        private StandardValuesCollection LookupDevices(ITypeDescriptorContext context)
        {
            if (context != null)
            {
                // We need to know which CreateIPConnection nodes are present, to know what channels we should look for devices on
                // Therefore we search the workflow for CreateIPConnection nodes. From each node we extract the Host / Port
                var workflowBuilder = (WorkflowBuilder)context.GetService(typeof(WorkflowBuilder));
                if (workflowBuilder != null)
                {
                    var connectionIDs = (from builder in workflowBuilder.Workflow.Descendants()
                                         let createIP = ExpressionBuilder.GetWorkflowElement(builder) as CreateBrickConnection
                                         where createIP != null
                                         select new ConnectionID { HostName = createIP.HostName, Port = createIP.Port })
                                            .Distinct().ToList();

                    // For each IP connection, we search the connected devices and add it to the list - TODO test for multiple IP
                    devices = new Dictionary<string, DeviceData>();
                    foreach (ConnectionID connectionID in connectionIDs)
                    {
                        var ipcon = new IPConnection();
                        ipcon.EnumerateCallback += EnumerateConnection;
                        ipcon.Connect(connectionID.HostName, connectionID.Port);
                        ipcon.Enumerate();
                        /// N.B. GetStandardValues is called twice by Windows Forms. Once to check the dropdown list and then again
                        /// to check cursor position in the list. This is annoying here because it means the enumerate thread will
                        /// be called twice leading to e.g. the device list being cleared in the middle of population. The 'hack' 
                        /// here is to sleep the thread so it can complete before the 2nd call. Not optimal but the Enumerate 
                        /// method doesn't give us much choice. TODO - look for more elegant solution
                        Thread.Sleep(10);
                        ipcon.Disconnect();
                    }
                }
                return new StandardValuesCollection(devices.Values.ToList());
            }

            return base.GetStandardValues(context);
        }

        // Callback for IPConnection.Enumerate() that adds devices to the device list
        private void EnumerateConnection(IPConnection sender, string uid, string connectedUid, char position,
            short[] hardwareVersion, short[] firmwareVersion, int deviceIdentifier, short enumerationType)
        {
            DeviceData discoveredDevice = new DeviceData(uid, connectedUid, position, deviceIdentifier);
            devices.Add(discoveredDevice.Uid, discoveredDevice);
        }

        public struct ConnectionID
        {
            public string HostName;
            public int Port;

            public override string ToString()
            {
                return $"{HostName}:{Port}";
            }
        }

        // Data representation of a connected TinkerForge module
        internal class DeviceData
        {
            public string Uid; // Unique module ID
            public string ConnectedUid; // IDs of connected modules
            public char Position; // Position in the network
            public int DeviceIdentifier; // Number corresponding to device name

            public DeviceData(string uid, string connectedUid, char position, int deviceIdentifier)
            {
                Uid = uid;
                ConnectedUid = connectedUid;
                Position = position;
                DeviceIdentifier = deviceIdentifier;
            }

            public override string ToString()
            {
                return $"{TinkerforgeDeviceLookup.Defaults[DeviceIdentifier]}:{Uid}";
            }
        }

        // Tinkerforge modules have a standard int->Name lookup
        internal class TinkerforgeDeviceLookup
        {
            public static readonly Dictionary<int, string> Defaults = new Dictionary<int, string>
            {
                { BrickMaster.DEVICE_IDENTIFIER,  BrickMaster.DEVICE_DISPLAY_NAME},
                { BrickletAmbientLightV3.DEVICE_IDENTIFIER,  BrickletAmbientLightV3.DEVICE_DISPLAY_NAME},
                { BrickletCO2V2.DEVICE_IDENTIFIER, BrickletCO2V2.DEVICE_DISPLAY_NAME },
                { BrickletAccelerometer.DEVICE_IDENTIFIER, BrickletAccelerometer.DEVICE_DISPLAY_NAME },
                { BrickletAirQuality.DEVICE_IDENTIFIER, BrickletAirQuality.DEVICE_DISPLAY_NAME },
                { BrickletGPSV2.DEVICE_IDENTIFIER, BrickletGPSV2.DEVICE_DISPLAY_NAME },
                { BrickletHumidityV2.DEVICE_IDENTIFIER, BrickletHumidityV2.DEVICE_DISPLAY_NAME },
                { BrickletIndustrialAnalogOutV2.DEVICE_IDENTIFIER, BrickletIndustrialAnalogOutV2.DEVICE_DISPLAY_NAME },
                { BrickletIndustrialPTC.DEVICE_IDENTIFIER, BrickletIndustrialPTC.DEVICE_DISPLAY_NAME },
                { BrickletParticulateMatter.DEVICE_IDENTIFIER, BrickletParticulateMatter.DEVICE_DISPLAY_NAME },
                { BrickletSoundPressureLevel.DEVICE_IDENTIFIER, BrickletSoundPressureLevel.DEVICE_DISPLAY_NAME },
                { BrickletThermocoupleV2.DEVICE_IDENTIFIER, BrickletThermocoupleV2.DEVICE_DISPLAY_NAME },
                { BrickletAnalogInV3.DEVICE_IDENTIFIER, BrickletAnalogInV3.DEVICE_DISPLAY_NAME }
            };
        }
    }
}
