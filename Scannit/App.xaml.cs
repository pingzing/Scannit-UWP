﻿using Scannit.Broker;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Scannit
{
    sealed partial class App : Application
    {
        public ScannerComm Scanner;

        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            Scanner = new ScannerComm();            
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }

            await Scanner.StartScanner();
            var currentState = await SharedState.GetAsync();
            if (currentState == null)
            {
                currentState = new SharedFileModel();
            }
            currentState.IsAppInForeground = true;
            await SharedState.SetAsync(currentState);
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
        
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            var currentState = await SharedState.GetAsync();
            if (currentState == null)
            {
                currentState = new SharedFileModel();
            }
            currentState.IsAppInForeground = false;
            await SharedState.SetAsync(currentState);

            deferral.Complete();
        }       
    }
}
