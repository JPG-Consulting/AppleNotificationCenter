using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppleNotificationCenterService
{
    public sealed class NotificationEventArgs
    {
        public readonly NotificationSourceData NotificationSource;
        public readonly NotificationAttributeCollection NotificationAttributes;

        public NotificationEventArgs(NotificationSourceData notificationSource, NotificationAttributeCollection attributes)
        {
            this.NotificationSource = notificationSource;
            this.NotificationAttributes = attributes;
        }

    }
}
