using HslTravelSharp.Core.Models;
using HslTravelSharpUwp;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Enumeration;
using Windows.Devices.SmartCards;
using Windows.System.Threading;
using Windows.UI.Notifications;

namespace Scannit.BackgroundScanner
{
    public sealed class BackgroundScanner : IBackgroundTask
    {
        BackgroundTaskDeferral _deferral;                
        private IBackgroundTaskInstance _taskInstance;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _taskInstance = taskInstance;
            _deferral = taskInstance.GetDeferral();
            _taskInstance.Progress = 1;
            _taskInstance.Canceled += TaskInstance_Canceled;

            string selector = SmartCardReader.GetDeviceSelector();
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(selector);
            var reader = await SmartCardReader.FromIdAsync(devices.FirstOrDefault().Id);
            reader.CardAdded += Reader_CardAdded;        
        }

        private async void Reader_CardAdded(SmartCardReader sender, CardAddedEventArgs args)
        {
            try
            {
                TravelCard card = await CardOperations.ReadTravelCardAsync(args.SmartCard);
                if (card != null)
                {
                    PostToastNotification(card);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to read travel card! Exception: {ex}\nStack trace: {ex.StackTrace}");
            }
        }

        private void PostToastNotification(TravelCard card)
        {
            DateTimeOffset youngestValueExpiration = card.PeriodEndDate1 > card.PeriodEndDate2 ? card.PeriodEndDate1 : card.PeriodEndDate2;
            ToastVisual visual = new ToastVisual()
            {
                BindingGeneric = new ToastBindingGeneric()
                {
                    Children =
                    {
                        new AdaptiveText()
                        {
                            Text = "Scannit"
                        },
                        new AdaptiveText()
                        {
                            Text = $"Remaining value: {(card.ValueTotalCents / 100m).ToString("#.##")} €",
                            HintMaxLines = 1
                        },
                        new AdaptiveText()
                        {
                            Text = $"Season pass expires: {youngestValueExpiration.ToLocalTime().ToString(DateTimeFormatInfo.CurrentInfo.ShortDatePattern)}"
                        }
                    }
                },
            };

            ToastContent content = new ToastContent
            {
                Visual = visual,
                Scenario = ToastScenario.Default
            };

            var toast = new ToastNotification(content.GetXml());
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {            
            _taskInstance.Progress = 0;            
            _deferral.Complete();
        }
    }
}
