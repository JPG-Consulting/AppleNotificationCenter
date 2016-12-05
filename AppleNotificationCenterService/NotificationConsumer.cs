using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;

namespace AppleNotificationCenterService
{
    public class NotificationConsumer
    {
        public readonly GattDeviceService DeviceService = null;
        public readonly Guid Uuid = new Guid("7905F431-B5CE-4E99-A40F-4B1E122D00D0");

        private NotificationSource _NotificationSource;
        private DataSource _DataSource;
        private ControlPoint _ControlPoint;

        private Dictionary<UInt32, NotificationSourceData> Notifications = new Dictionary<uint, NotificationSourceData>();

        public event TypedEventHandler<NotificationConsumer, NotificationEventArgs> NotificationAdded;
        //public event TypedEventHandler<NotificationSource, NotificationSourceData> NotificationModified;
        public event TypedEventHandler<NotificationConsumer, NotificationSourceData> NotificationRemoved;

        #region Constructors

        public NotificationConsumer(GattDeviceService deviceService)
        {
            if (deviceService.Uuid != this.Uuid)
            {
                throw new Exception("Apple Notification Center Service not found on given device.");
            }

            DeviceService = deviceService;
            LoadBaseCharacteristics();
        }

        public NotificationConsumer(BluetoothLEDevice device)
        {
            try
            {
                DeviceService = device.GetGattService(new Guid("7905F431-B5CE-4E99-A40F-4B1E122D00D0"));
            }
            catch (Exception)
            {
            }

            if (DeviceService == null)
            {
                // TODO: Build a custom exception
                throw new Exception("Apple Notification Center Service not found on given device.");
            }

            LoadBaseCharacteristics();
        }

        #endregion
        
        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public ControlPoint ControlPoint
        {
            get { return _ControlPoint; }
        }

        /// <summary>
        /// 
        /// </summary>
        public DataSource DataSource
        {
            get { return _DataSource; }
        }

        /// <summary>
        /// 
        /// </summary>
        public NotificationSource NotificationSource
        {
            get { return _NotificationSource; }
        }

        #endregion

        /// <summary>
        /// Static method to make it easier to create this class form a DeviceInformation object or any other
        /// object containing the devide ID.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public static async Task<NotificationConsumer> FromDeviceIdAsync(string deviceId)
        {
            BluetoothLEDevice device;

            try
            {
                device = await BluetoothLEDevice.FromIdAsync(deviceId);
            }
            catch (Exception e)
            {
                throw e;
            }

            return new NotificationConsumer(device);
        }

        private void LoadBaseCharacteristics()
        {
            Guid notificationSourceUuid = new Guid("9FBF120D-6301-42D9-8C58-25E699A21DBD");
            Guid controlPointUuid = new Guid("69D1D8F3-45E1-49A8-9821-9BBDFDAAD9D9");
            Guid dataSourceUuid = new Guid("22EAC6E9-24D6-4BB5-BE44-B36ACE7C7BFB");

            IReadOnlyList<GattCharacteristic> allCharacteristics = DeviceService.GetAllCharacteristics();
            foreach (GattCharacteristic c in allCharacteristics)
            {

                if (c.Uuid == controlPointUuid)
                {
                    try
                    {
                        _ControlPoint = new ControlPoint(c);
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
                else if (c.Uuid == dataSourceUuid)
                {
                    try
                    {
                        _DataSource = new DataSource(c);
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
                else if (c.Uuid == notificationSourceUuid)
                {
                    try
                    {
                        _NotificationSource = new NotificationSource(c);
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
            }

            if (_NotificationSource == null)
            {
                //    throw new AppleNotificationCenterServiceCharacteristicNotFound("Missing notification source characteristic");
                throw new Exception("Missing notification source characteristic");
            }
            else if (_ControlPoint == null)
            {
                //    throw new AppleNotificationCenterServiceCharacteristicNotFound("Missing control point characteristic");
                throw new Exception("Missing control point characteristic");
            }
            else if (_DataSource == null)
            {
                //    throw new AppleNotificationCenterServiceCharacteristicNotFound("Missing data source characteristic");
                throw new Exception("Missing data source characteristic");
            }

            // Add watcher for connection status changes
            //DeviceService.Device.ConnectionStatusChanged += Device_ConnectionStatusChanged;
        }

        /// <summary>
        /// Subscribe to the Apple Notification Center Service Caharacteristics
        /// </summary>
        /// <returns></returns>
        public async Task<GattCommunicationStatus> SubscribeAsync()
        {
            GattCommunicationStatus communicationStatus = GattCommunicationStatus.Unreachable;

            try
            {
                communicationStatus = await _NotificationSource.SubscribeAsync();
                if (communicationStatus == GattCommunicationStatus.Success)
                {
                    communicationStatus = await _DataSource.SubscribeAsync();
                }
            }
            catch (Exception e)
            {
                //if (_NotificationSource.Subscribed)
                //{
                //    // TODO: Unsubscribe
                //}
                throw e;
            }

            if (communicationStatus == GattCommunicationStatus.Success)
            {
                // Add events
                this._NotificationSource.NotificationAdded += _NotificationSource_NotificationAdded;
                this._NotificationSource.NotificationRemoved += _NotificationSource_NotificationRemoved;
                this._DataSource.NotificationAttributesReceived += _DataSource_NotificationAttributesReceived;
            }

            return communicationStatus;
        }
        
        /// <summary>
        /// Unsubscribe to the Apple Notification Center Service Caharacteristics
        /// </summary>
        /// <returns></returns>
        public async Task<GattCommunicationStatus> UnsubscribeAsync()
        {
            GattCommunicationStatus communicationStatus = GattCommunicationStatus.Unreachable;

            try
            {
                communicationStatus = await _NotificationSource.UnsubscribeAsync();
                if (communicationStatus == GattCommunicationStatus.Success)
                {
                    communicationStatus = await _DataSource.UnsubscribeAsync();
                }
            }
            catch (Exception e)
            {
                //if (_NotificationSource.Subscribed)
                //{
                //    // TODO: Unsubscribe
                //}
                throw e;
            }

            if (communicationStatus == GattCommunicationStatus.Success)
            {
                // Add events
                this._NotificationSource.NotificationAdded -= _NotificationSource_NotificationAdded;
                this._NotificationSource.NotificationRemoved -= _NotificationSource_NotificationRemoved;
                this._DataSource.NotificationAttributesReceived -= _DataSource_NotificationAttributesReceived;
            }

            return communicationStatus;
        }

        private void _NotificationSource_NotificationRemoved(NotificationSource sender, NotificationSourceData args)
        {
            if (sender == _NotificationSource)
            {
                if (Notifications.ContainsKey(args.NotificationUID))
                {
                    Notifications.Remove(args.NotificationUID);
                }

                this.NotificationRemoved?.Invoke(this, args);
            }
        }

        private async void _NotificationSource_NotificationAdded(NotificationSource sender, NotificationSourceData args)
        {
            if (sender == _NotificationSource)
            {
                if (!Notifications.ContainsKey(args.NotificationUID))
                {
                    Notifications.Add(args.NotificationUID, args);
                }
                else
                {
                    Notifications[args.NotificationUID] = args;
                }

                // We don't trigger the notification added event as we need to query for
                // the notification data.

                // Build the attributes list for the GetNotificationAttributtes command.   
                List<NotificationAttributeID> attributes = new List<NotificationAttributeID>();
                attributes.Add(NotificationAttributeID.AppIdentifier);
                attributes.Add(NotificationAttributeID.Title);
                attributes.Add(NotificationAttributeID.Message);

                if (args.EventFlags.HasFlag(EventFlags.EventFlagPositiveAction))
                {
                    attributes.Add(NotificationAttributeID.PositiveActionLabel);
                }

                if (args.EventFlags.HasFlag(EventFlags.EventFlagNegativeAction))
                {
                    attributes.Add(NotificationAttributeID.NegativeActionLabel);
                }

                try
                {
                    GattCommunicationStatus communicationStatus = await ControlPoint.GetNotificationAttributesAsync(args.NotificationUID, attributes);
                }
                catch (Exception e)
                {
                    // Simply log the exception to output console
                    //System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
        }

        private void _DataSource_NotificationAttributesReceived(NotificationAttributeCollection obj)
        {
            // Is it a known notification?
            if (this.Notifications.ContainsKey(obj.NotificationUID) == false)
            {
                return;
            }

            //ApplicationAttributeCollection applicationAttributes;

            //if (attributes.ContainsKey(NotificationAttributeID.AppIdentifier))
            //{
            //    string appIdentifier = attributes[NotificationAttributeID.AppIdentifier];

            //    if (Applications.ContainsKey(appIdentifier) == false)
            //    {
            //        // Enque notifications
            //        if (ApplicationNotificationQueue.ContainsKey(appIdentifier) == false)
            //        {
            //            ApplicationNotificationQueue.Add(appIdentifier, new Queue<NotificationAttributeCollection>());
            //        }
            //        ApplicationNotificationQueue[appIdentifier].Enqueue(attributes);

            //        List<AppAttributeID> requestAppAttributes = new List<AppAttributeID>();
            //        requestAppAttributes.Add(AppAttributeID.DisplayName);

            //        try
            //        {
            //            var commStatus = await ControlPoint.GetAppAttributesAsync(attributes[NotificationAttributeID.AppIdentifier], requestAppAttributes);
            //        }
            //        catch (Exception e)
            //        {
            //            System.Diagnostics.Debug.WriteLine("Bad get app attributes request");
            //        }
            //        return;
            //    }

            //    applicationAttributes = Applications[appIdentifier];
            //}

            //RaiseNotificationEvent(attributes);
            this.NotificationAdded(this, new NotificationEventArgs(this.Notifications[obj.NotificationUID], obj));
        }


    }
}
