using AppleNotificationCenter.ShellHelpers;
using AppleNotificationCenterService;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Windows.Data.Xml.Dom;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.UI.Notifications;

namespace AppleNotificationCenter
{
    public class TaskTrayApplicationContext : ApplicationContext
    {
        NotifyIcon notifyIcon = new NotifyIcon();

        ApplicationsForm ApplicationsForm = new ApplicationsForm();
        BluetoothConnectDialog BluetoothConnectDialog = null;

        NotificationConsumer NotificationConsumer = null;

        private const String APP_ID = "Apple Notification Center";

        // Menu Items
        private MenuItem ConnectMenuItem;
        private MenuItem DisconnectMenuItem;

        public TaskTrayApplicationContext()
        {
            RegisterAppForNotificationSupport();
            NotificationActivator.Initialize();

            // Common menu items
            //MenuItem configMenuItem = new MenuItem("Configuration", new EventHandler(ShowConfig));
            //MenuItem exitMenuItem = new MenuItem("Exit", new EventHandler(Exit));
            MenuItem exitMenuItem = new MenuItem("Exit", new EventHandler(Exit));

            // Dynamic Menu Items
            MenuItem ConnectionSeparatorMenuItem = new MenuItem("-");
            this.ConnectMenuItem = new MenuItem("Connect", new EventHandler(ConnectMenuItem_Click));
            this.DisconnectMenuItem = new MenuItem("Disconnect", new EventHandler(Disconnect));
            this.DisconnectMenuItem.Visible = false;

            notifyIcon.Icon = AppleNotificationCenter.Properties.Resources.AppIcon;
            //notifyIcon.DoubleClick += new EventHandler(ShowMessage);
            notifyIcon.ContextMenu = new ContextMenu(new MenuItem[] {ConnectMenuItem, DisconnectMenuItem, ConnectionSeparatorMenuItem, exitMenuItem });
            notifyIcon.Text = "Apple Notification Center";
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            notifyIcon.Visible = true;

            // Display the connection window right away
            this.BluetoothConnectDialog = new BluetoothConnectDialog();
            DialogResult result = this.BluetoothConnectDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                this.SetNotificationConsumer(this.BluetoothConnectDialog.NotificationConsumer);
                this.BluetoothConnectDialog.Dispose();
                this.BluetoothConnectDialog = null;

                // Minimal Toast notification telling the user we are connected
                // Get a toast XML template
                XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);

                // Fill in the text elements
                XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
                stringElements[0].AppendChild(toastXml.CreateTextNode("Connected"));
                stringElements[1].AppendChild(toastXml.CreateTextNode("You are now connected to " + this.NotificationConsumer.DeviceService.Device.DeviceInformation.Name));

                ToastNotification toastNotification = new ToastNotification(toastXml);
                toastNotification.ExpirationTime = DateTime.Now.AddSeconds(5);
                toastNotification.Tag = "connected";
                toastNotification.Group = "-1";

                ToastNotificationManager.CreateToastNotifier(APP_ID).Show(toastNotification);
            }
            //else
            //{
            //    MessageBox.Show("Not yet.");
            //}
        }

        #region Register COM for Windows toast notifications

        // In order to display toasts, a desktop application must have a shortcut on the Start menu.
        // Also, an AppUserModelID must be set on that shortcut.
        //
        // For the app to be activated from Action Center, it needs to register a COM server with the OS
        // and register the CLSID of that COM server on the shortcut.
        //
        // The shortcut should be created as part of the installer. The following code shows how to create
        // a shortcut and assign the AppUserModelID and ToastActivatorCLSID properties using Windows APIs.
        //
        // Included in this project is a wxs file that be used with the WiX toolkit
        // to make an installer that creates the necessary shortcut. One or the other should be used.
        //
        // This sample doesn't clean up the shortcut or COM registration.

        private void RegisterAppForNotificationSupport()
        {
            String shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Microsoft\\Windows\\Start Menu\\Programs\\Apple Notification Center.lnk";
            if (!File.Exists(shortcutPath))
            {
                // Find the path to the current executable
                String exePath = Process.GetCurrentProcess().MainModule.FileName;
                InstallShortcut(shortcutPath, exePath);
                RegisterComServer(exePath);
            }
        }

        private void InstallShortcut(String shortcutPath, String exePath)
        {
            IShellLinkW newShortcut = (IShellLinkW)new CShellLink();

            // Create a shortcut to the exe
            newShortcut.SetPath(exePath);

            // Open the shortcut property store, set the AppUserModelId property
            IPropertyStore newShortcutProperties = (IPropertyStore)newShortcut;

            PropVariantHelper varAppId = new PropVariantHelper();
            varAppId.SetValue(APP_ID);
            newShortcutProperties.SetValue(PROPERTYKEY.AppUserModel_ID, varAppId.Propvariant);

            PropVariantHelper varToastId = new PropVariantHelper();
            varToastId.VarType = VarEnum.VT_CLSID;
            varToastId.SetValue(typeof(NotificationActivator).GUID);

            newShortcutProperties.SetValue(PROPERTYKEY.AppUserModel_ToastActivatorCLSID, varToastId.Propvariant);

            // Commit the shortcut to disk
            ShellHelpers.IPersistFile newShortcutSave = (ShellHelpers.IPersistFile)newShortcut;

            newShortcutSave.Save(shortcutPath, true);
        }

        private void RegisterComServer(String exePath)
        {
            // We register the app process itself to start up when the notification is activated, but
            // other options like launching a background process instead that then decides to launch
            // the UI as needed.
            string regString = String.Format("SOFTWARE\\Classes\\CLSID\\{{{0}}}\\LocalServer32", typeof(NotificationActivator).GUID);
            var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(regString);
            key.SetValue(null, exePath);
        }

        #endregion

        private async void ConnectMenuItem_Click(object sender, EventArgs e)
        {
            if (this.NotificationConsumer == null)
            {
                if (this.BluetoothConnectDialog == null)
                {
                    this.BluetoothConnectDialog = new BluetoothConnectDialog();
                }

                if (this.BluetoothConnectDialog.Visible)
                {
                    this.BluetoothConnectDialog.Activate();
                    this.BluetoothConnectDialog.Focus();
                }
                else
                {
                    this.BluetoothConnectDialog.ShowDialog();
                }
            }
            else
            {
                GattCommunicationStatus communicationStatus = GattCommunicationStatus.Unreachable;

                try
                {
                    communicationStatus = await this.NotificationConsumer.SubscribeAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (communicationStatus == GattCommunicationStatus.Success)
                {
                    this.ConnectMenuItem.Visible = false;
                    this.DisconnectMenuItem.Visible = true;

                    // Minimal Toast notification telling the user we are connected
                    // Get a toast XML template
                    XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);

                    // Fill in the text elements
                    XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
                    stringElements[0].AppendChild(toastXml.CreateTextNode("Connected"));
                    stringElements[1].AppendChild(toastXml.CreateTextNode("You are now connected to " + this.NotificationConsumer.DeviceService.Device.DeviceInformation.Name));

                    ToastNotification toastNotification = new ToastNotification(toastXml);
                    toastNotification.ExpirationTime = DateTime.Now.AddSeconds(5);
                    toastNotification.Tag = "connected";
                    toastNotification.Group = "-1";

                    ToastNotificationManager.CreateToastNotifier(APP_ID).Show(toastNotification);
                }
                else
                {
                    MessageBox.Show("Failed to connect", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void Disconnect(object sender, EventArgs e)
        {
            GattCommunicationStatus communicationStatus = GattCommunicationStatus.Unreachable;

            try
            {
                communicationStatus = await this.NotificationConsumer.UnsubscribeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (communicationStatus == GattCommunicationStatus.Success)
            {
                this.ConnectMenuItem.Visible = true;
                this.DisconnectMenuItem.Visible = false;

                // Minimal Toast notification telling the user we are connected
                // Get a toast XML template
                XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);

                // Fill in the text elements
                XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
                stringElements[0].AppendChild(toastXml.CreateTextNode("Disconnected"));
                stringElements[1].AppendChild(toastXml.CreateTextNode("You are now disconnected from " + this.NotificationConsumer.DeviceService.Device.DeviceInformation.Name));

                ToastNotification toastNotification = new ToastNotification(toastXml);
                toastNotification.ExpirationTime = DateTime.Now.AddSeconds(5);
                toastNotification.Tag = "disconnected";
                toastNotification.Group = "-1";

                ToastNotificationManager.CreateToastNotifier(APP_ID).Show(toastNotification);
            }
            else
            {
                MessageBox.Show("Failed to Disconnect", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            if (this.NotificationConsumer == null)
            {
                if (this.BluetoothConnectDialog == null)
                {
                    this.BluetoothConnectDialog = new BluetoothConnectDialog();
                }

                if (this.BluetoothConnectDialog.Visible)
                {
                    this.BluetoothConnectDialog.Activate();
                    this.BluetoothConnectDialog.Focus();
                }
                else
                {
                    this.BluetoothConnectDialog.ShowDialog();
                }
            }
            else
            {
                MessageBox.Show("The configuration has not been implemented yet.", "Apple Notification Center", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;

                if (this.ApplicationsForm.Visible)
                {
                    ApplicationsForm.Focus();
                }
                else
                {
                    ApplicationsForm.ShowDialog();
                }
            }
        }

        private void SetNotificationConsumer(NotificationConsumer notificationConsumer)
        {
            this.NotificationConsumer = notificationConsumer;

            // Register event handlers
            this.NotificationConsumer.NotificationAdded += NotificationConsumer_NotificationAdded;
            this.NotificationConsumer.NotificationRemoved += NotificationConsumer_NotificationRemoved;

            if (this.NotificationConsumer.DeviceService.Device.ConnectionStatus == Windows.Devices.Bluetooth.BluetoothConnectionStatus.Connected)
            {
                this.ConnectMenuItem.Visible = false;
                this.DisconnectMenuItem.Visible = true;
            }
            else
            {
                this.ConnectMenuItem.Visible = true;
                this.DisconnectMenuItem.Visible = false;
            }
        }

        private void NotificationConsumer_NotificationRemoved(NotificationConsumer sender, NotificationSourceData args)
        {
            try
            {
                ToastNotificationManager.History.Remove(args.NotificationUID.ToString(), args.CategoryId.ToString(), "Apple Notification Center");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void NotificationConsumer_NotificationAdded(NotificationConsumer sender, NotificationEventArgs args)
        {
            XmlDocument toastXml;
            AppleToastNotification toastBuilder = new AppleToastNotification(args.NotificationSource, args.NotificationAttributes);
            toastXml = toastBuilder.GetXml();

            ToastNotification toastNotification = new ToastNotification(toastXml);

            // Data used to remove the notification if requested
            toastNotification.Tag = args.NotificationSource.NotificationUID.ToString();
            toastNotification.Group = args.NotificationSource.CategoryId.ToString();
            toastNotification.ExpirationTime = DateTime.Now.AddMinutes(30);

            toastNotification.Activated += Toast_Activated;
            toastNotification.Dismissed += Toast_Dismissed;
            toastNotification.Failed += Toast_Failed;
            
            ToastNotificationManager.CreateToastNotifier(APP_ID).Show(toastNotification);
        }

        private void Toast_Failed(ToastNotification sender, ToastFailedEventArgs args)
        {
            //MessageBox.Show("Toast failed");
        }

        private void Toast_Dismissed(ToastNotification sender, ToastDismissedEventArgs args)
        {
            //String outputText = "";
            //switch (args.Reason)
            //{
            //    case ToastDismissalReason.ApplicationHidden:
            //        outputText = "The app hid the toast using ToastNotifier.Hide";
            //        break;
            //    case ToastDismissalReason.UserCanceled:
            //        outputText = "The user dismissed the toast";
            //        break;
            //    case ToastDismissalReason.TimedOut:
            //        outputText = "The toast has timed out";
            //        break;
            //}

            //MessageBox.Show(outputText);
        }

        private async void Toast_Activated(ToastNotification sender, object args)
        {
            // Handle toast activation
            if (args is ToastActivatedEventArgs)
            {
                ToastActivatedEventArgs tArg = (ToastActivatedEventArgs)args;

                // Process the action
                if (tArg.Arguments == "positive")
                {
                    await this.NotificationConsumer.ControlPoint.PerformNotificationActionAsync(Convert.ToUInt32(sender.Tag), ActionID.Positive);
                }
                else if (tArg.Arguments == "negative")
                {
                    await this.NotificationConsumer.ControlPoint.PerformNotificationActionAsync(Convert.ToUInt32(sender.Tag), ActionID.Negative);
                }
            }
            
        }

        void Exit(object sender, EventArgs e)
        {
            // We must manually tidy up and remove the icon before we exit.
            // Otherwise it will be left behind until the user mouses over.
            notifyIcon.Visible = false;

            Application.Exit();
        }

    }
}