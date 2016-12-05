using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;

namespace AppleNotificationCenterService
{
    public sealed class AppleNotificationCenterDeviceWatcher
    {
        private DeviceWatcher DeviceWatcher;

        private Guid ANCSUuid = new Guid("7905F431-B5CE-4E99-A40F-4B1E122D00D0");

        private Dictionary<string, DeviceInformation> Devices;

        public readonly bool IgnoreUnpaired = false;

        public event TypedEventHandler<AppleNotificationCenterDeviceWatcher, DeviceInformation> Added;
        public event TypedEventHandler<AppleNotificationCenterDeviceWatcher, IReadOnlyList<DeviceInformation>> EnumerationCompleted;
        public event TypedEventHandler<AppleNotificationCenterDeviceWatcher, object> Stopped;
        public event TypedEventHandler<AppleNotificationCenterDeviceWatcher, DeviceInformationUpdate> Updated;
        public event TypedEventHandler<AppleNotificationCenterDeviceWatcher, DeviceInformationUpdate> Removed;

        public AppleNotificationCenterDeviceWatcher()
        {
            this.Devices = new Dictionary<string, DeviceInformation>();
        }

        ~AppleNotificationCenterDeviceWatcher()
        {
            this.Stop();
        }

        public void Start()
        {
            //Find a device that is advertising the ancs service uuid

            // Request additional properties
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            string serviceDeviceSelector = GattDeviceService.GetDeviceSelectorFromUuid(this.ANCSUuid);
            this.DeviceWatcher = DeviceInformation.CreateWatcher(serviceDeviceSelector, requestedProperties);

            // Register event handlers before starting the watcher.
            this.DeviceWatcher.Added += DeviceWatcher_Added;
            this.DeviceWatcher.Updated += DeviceWatcher_Updated;
            this.DeviceWatcher.Removed += DeviceWatcher_Removed;
            this.DeviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            this.DeviceWatcher.Stopped += DeviceWatcher_Stopped;

            this.DeviceWatcher.Start();
        }

        public void Stop()
        {
            if (this.DeviceWatcher == null)
            {
                // Request additional properties
                string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

                string serviceDeviceSelector = GattDeviceService.GetDeviceSelectorFromUuid(this.ANCSUuid);
                this.DeviceWatcher = DeviceInformation.CreateWatcher(serviceDeviceSelector, requestedProperties);
            }

            if (this.DeviceWatcher != null)
            {
                // Unregister the event handlers.
                this.DeviceWatcher.Added -= DeviceWatcher_Added;
                this.DeviceWatcher.Updated -= DeviceWatcher_Updated;
                this.DeviceWatcher.Removed -= DeviceWatcher_Removed;
                this.DeviceWatcher.EnumerationCompleted -= DeviceWatcher_EnumerationCompleted;
                this.DeviceWatcher.Stopped -= DeviceWatcher_Stopped;

                // Stop the watcher.
                if (DeviceWatcher.Status == DeviceWatcherStatus.Started)
                {
                    this.DeviceWatcher.Stop();
                }
                DeviceWatcher = null;
            }

            // Clear the devices list
            Devices.Clear();
        }

        private void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            if (sender == this.DeviceWatcher)
            {
                if (DeviceWatcher != null)
                {
                    // Unregister the event handlers.
                    DeviceWatcher.Added -= DeviceWatcher_Added;
                    DeviceWatcher.Updated -= DeviceWatcher_Updated;
                    DeviceWatcher.Removed -= DeviceWatcher_Removed;
                    DeviceWatcher.EnumerationCompleted -= DeviceWatcher_EnumerationCompleted;
                    DeviceWatcher.Stopped -= DeviceWatcher_Stopped;

                    DeviceWatcher = null;
                }
                
                // Clear the devices list
                Devices.Clear();

                // event
                Stopped?.Invoke(this, args);
            }
        }

        private void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            if (sender == this.DeviceWatcher)
            {
                // Get a list of paired devices
                EnumerationCompleted?.Invoke(this, Devices.Values.ToList());
            }
        }

        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate updateInfo)
        {
            if (sender == this.DeviceWatcher)
            {
                if (this.Devices.ContainsKey(updateInfo.Id))
                {
                    Devices.Remove(updateInfo.Id);
                    Removed?.Invoke(this, updateInfo);
                }
            }
        }

        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate updateInfo)
        {
            if (sender == this.DeviceWatcher)
            {
                if (this.Devices.ContainsKey(updateInfo.Id))
                {
                    Devices[updateInfo.Id].Update(updateInfo);
                    Updated?.Invoke(this, updateInfo);
                }
            }
        }

        private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInformation)
        {
            // Protect against race condition if the task runs after the app stopped the deviceWatcher.
            if (sender == DeviceWatcher)
            {
                if (!this.Devices.ContainsKey(deviceInformation.Id))
                {
                    this.Devices.Add(deviceInformation.Id, deviceInformation);
                    Added?.Invoke(this, deviceInformation);
                }
            }
        }
    }
















}
