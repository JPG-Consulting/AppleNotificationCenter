using System;
using System.Runtime.InteropServices;

namespace AppleNotificationCenterService
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NotificationSourceData
    {
        public EventID EventId;
        public EventFlags EventFlags;
        public CategoryID CategoryId;
        public byte CategoryCount;

        public UInt32 NotificationUID;
    }
}