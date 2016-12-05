using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;

namespace AppleNotificationCenterService
{
    public sealed class NotificationSource
    {
        public readonly GattCharacteristic Characteristic;

        public event TypedEventHandler<NotificationSource, NotificationSourceData> NotificationAdded;
        public event TypedEventHandler<NotificationSource, NotificationSourceData> NotificationModified;
        public event TypedEventHandler<NotificationSource, NotificationSourceData> NotificationRemoved;

        public NotificationSource(GattCharacteristic characteristic)
        {
            this.Characteristic = characteristic;
        }

        public async Task<GattCommunicationStatus> SubscribeAsync()
        {
            GattCommunicationStatus communicationStatus = GattCommunicationStatus.Unreachable;

            try
            {
                communicationStatus = await Characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
            }
            catch (Exception e)
            {
                throw e;
            }

            if (communicationStatus == GattCommunicationStatus.Success)
            {
                this.Characteristic.ValueChanged += Characteristic_ValueChanged;
            }

            return communicationStatus;
        }

        /// <summary>
        /// Unsubscribe from the notification source.
        /// </summary>
        /// <returns></returns>
        public async Task<GattCommunicationStatus> UnsubscribeAsync()
        {
            GattCommunicationStatus communicationStatus = GattCommunicationStatus.Unreachable;

            try
            {
                communicationStatus = await Characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
            }
            catch (Exception e)
            {
                throw e;
            }

            return communicationStatus;
        }


        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (sender == this.Characteristic)
            {
                var valueBytes = WindowsRuntimeBufferExtensions.ToArray(args.CharacteristicValue);

                // Convert the bytes array to the structure
                GCHandle pinnedPacket = GCHandle.Alloc(valueBytes, GCHandleType.Pinned);
                var msg = Marshal.PtrToStructure<NotificationSourceData>(pinnedPacket.AddrOfPinnedObject());
                pinnedPacket.Free();

                //We dont care about old notifications
                if (msg.EventFlags.HasFlag(EventFlags.EventFlagPreExisting))
                {
                    return;
                }

                switch (msg.EventId)
                {
                    case EventID.NotificationAdded:
                        this.NotificationAdded?.Invoke(this, msg);
                        break;
                    case EventID.NotificationModified:
                        this.NotificationModified?.Invoke(this, msg);
                        break;
                    case EventID.NotificationRemoved:
                        this.NotificationRemoved?.Invoke(this, msg);
                        break;
                }
            }
        }
    }
}