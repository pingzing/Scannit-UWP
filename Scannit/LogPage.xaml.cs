using Scannit.Broker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Scannit
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LogPage : Page
    {
        public LogPage()
        {
            this.InitializeComponent();            
        }        

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            this.LogBox.Text = await SharedState.GetLogContentsAsync();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {            
            this.LogBox.SelectAll();            
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            this.LogBox.Text = await SharedState.GetLogContentsAsync();
        }

        private async void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            await SharedState.ClearLogAsync();
        }
    }
}
