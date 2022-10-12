using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Tinkerforge;

namespace Bonsai.Tinkerforge
{
    /// <summary>
    /// Represents an operator that measures current between 0 and 22.5mA
    /// from an Industrial Dual 0-20mA Bricklet.
    /// </summary>
    [DefaultProperty(nameof(Uid))]
    [Description("Measures current from an Industrial Dual 0-20mA Bricklet.")]
    public class IndustrialDual020mAV2 : Combinator<IPConnection, int>
    {
        /// <summary>
        /// Gets or sets the bricklet device UID.
        /// </summary>
        [TypeConverter(typeof(UidConverter))]
        [DeviceType(typeof(BrickletIndustrialPTC))]
        [Description("The bricklet device UID.")]
        public string Uid { get; set; }

        /// <summary>
        /// Gets or sets a value specifying the period between sample event callbacks.
        /// A value of zero disables event reporting.
        /// </summary>
        [Description("Specifies the period between sample event callbacks. A value of zero disables event reporting.")]
        public long Period { get; set; } = 1000;

        /// <summary>
        /// Gets or sets a value specifying the channel to read current from.
        /// </summary>
        [Range(0, 1)]
        [Description("Specifies the channel to read current from.")]
        public byte Channel { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value specifying the behavior of the status LED.
        /// </summary>
        [Description("Specifies the behavior of the status LED.")]
        public IndustrialDual020mAV2StatusLedConfig StatusLed { get; set; } = IndustrialDual020mAV2StatusLedConfig.ShowStatus;

        /// <inheritdoc/>
        public override string ToString()
        {
            return BrickletIndustrialDual020mAV2.DEVICE_DISPLAY_NAME;
        }

        /// <summary>
        /// Measures current from an Industrial Dual 0-20mA Bricklet.
        /// </summary>
        /// <param name="source">
        /// A sequence containing the TCP/IP connection to the Brick Daemon.
        /// </param>
        /// <returns>the 
        /// A sequence of <see cref="int"/> values representing the
        /// current measurements from the Industrial Dual 0-20mA Bricklet
        /// in nA.
        /// </returns>
        public override IObservable<int> Process(IObservable<IPConnection> source)
        {
            if (string.IsNullOrEmpty(Uid))
            {
                throw new ArgumentException("A device Uid must be specified", "Uid");
            }

            return source.SelectStream(connection =>
            {
                var device = new BrickletIndustrialDual020mAV2(Uid, connection);
                connection.Connected += (sender, e) =>
                {
                    device.SetStatusLEDConfig((byte)StatusLed);
                    device.SetCurrentCallbackConfiguration(Channel, Period, false, 'x', 0, 0);
                };

                return Observable.Create<int>(observer =>
                {
                    BrickletIndustrialDual020mAV2.CurrentEventHandler handler = (sender, channel, current) =>
                    {
                        observer.OnNext(current);
                    };

                    device.CurrentCallback += handler;
                    return Disposable.Create(() =>
                    {
                        try { device.SetCurrentCallbackConfiguration(Channel, 0, false, 'x', 0, 0); }
                        catch (NotConnectedException) { }
                        device.CurrentCallback -= handler;
                    });
                });
            });
        }

        /// <summary>
        /// Specifies the behavior of the Industrial Dual 0-20mA Bricklet status LED.
        /// </summary>
        public enum IndustrialDual020mAV2StatusLedConfig : byte
        {
            /// <summary>
            /// The status LED will be permanently OFF.
            /// </summary>
            Off = BrickletIndustrialDual020mAV2.STATUS_LED_CONFIG_OFF,

            /// <summary>
            /// The status LED will be permanently ON as long as the bricklet is powered.
            /// </summary>
            On = BrickletIndustrialDual020mAV2.STATUS_LED_CONFIG_ON,

            /// <summary>
            /// The status LED will change state periodically every second.
            /// </summary>
            ShowHeartbeat = BrickletIndustrialDual020mAV2.STATUS_LED_CONFIG_SHOW_HEARTBEAT,

            /// <summary>
            /// The LED will show communication traffic between Brick and Bricklet,
            /// flickering once for every 10 received data packets.
            /// </summary>
            ShowStatus = BrickletIndustrialDual020mAV2.STATUS_LED_CONFIG_SHOW_STATUS
        }
    }
}
