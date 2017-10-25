using HslTravelSharp.Core.Models;
using HslTravelSharpUwp;
using Microsoft.Toolkit.Uwp.Notifications;
using Scannit.Broker;
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
            await SharedState.LogAsync($"BackgroundScanner ({taskInstance.InstanceId}): Starting run of background task...");
            _taskInstance = taskInstance;
            _deferral = taskInstance.GetDeferral();
            _taskInstance.Progress = 1;
            _taskInstance.Canceled += TaskInstance_Canceled;

            string selector = SmartCardReader.GetDeviceSelector();
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(selector);
            var reader = await SmartCardReader.FromIdAsync(devices.FirstOrDefault().Id);

            await SharedState.LogAsync($"BackgroundScanner ({taskInstance.InstanceId}): Got card reader device.");

            reader.CardAdded += Reader_CardAdded;
        }

        private async void Reader_CardAdded(SmartCardReader sender, CardAddedEventArgs args)
        {
            await SharedState.LogAsync($"BackgroundScanner ({_taskInstance.InstanceId}): CardAdded event fired.");
            try
            {
                TravelCard card = await CardOperations.ReadTravelCardAsync(args.SmartCard);
                if (card != null)
                {
                    await SharedState.LogAsync($"BackgroundScanner ({_taskInstance.InstanceId}): Successful read card.");

                    Task updateCardTask = SharedState.SetAsync(SharedState.LastSeenCard, card.RawValues);
                    await SharedState.LogAsync($"BackgroundScanner ({_taskInstance.InstanceId}): LastSeenCard updated.");


                    Task updateTimestampTask = SharedState.SetAsync(SharedState.LastSeenTimestamp, DateTimeOffset.UtcNow);
                    await SharedState.LogAsync($"BackgroundScanner ({_taskInstance.InstanceId}): LastSeenTimestamp updated.");

                    if (await SharedState.GetAsync<bool>(SharedState.IsApplicationInForeground))
                    {
                        await SharedState.LogAsync($"BackgroundScanner ({_taskInstance.InstanceId}): Application is in the foreground. Changed Progress value.");

                       _taskInstance.Progress = 2;
                    }
                    else
                    {
                        await SharedState.LogAsync($"BackgroundScanner ({_taskInstance.InstanceId}): Application is in the background. Post toast notification.");


                       PostToastNotification(card);
                    }
                }
            }
            catch (Exception ex)
            {
                await SharedState.LogAsync($"BackgroundScanner ({_taskInstance.InstanceId}): Failed to read travel card! Exception: {ex}\nStack trace: {ex.StackTrace}");
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
                            Text = $"Remaining value: {(card.ValueTotalCents / 100m).ToString("#.##")} €\n" +
                            $"Season pass expires: {youngestValueExpiration.ToLocalTime().ToString(DateTimeFormatInfo.CurrentInfo.ShortDatePattern)}",
                            HintMaxLines = 2
                        },
                        new AdaptiveText()
                        {
                            Text = $"Card {card.CardNumber}\n"
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

        private async void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            await SharedState.LogAsync($"BackgroundScanner ({sender.InstanceId}): Background task cancelled. Reason: {reason}");

           _taskInstance.Canceled -= TaskInstance_Canceled;
            _taskInstance.Progress = 0;
            _deferral.Complete();
        }
    }
}
