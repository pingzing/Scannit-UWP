﻿using HslTravelSharp.Core.Models;
using Scannit.Broker;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Windows.UI.Core;

namespace Scannit
{
    public class ScannerComm : INotifyPropertyChanged
    {
        private ApplicationTrigger _appTrigger;
        private BackgroundTaskRegistration _bgTask;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<TravelCard> LastSeenCardUpdated;

        private bool _isBgTaskAlive = false;
        public bool IsBgTaskAlive
        {
            get => _isBgTaskAlive;
            private set
            {
                if (_isBgTaskAlive != value)
                {
                    _isBgTaskAlive = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBgTaskAlive)));
                }
            }
        }        

        public async Task StartScanner()
        {
            if (_appTrigger == null)
            {
                StopScanner();
                _appTrigger = new ApplicationTrigger();
            }

            var requestStatus = await BackgroundExecutionManager.RequestAccessAsync();
            if (requestStatus == BackgroundAccessStatus.AlwaysAllowed
                || requestStatus == BackgroundAccessStatus.AllowedSubjectToSystemPolicy)
            {
                string entryPoint = "Scannit.BackgroundScanner.BackgroundScanner";
                string taskName = "Start background scanner";
                _bgTask = RegisterBackgroundTask(entryPoint, taskName, _appTrigger);
                ApplicationTriggerResult result = await _appTrigger.RequestAsync();
                if (result == ApplicationTriggerResult.Allowed || result == ApplicationTriggerResult.CurrentlyRunning)
                {
                    IsBgTaskAlive = true;
                }
                else
                {
                    IsBgTaskAlive = false;
                }
            }
        }

        public void StopScanner()
        {
            if (_bgTask != null)
            {
                _bgTask.Unregister(true);
                _bgTask = null;
            }
        }

        private BackgroundTaskRegistration RegisterBackgroundTask(string entryPoint, string taskName, ApplicationTrigger appTrigger)
        {
            foreach (var cur in BackgroundTaskRegistration.AllTasks)
            {
                if (cur.Value.Name == taskName)
                {                                        
                    return (BackgroundTaskRegistration)(cur.Value);
                }
            }
            var builder = new BackgroundTaskBuilder();
            builder.Name = taskName;
            builder.TaskEntryPoint = entryPoint;
            builder.SetTrigger(appTrigger);

            BackgroundTaskRegistration bgTask = builder.Register();
            bgTask.Progress += BgTask_Progress;
            bgTask.Completed += BgTask_Completed;            

            return bgTask;
        }

        private async void BgTask_Progress(BackgroundTaskRegistration sender, BackgroundTaskProgressEventArgs args)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () => IsBgTaskAlive = true);

            if (args.Progress > 0)
            {
                RawTravelCard rawCard = await SharedState.GetAsync<RawTravelCard>(SharedState.LastSeenCard);
                if (rawCard != null)
                {
                    TravelCard card = new TravelCard(rawCard);
                    LastSeenCardUpdated?.Invoke(this, card);
                }
            }
        }

        private async void BgTask_Completed(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () => IsBgTaskAlive = false);
        }
    }
}

