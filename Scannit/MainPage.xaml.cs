using HslTravelSharp.Core.Models;
using HslTravelSharpUwp;
using Scannit.Broker;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Devices.SmartCards;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Scannit
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public ScannerComm Scanner => ((App)Application.Current).Scanner;

        private TravelCard _travelCard = null;
        public TravelCard TravelCard
        {
            get => _travelCard;
            set
            {
                if (_travelCard != value)
                {
                    _travelCard = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(TravelCardValueString));
                    RaisePropertyChanged(nameof(ValueTicketInfoString));
                    RaisePropertyChanged(nameof(SeasonPassInfoString));
                    RaisePropertyChanged(nameof(CardHistory));
                }
            }
        }

        public string TravelCardValueString
        {
            get
            {
                if (TravelCard != null)
                {
                    decimal eurosAndCents = TravelCard.ValueTotalCents / 100m;
                    return eurosAndCents.ToString("#.##") + " €";
                }
                else
                {
                    return "-";
                }
            }
        }

        public string ValueTicketInfoString
        {
            get
            {
                if (TravelCard?.ValueTicket != null)
                {
                    var valueTicket = TravelCard.ValueTicket;
                    if (!TravelCard.ValueTicket.IsValid)
                    {
                        return "-";
                    }

                    if(DateTimeOffset.Now > valueTicket.ValidityEndDate)
                    {
                        return "-";
                    }

                    string validityZone = valueTicket.ValidityArea.IsZone 
                        ? valueTicket.ValidityArea.ValidityZone.ToString() 
                        : valueTicket.ValidityArea.ValidityVehicle.ToString();

                    string boardingArea = valueTicket.BoardingArea.IsZone 
                        ? valueTicket.BoardingArea.ValidityZone.ToString() 
                        : valueTicket.BoardingArea.ValidityVehicle.ToString();

                    return $"{validityZone}\n{boardingArea}\nValid until: {valueTicket.ValidityEndDate.ToLocalTime()}\nTime remaining: {Math.Round((DateTimeOffset.Now - valueTicket.ValidityEndDate).TotalMinutes, 2)}";
                }
                else
                {
                    return "-";
                }
            }
        }

        public string SeasonPassInfoString
        {
            get
            {
                if (TravelCard == null)
                {
                    return "-";
                }

                string validityZone = TravelCard.ValidityArea1.IsZone 
                    ? TravelCard.ValidityArea1.ValidityZone.ToString() 
                    : TravelCard.ValidityArea1.ValidityVehicle.ToString();
                if (TravelCard.PeriodEndDate1 >= TravelCard.PeriodEndDate2)
                {
                    return $"{validityZone}\nExpires on: {TravelCard.PeriodEndDate1.ToLocalTime()}\nTime remaining: {Math.Max(0, Math.Round((TravelCard.PeriodEndDate1 - DateTimeOffset.Now).TotalDays, 1))} days";
                }
                else
                {
                    return $"{validityZone}\nExpires on: {TravelCard.PeriodEndDate2.ToLocalTime()}\nTime remaining: {Math.Max(0, Math.Round((TravelCard.PeriodEndDate2 - DateTimeOffset.Now).TotalDays, 1))} days";
                }
            }
        }

        public IEnumerable<History> CardHistory => TravelCard?.History?.Where(x => x != null);

        public event PropertyChangedEventHandler PropertyChanged;

        public MainPage()
        {
            this.InitializeComponent();            
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ((App)Application.Current).Scanner.LastSeenCardUpdated += Scanner_LastSeenCardUpdated;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ((App)Application.Current).Scanner.LastSeenCardUpdated -= Scanner_LastSeenCardUpdated;
        }

        private async void Scanner_LastSeenCardUpdated(object sender, TravelCard e)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
               () => TravelCard = e);
        }

        private async void ToggleBackgroundScanning()
        {
            if (Scanner.IsBgTaskAlive)
            {
                Scanner.StopScanner();
            }
            else
            {
                await Scanner.StartScanner();
            }
        }

        private void RaisePropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(LogPage));
        }
    }
}
