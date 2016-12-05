using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AppleNotificationCenterService
{
    public sealed class AppleToastNotification
    {
        public string Title;
        public string Message;

        public string PositiveActionLabel = null;
        public string NegativeActionLabel = null;

        private NotificationSourceData NotificationSource;

        public AppleToastNotification(NotificationSourceData source)
        {
            this.NotificationSource = source;
        }

        public AppleToastNotification(NotificationSourceData source, NotificationAttributeCollection attributes)
        {
            NotificationSource = source;

            if (attributes.ContainsKey(NotificationAttributeID.Title))
            {
                this.Title = attributes[NotificationAttributeID.Title];
            }
            else
            {
                this.Title = "Apple Notification";
            }

            if (attributes.ContainsKey(NotificationAttributeID.Message))
            {
                this.Message = attributes[NotificationAttributeID.Message];
            }
            else
            {
                this.Message = "New incoming notification";
            }

            if (source.EventFlags.HasFlag(EventFlags.EventFlagPositiveAction))
            {
                if (attributes.ContainsKey(NotificationAttributeID.PositiveActionLabel))
                {
                    this.PositiveActionLabel = attributes[NotificationAttributeID.PositiveActionLabel];
                }
                else
                {
                    this.PositiveActionLabel = "Positive";
                }
            }

            if (source.EventFlags.HasFlag(EventFlags.EventFlagNegativeAction))
            {
                if (attributes.ContainsKey(NotificationAttributeID.NegativeActionLabel))
                {
                    this.NegativeActionLabel = attributes[NotificationAttributeID.NegativeActionLabel];
                }
                else
                {
                    this.NegativeActionLabel = "Negative";
                }
            }
        }

        public Windows.Data.Xml.Dom.XmlDocument GetXml()
        {
            XmlDocument XmlDocument = new XmlDocument();
            XmlElement toast = XmlDocument.CreateElement("toast");
            XmlAttribute toastLaunch = XmlDocument.CreateAttribute("launch");
            toastLaunch.Value = this.NotificationSource.NotificationUID.ToString();
            toast.Attributes.Append(toastLaunch);
            XmlDocument.AppendChild(toast);

            XmlElement visual = XmlDocument.CreateElement("visual");
            XmlElement binding = XmlDocument.CreateElement("binding");
            XmlAttribute bindingTemplate = XmlDocument.CreateAttribute("template");
            bindingTemplate.Value = "ToastGeneric";
            binding.Attributes.Append(bindingTemplate);

            XmlElement title = XmlDocument.CreateElement("text");
            XmlAttribute titleId = XmlDocument.CreateAttribute("id");
            titleId.Value = "1";
            title.Attributes.Append(titleId);
            title.AppendChild(XmlDocument.CreateTextNode(this.Title));
            binding.AppendChild(title);

            XmlElement content = XmlDocument.CreateElement("text");
            XmlAttribute contentId = XmlDocument.CreateAttribute("id");
            contentId.Value = "2";
            content.Attributes.Append(contentId);
            content.AppendChild(XmlDocument.CreateTextNode(this.Message));
            binding.AppendChild(content);

            // <image src='{logo}' placement='appLogoOverride' hint-crop='circle'/>

            visual.AppendChild(binding);
            toast.AppendChild(visual);

            // Actions

            if (this.NotificationSource.EventFlags.HasFlag(EventFlags.EventFlagPositiveAction) || this.NotificationSource.EventFlags.HasFlag(EventFlags.EventFlagNegativeAction))
            {
                XmlElement actions = XmlDocument.CreateElement("actions");

                if (this.NotificationSource.EventFlags.HasFlag(EventFlags.EventFlagPositiveAction))
                {
                    XmlElement positiveAction = XmlDocument.CreateElement("action");
                    XmlAttribute positiveActionContent = XmlDocument.CreateAttribute("content");
                    positiveActionContent.Value = this.PositiveActionLabel;

                    XmlAttribute positiveActionArgument = XmlDocument.CreateAttribute("arguments");
                    positiveActionArgument.Value = "positive";

                    XmlAttribute positiveActionActivationType = XmlDocument.CreateAttribute("activationType");
                    positiveActionActivationType.Value = "foreground";


                    positiveAction.Attributes.Append(positiveActionContent);
                    positiveAction.Attributes.Append(positiveActionArgument);
                    positiveAction.Attributes.Append(positiveActionActivationType);
                    actions.AppendChild(positiveAction);
                }

                if (this.NotificationSource.EventFlags.HasFlag(EventFlags.EventFlagNegativeAction))
                {
                    XmlElement negativeAction = XmlDocument.CreateElement("action");
                    XmlAttribute negativeActionContent = XmlDocument.CreateAttribute("content");
                    negativeActionContent.Value = this.NegativeActionLabel;

                    XmlAttribute negativeActionArgument = XmlDocument.CreateAttribute("arguments");
                    negativeActionArgument.Value="negative";

                    XmlAttribute negativeActionActivationType = XmlDocument.CreateAttribute("activationType");
                    negativeActionActivationType.Value = "foreground";

                    negativeAction.Attributes.Append(negativeActionContent);
                    negativeAction.Attributes.Append(negativeActionArgument);
                    negativeAction.Attributes.Append(negativeActionActivationType);
                    actions.AppendChild(negativeAction);
                }

                toast.AppendChild(actions);
            }

            Windows.Data.Xml.Dom.XmlDocument toastXml = new Windows.Data.Xml.Dom.XmlDocument();
            toastXml.LoadXml(XmlDocument.OuterXml);

            return toastXml;
        }
    }
}
