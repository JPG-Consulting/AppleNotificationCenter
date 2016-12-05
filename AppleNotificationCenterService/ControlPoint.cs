using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace AppleNotificationCenterService
{
    public sealed class ControlPoint
    {
        private GattCharacteristic Characteristic;

        public ControlPoint(GattCharacteristic characteristic)
        {
            this.Characteristic = characteristic;
        }

        private async Task<GattCommunicationStatus> WriteValueAsync(IBuffer value)
        {
            // Send the command
            byte[] debugValue = value.ToArray();

            try
            {
                if (this.Characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.ReliableWrites))
                {
                    GattReliableWriteTransaction transaction = new GattReliableWriteTransaction();
                    transaction.WriteValue(this.Characteristic, value);
                    return await transaction.CommitAsync();
                }
                else
                {
                    return await this.Characteristic.WriteValueAsync(value, GattWriteOption.WriteWithResponse);
                }
            }
            catch (Exception e)
            {
                //switch ((uint)e.HResult)
                //{
                //    case 0xE04200A0:
                //        //System.Diagnostics.Debug.WriteLine("Unknown command. The commandID was not recognized by the NP.");
                //        throw new UnknownCommandException();
                //    case 0xE04200A1:
                //        //System.Diagnostics.Debug.WriteLine("Invalid command. The command was improperly formatted.");
                //        throw new InvalidCommandException();
                //    case 0xE04200A2:
                //        //System.Diagnostics.Debug.WriteLine("Invalid parameter. One of the parameters (for example, the NotificationUID) does not refer to an existing object on the NP.");
                //        throw new InvalidParameterException();
                //    case 0xE04200A3:
                //        //System.Diagnostics.Debug.WriteLine("Action failed. The action was not performed.");
                //        throw new ActionFailedException();
                //    default:
                //        System.Diagnostics.Debug.WriteLine("Failed to get notification attributes. " + e.Message);
                //        break;
                //}

                throw e;
            }
        }

        /// <summary>
        /// The Get Notification Attributes command allows an NC to retrieve the attributes of a specific iOS notification.
        /// </summary>
        /// <param name="notificationUID">The 32-bit numerical value representing the UID of the iOS notification for which the client wants information.</param>
        /// <param name="attributeIDs"></param>
        /// <returns></returns>
        public async Task<GattCommunicationStatus> GetNotificationAttributesAsync(UInt32 notificationUID, List<NotificationAttributeID> attributeIDs)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            writer.Write((byte)CommandID.GetNotificationAttributes);
            writer.Write(notificationUID);

            foreach (NotificationAttributeID attrID in attributeIDs)
            {
                writer.Write((byte)attrID);

                // Some attributes need to be followed by a 2-bytes max length parameter
                switch (attrID)
                {
                    case NotificationAttributeID.Message:
                        // Max length for title
                        writer.Write((UInt16)128);
                        break;
                    case NotificationAttributeID.Subtitle:
                        // Max length for title
                        writer.Write((UInt16)64);
                        break;
                    case NotificationAttributeID.Title:
                        // Max length for title
                        writer.Write((UInt16)64);
                        break;
                }
            }

            byte[] bytes = stream.ToArray();

            // Send the command
            try
            {
                return await WriteValueAsync(bytes.AsBuffer());
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Starting with iOS 8.0, the NP can inform the NC of potential actions that are associated with iOS notifications. On the user’s behalf, the NC can then request the NP to perform an action associated with a specific iOS notification.
        /// 
        /// The NC is informed of the existence of performable actions on an iOS notification by detecting the presence of set flags in the EventFlags field of the GATT notifications generated by the Notification Source characteristic
        /// </summary>
        /// <param name="notificationUID">A 32-bit numerical value that is the unique identifier (UID) for the iOS notification on which to perform the action.</param>
        /// <param name="actionID">The action identifier.</param>
        /// <returns></returns>
        public async Task<GattCommunicationStatus> PerformNotificationActionAsync(UInt32 notificationUID, ActionID actionID)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            writer.Write((byte)CommandID.PerformNotificationAction);
            writer.Write(notificationUID);
            writer.Write((byte)actionID);

            byte[] bytes = stream.ToArray();

            try
            {
                return await WriteValueAsync(bytes.AsBuffer());
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}