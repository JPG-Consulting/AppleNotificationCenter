using AppleNotificationCenter.ShellHelpers;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Windows.Foundation;
using Windows.UI.Notifications;

namespace AppleNotificationCenter
{
    [ClassInterface(ClassInterfaceType.None)]
    [ComSourceInterfaces(typeof(INotificationActivationCallback))]
    [Guid("75dc85a4-4aba-4568-aeaf-0c5e8ebd0e53"), ComVisible(true)]
    public class NotificationActivator : INotificationActivationCallback
    {
        public void Activate(string appUserModelId, string invokedArgs, NOTIFICATION_USER_INPUT_DATA[] data, uint dataCount)
        {
            //App.Current.Dispatcher.Invoke(() =>
            //{
            //    (App.Current.MainWindow as MainWindow).ToastActivated();
            //});
        }

        public static void Initialize()
        {
            regService = new RegistrationServices();

            cookie = regService.RegisterTypeForComClients(
                typeof(NotificationActivator),
                RegistrationClassContext.LocalServer,
                RegistrationConnectionType.MultipleUse);
        }
        public static void Uninitialize()
        {
            if (cookie != -1 && regService != null)
            {
                regService.UnregisterTypeForComClients(cookie);
            }
        }

        private static int cookie = -1;
        private static RegistrationServices regService = null;
    }
}