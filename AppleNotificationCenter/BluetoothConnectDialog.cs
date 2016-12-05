using AppleNotificationCenterService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace AppleNotificationCenter
{
    public partial class BluetoothConnectDialog : Form
    {
        private DeviceWatcher DeviceWatcher;
        private Guid ANCSUuid = new Guid("7905F431-B5CE-4E99-A40F-4B1E122D00D0");

        public NotificationConsumer NotificationConsumer = null;

        public BluetoothConnectDialog()
        {
            InitializeComponent();
        }


        private void StartDeviceWatcher()
        {
            this.RefreshButton.Enabled = false;
            this.RefreshButton.Text = "Stop";

            // Additional properties we would like about the device.
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            string serviceDeviceSelector = GattDeviceService.GetDeviceSelectorFromUuid(this.ANCSUuid);
            this.DeviceWatcher = DeviceInformation.CreateWatcher(serviceDeviceSelector, requestedProperties);

            // Register event handlers before starting the watcher.
            this.DeviceWatcher.Added += DeviceWatcher_Added;
            this.DeviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            this.DeviceWatcher.Stopped += DeviceWatcher_Stopped;
            //deviceWatcher.Added += DeviceWatcher_Added;
            //deviceWatcher.Updated += DeviceWatcher_Updated;
            //deviceWatcher.Removed += DeviceWatcher_Removed;
            //deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            //deviceWatcher.Stopped += DeviceWatcher_Stopped;

            // Clear the devices list view
            this.DevicesListView.BeginUpdate();
            this.DevicesListView.Clear();
            this.DevicesListView.EndUpdate();

            // Start listening
            this.DeviceWatcher.Start();

            // Enable the button
            this.RefreshButton.Enabled = true;
        }

        private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<DeviceInformation>(AddDevice), args);
            }
            else
            {
                this.AddDevice(args);
            }
        }

        private void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(DeviceWatcherEnd));
                return;
            }
            else
            {
                this.DeviceWatcherEnd();
            }
        }

        private void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(DeviceWatcherEnd));
                return;
            }
            else
            {
                this.DeviceWatcherEnd();
            }
        }

        private void AddDevice(DeviceInformation deviceInformation)
        {
            ListViewItem lvItem = new ListViewItem(deviceInformation.Name, 0);
            lvItem.Tag = deviceInformation.Id;

            this.DevicesListView.Items.Add(lvItem);
        }

        private void DeviceWatcherEnd()
        {
            this.RefreshButton.Enabled = false;

            // Unregister event handlers
            this.DeviceWatcher.Added -= DeviceWatcher_Added;
            this.DeviceWatcher.EnumerationCompleted -= DeviceWatcher_EnumerationCompleted;
            this.DeviceWatcher.Stopped -= DeviceWatcher_Stopped;

            this.RefreshButton.Text = "Refresh";
            this.RefreshButton.Enabled = true;
        }

        private void StopDeviceWatcher()
        {
            this.RefreshButton.Enabled = false;

            if (this.DeviceWatcher != null)
            {
                if (this.DeviceWatcher.Status == DeviceWatcherStatus.Started)
                {
                    this.DeviceWatcher.Stop();
                }
            }
            
            // The label change goes last
            this.RefreshButton.Text = "Refresh";
            this.RefreshButton.Enabled = true;
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            if (this.RefreshButton.Text == "Stop")
            {
                this.StopDeviceWatcher();
            }
            else if (this.RefreshButton.Text == "Refresh")
            {
                this.StartDeviceWatcher();
            }
        }

        private async void Connect()
        {
            if (this.DevicesListView.SelectedItems.Count == 1)
            {
                NotificationConsumer nc = null;
                ListViewItem item = this.DevicesListView.SelectedItems[0];
                this.StopDeviceWatcher();
                this.DevicesListView.Enabled = false;
                this.RefreshButton.Enabled = false;
                this.ButtonConnect.Enabled = false;

                string deviceId = (string)item.Tag;

                try
                {
                    BluetoothLEDevice device = await BluetoothLEDevice.FromIdAsync(deviceId);
                    nc = new AppleNotificationCenterService.NotificationConsumer(device);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.DevicesListView.Enabled = true;
                    this.RefreshButton.Enabled = true;
                    this.ButtonConnect.Enabled = true;
                    return;
                }

                if (nc == null)
                {
                    MessageBox.Show("Unable to connect to " + item.Text, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DevicesListView.Enabled = true;
                    this.RefreshButton.Enabled = true;
                    this.ButtonConnect.Enabled = true;
                    return;
                }

                GattCommunicationStatus communicationStatus = GattCommunicationStatus.Unreachable;

                // Try to establlish the connection by subscribing
                try
                {
                    communicationStatus = await nc.SubscribeAsync();
                }
                catch (Exception ex)
                {
                    // TODO: dispose NC
                    MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DevicesListView.Enabled = true;
                    this.RefreshButton.Enabled = true;
                    this.ButtonConnect.Enabled = true;
                    return;
                }

                if (communicationStatus != GattCommunicationStatus.Success)
                {
                    MessageBox.Show("Apple notification center service is unreachable.\n\nHave you opened the application on your iOS device?", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DevicesListView.Enabled = true;
                    this.RefreshButton.Enabled = true;
                    this.ButtonConnect.Enabled = true;
                    return;
                }

                this.NotificationConsumer = nc;
                this.DialogResult = DialogResult.OK;
            }
        }

        private async void DevicesListView_DoubleClick(object sender, EventArgs e)
        {
            if (this.DevicesListView.SelectedItems.Count == 1)
            {
                this.Connect();
            }
        }

        private void BluetoothConnectDialog_Load(object sender, EventArgs e)
        {
            this.StartDeviceWatcher();
        }

        private void ButtonConnect_Click(object sender, EventArgs e)
        {
            if (this.DevicesListView.SelectedItems.Count == 1)
            {
                this.Connect();
            }
            else
            {
                // Something went wrong!
                this.ButtonConnect.Enabled = false;
            }
        }

        private void DevicesListView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (this.DevicesListView.SelectedItems.Count == 1)
            {
                this.ButtonConnect.Enabled = true;
            }
            else
            {
                this.ButtonConnect.Enabled = false;
            }
        }
    }
}
