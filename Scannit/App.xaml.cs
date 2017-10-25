using Scannit.Broker;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.UI.Core;
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
            this.Resuming += OnResuming;
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

                SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
                Window.Current.Activate();
            }

            await SharedState.SetAsync(SharedState.IsApplicationInForeground, true);
            await Scanner.StartScanner();
        }

        private void App_BackRequested(object sender, BackRequestedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame.CanGoBack)
            {
                rootFrame.GoBack();
                e.Handled = true;
            }
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
        
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();
            var newSession = new ExtendedExecutionSession
            {
                Reason = ExtendedExecutionReason.SavingData
            };
            newSession.Revoked += NewSession_Revoked;

            ExtendedExecutionResult result = await newSession.RequestExtensionAsync();

            if (result == ExtendedExecutionResult.Allowed)
            {
                await SharedState.LogAsync("Scannit: SUSPENDING!");
                await SharedState.SetAsync(SharedState.IsApplicationInForeground, false);
                newSession.Revoked -= NewSession_Revoked;
                newSession.Dispose();
                newSession = null;
            }            
            deferral.Complete();
        }

        private void NewSession_Revoked(object sender, ExtendedExecutionRevokedEventArgs args)
        {
            // =(
        }

        private async void OnResuming(object sender, object e)
        {
            await SharedState.LogAsync("Scannit: RESUMING!");
            await SharedState.SetAsync(SharedState.IsApplicationInForeground, true);
            await Scanner.StartScanner();
        }
    }
}
