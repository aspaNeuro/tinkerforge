﻿using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Tinkerforge;
using System.Xml.Serialization;

namespace Bonsai.Tinkerforge
{
    [Combinator]
    [DefaultProperty(nameof(Device))]
    [Description("Measures CO2 concentration, in ppm, temperature, and humidity from a CO2 Bricklet 2.0.")]
    public class BrickletCO2V2
    {
        [Description("Device data including address UID.")]
        [TypeConverter(typeof(BrickletDeviceNameConverter))]
        public TinkerforgeHelpers.DeviceData Device { get; set; }

        [Description("Specifies the period between sample event callbacks. A value of zero disables event reporting.")]
        public long Period { get; set; } = 1000;

        [Description("Specifies the ambient air pressure. Can be used to increase the accuracy of the CO2 sensor.")]
        public int AirPressure { get; set; }

        [Description("Specifies a temperature offset, in hundredths of a degree, to compensate for heat inside an enclosure.")]
        public int TemperatureOffset { get; set; }

        [Description("Specifies the behavior of the status LED.")]
        public CO2V2StatusLedConfig StatusLed { get; set; } = CO2V2StatusLedConfig.ShowStatus;

        public IObservable<DataFrame> Process(IObservable<IPConnection> source)
        {
            return source.SelectStream(connection =>
            {
                var device = new global::Tinkerforge.BrickletCO2V2(Device.UID, connection);
                connection.Connected += (sender, e) =>
                {
                    device.SetStatusLEDConfig((byte)StatusLed);
                    device.SetAirPressure(AirPressure);
                    device.SetTemperatureOffset(TemperatureOffset);
                    device.SetAllValuesCallbackConfiguration(Period, false);
                };

                return Observable.Create<DataFrame>(observer =>
                {
                    global::Tinkerforge.BrickletCO2V2.AllValuesEventHandler handler = (sender, co2Concentration, temperature, humidity) =>
                    {
                        observer.OnNext(new DataFrame(co2Concentration, temperature, humidity));
                    };

                    device.AllValuesCallback += handler;
                    return Disposable.Create(() =>
                    {
                        try { device.SetAllValuesCallbackConfiguration(0, false); }
                        catch (NotConnectedException) { } // best effort
                        device.AllValuesCallback -= handler;
                    });
                });
            });
        }

        public struct DataFrame
        {
            public int Co2Concentration;
            public short Temperature;
            public int Humidity;

            public DataFrame(int co2Concentration, short temperature, int humidity)
            {
                Co2Concentration = co2Concentration;
                Temperature = temperature;
                Humidity = humidity;
            }
        }

        public enum CO2V2StatusLedConfig : byte
        {
            Off = global::Tinkerforge.BrickletCO2V2.STATUS_LED_CONFIG_OFF,
            On = global::Tinkerforge.BrickletCO2V2.STATUS_LED_CONFIG_ON,
            ShowHeartbeat = global::Tinkerforge.BrickletCO2V2.STATUS_LED_CONFIG_SHOW_HEARTBEAT,
            ShowStatus = global::Tinkerforge.BrickletCO2V2.STATUS_LED_CONFIG_SHOW_STATUS
        }
    }
}
