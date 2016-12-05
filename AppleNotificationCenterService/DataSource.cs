using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace AppleNotificationCenterService
{
    public sealed class DataSource
    {
        public readonly GattCharacteristic Characteristic;
        private bool isValueChangedHandlerRegistered = false;

        public event Action<NotificationAttributeCollection> NotificationAttributesReceived;
        //public event Action<ApplicationAttributeCollection> ApplicationAttributesReceived;

        public DataSource(GattCharacteristic c)
        {
            this.Characteristic = c;
        }

        private void AddValueChangedHandler()
        {
            if (!isValueChangedHandlerRegistered)
            {
                this.Characteristic.ValueChanged += GattCharacteristic_ValueChanged;
                isValueChangedHandlerRegistered = true;
            }
        }

        private void RemoveValueChangedHandler()
        {
            if (isValueChangedHandlerRegistered)
            {
                this.Characteristic.ValueChanged -= GattCharacteristic_ValueChanged;
                isValueChangedHandlerRegistered = false;
            }
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
                AddValueChangedHandler();
            }
            
            return communicationStatus;
        }

        public async Task<GattCommunicationStatus> UnsubscribeAsync()
        {
            GattCommunicationStatus communicationStatus = GattCommunicationStatus.Unreachable;

            RemoveValueChangedHandler();

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

        private void GattCharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            CommandID commandID = (CommandID)args.CharacteristicValue.GetByte(0);

            switch (commandID)
            {
                case CommandID.GetAppAttributes:
                    //ApplicationAttributesReceived?.Invoke(new ApplicationAttributeCollection(args.CharacteristicValue));
                    break;
                case CommandID.GetNotificationAttributes:
                    NotificationAttributesReceived?.Invoke(new NotificationAttributeCollection(args.CharacteristicValue));
                    break;
                case CommandID.PerformNotificationAction:
                    break;
            }
        }
    }
}