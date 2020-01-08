using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using System.Diagnostics;
using System.Windows.Interop;
using System.Windows.Threading;
using Blynclight;

namespace EmbravaTeamsPresenceNotifications
{


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private string graphAPIEndpoint = "https://graph.microsoft.com/beta/me/presence";
        private string[] scopes = new string[] { "presence.read" };
        int nSelectedDeviceIndex = -1;

        private WindowState lastWindowState;
        public MainWindow()
        {
            InitializeComponent();
            this.notifyIcon.Text = "Presence unknown";
            LoginAsync(true);
           
        }
        private async void OnAccountClick(object sender, RoutedEventArgs e)
        {
            LoginAsync(false);
        }
        private async void LoginAsync(bool silent)
        {
            AuthenticationResult authResult = null;
            var app = App.PublicClientApp;

            var accounts = await app.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();

            try
            {
                authResult = await app.AcquireTokenSilent(scopes, firstAccount)
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                //don't do the interactive part if the flag to be silent is true. This is used when the app is started.
                if (silent==false)
                {
                   
                    // A MsalUiRequiredException happened on AcquireTokenSilent. 
                    // This indicates you need to call AcquireTokenInteractive to acquire a token
                    System.Diagnostics.Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");

                    try
                    {
                        authResult = await app.AcquireTokenInteractive(scopes)
                            .WithAccount(accounts.FirstOrDefault())
                            //.WithParentActivityOrWindow(new WindowInteropHelper(this).Handle) // optional, used to center the browser on the window, not available in .NET Core
                            .WithPrompt(Prompt.SelectAccount)
                            .ExecuteAsync();
                    }
                    catch (MsalException msalex)
                    {
                        ResultText.Text = $"Error Acquiring Token:{System.Environment.NewLine}{msalex}";
                    }
                }

            }
            catch (Exception ex)
            {
                //ResultText.Text = $"Error Acquiring Token Silently:{System.Environment.NewLine}{ex}";
                return;
            }

            if (authResult != null)
            {
                accountName.Text = authResult.Account.Username;
                this.AccountButton.Visibility = Visibility.Collapsed; 
                this.SignOutButton.Visibility = Visibility.Visible;

            }
            else
            {
                this.AccountButton.Visibility = Visibility.Visible;
                this.SignOutButton.Visibility = Visibility.Collapsed;
                accountName.Text = "none";
            }
        }

        private async void GetPresenceClick(object sender, RoutedEventArgs e)
        {
            var app = App.PublicClientApp;

            var accounts = await app.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();

            try
            {
                var authResult = await app.AcquireTokenSilent(scopes, firstAccount)
                    .ExecuteAsync();

                //get presence information
                //https://docs.microsoft.com/en-us/graph/api/resources/presence?view=graph-rest-beta

                var content = await GetHttpContentWithToken(graphAPIEndpoint, authResult.AccessToken).ConfigureAwait(false);

                //// Go back to the UI thread to make changes to the UI
                //await Dispatcher.RunAsync(System.Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                //{
                Dispatcher.Invoke(() =>
                {
                    ResultText.Text = content;
                });


                setPresenceLight(content);
                //    DisplayBasicTokenInfo(authResult);
                //    this.SignOutButton.Visibility = Visibility.Visible;
                //});

            }
            catch (MsalUiRequiredException ex)
            {
                //sorry, don't want to deal with interaction here. 
                ResultText.Text += ResultText.Text + ex.Message;
            }

           
        }
        
        private void setPresenceLight(string content)
        {
            //availability can be:
            //    Available, AvailableIdle, Away, BeRightBack, Busy, BusyIdle, DoNotDisturb, Offline, PresenceUnknown
            //activity can be:
            //    Available, Away, BeRightBack,Busy, DoNotDisturb, InACall, InAConferenceCall, Inactive,InAMeeting, Offline, OffWork,OutOfOffice, PresenceUnknown,Presenting, UrgentInterruptionsOnly.
            
            // light should be 
            //    blinking red for: in a call, in a meeting, presenting, in a call, out of office
            //    red for:          focusing
            //    green for:        available
        
        }

        private void ConnectLightClick(object sender, RoutedEventArgs e)
        {
            const byte DEVICETYPE_NODEVICE_INVALIDDEVICE_TYPE = 0x00;
            const byte DEVICETYPE_BLYNC_CHIPSET_TENX_10 = 0x01;
            const byte DEVICETYPE_BLYNC_CHIPSET_TENX_20 = 0x02;
            const byte DEVICETYPE_BLYNC_CHIPSET_V30 = 0x03;
            const byte DEVICETYPE_BLYNC_CHIPSET_V30S = 0x04;
            const byte DEVICETYPE_BLYNC_HEADSET_CHIPSET_V30_LUMENA110 = 0x05;
            const byte DEVICETYPE_BLYNC_WIRELESS_CHIPSET_V30S = 0x06;
            const byte DEVICETYPE_BLYNC_MINI_CHIPSET_V30S = 0x07;
            const byte DEVICETYPE_BLYNC_HEADSET_CHIPSET_V30_LUMENA120 = 0x08;
            const byte DEVICETYPE_BLYNC_HEADSET_CHIPSET_V30_LUMENA = 0x09;
            const byte DEVICETYPE_BLYNC_HEADSET_CHIPSET_V30_LUMENA210 = 0x0A;
            const byte DEVICETYPE_BLYNC_HEADSET_CHIPSET_V30_LUMENA220 = 0x0B;
            const byte DEVICETYPE_BLYNC_EMBRAVA_EMBEDDED_V30 = 0x0C;
            const byte DEVICETYPE_BLYNC_MINI_CHIPSET_V40S = 13;
            const byte DEVICETYPE_BLYNC_WIRELESS_CHIPSET_V40S = 14;
            const byte DEVICETYPE_BLYNC_CHIPSET_V40 = 15;
            const byte DEVICETYPE_BLYNC_CHIPSET_V40S = 16;
            const byte DEVICETYPE_BLYNC_NAMEDISPLAY_DEVICE = 17;
            const byte DEVICETYPE_BLYNC_PLANTRONICS_STATUS_INDICATOR = 21;
            const byte DEVICETYPE_BLYNC_MINI_CHIPSET_V40S_VERSION20 = 22; // version 2.0 - BrightTrend devices
            const byte DEVICETYPE_BLYNC_CHIPSET_V40_VERSION20 = 23;
            const byte DEVICETYPE_BLYNC_CHIPSET_V40S_VERSION20 = 24;

            const byte DEVICEFLASHSPEED_SLOW = 0x01;
            const byte DEVICEFLASHSPEED_MEDIUM = 0x02;
            const byte DEVICEFLASHSPEED_FAST = 0x03;

            

            BlynclightController oBlynclightController = new BlynclightController();

            this.EmbravaDeviceList.Items.Clear();

            var nNumberOfBlyncDevices = oBlynclightController.InitBlyncDevices();

            for (int i = 0; i < nNumberOfBlyncDevices; i++)
            {
                EmbravaDeviceList.Items.Insert(i, oBlynclightController.aoDevInfo[i].szDeviceName);

                if (oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_CHIPSET_TENX_10 ||
                    oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_CHIPSET_TENX_20)
                {
                    //comboBoxDeviceList.Items.Insert(nCbIndex, oBlynclightController.aoDevInfo[i].szDeviceName);
                    //nCbIndex++;

                    //EnableUIComponentsForBlyncUsb1020Devices();
                    //DisableUIComponentsForBlyncUsb30Devices();
                }
                else if (oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_CHIPSET_V30S ||
                    oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_CHIPSET_V40S ||
                    oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_CHIPSET_V40S_VERSION20 ||
                    oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_PLANTRONICS_STATUS_INDICATOR ||
                    oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_NAMEDISPLAY_DEVICE ||
                    oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_CHIPSET_V30 ||
                    oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_CHIPSET_V40 ||
                    oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_CHIPSET_V40_VERSION20 ||
                    oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_HEADSET_CHIPSET_V30_LUMENA110 ||
                    oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_HEADSET_CHIPSET_V30_LUMENA120 ||
                    oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_HEADSET_CHIPSET_V30_LUMENA ||
                    oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_WIRELESS_CHIPSET_V30S ||
                    oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_WIRELESS_CHIPSET_V40S ||
                    oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_EMBRAVA_EMBEDDED_V30 ||
                    oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_MINI_CHIPSET_V30S ||
                    oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_MINI_CHIPSET_V40S ||
                    oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_MINI_CHIPSET_V40S_VERSION20 ||
                    oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_HEADSET_CHIPSET_V30_LUMENA210 ||
                    oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_HEADSET_CHIPSET_V30_LUMENA220)
                {

                    nSelectedDeviceIndex = 0;
                    EmbravaDeviceList.SelectedIndex = 0;

                    bool bResult = false;
                    bResult = oBlynclightController.TurnOnBlueLight(nSelectedDeviceIndex);
                    bResult = oBlynclightController.StartLightFlash(nSelectedDeviceIndex);
                    bResult = oBlynclightController.SelectLightFlashSpeed(nSelectedDeviceIndex, DEVICEFLASHSPEED_SLOW);
                    //EnableUIComponentsForBlyncUsb30Devices();
                }
                /*else if (oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_HEADSET_CHIPSET_V30_LUMENA210_NOTIFICATIONINTERFACE ||
                oBlynclightController.aoDevInfo[i].byDeviceType == DEVICETYPE_BLYNC_HEADSET_CHIPSET_V30_LUMENA220_NOTIFICATIONINTERFACE)
                {
                    DisableUIComponentsForBlyncUsb1020Devices();
                    DisableUIComponentsForBlyncUsb30Devices();
                }*/

            }


        }
        private async void getPresence()
        {
            var app = App.PublicClientApp;

            var accounts = await app.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();

            try
            {
                var authResult = await app.AcquireTokenSilent(scopes, firstAccount)
                    .ExecuteAsync();


                var content = await GetHttpContentWithToken(graphAPIEndpoint, authResult.AccessToken).ConfigureAwait(false);

                //// Go back to the UI thread to make changes to the UI
                //await Dispatcher.RunAsync(System.Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                //{
                    ResultText.Text = content;
                //    DisplayBasicTokenInfo(authResult);
                //    this.SignOutButton.Visibility = Visibility.Visible;
                //});

            }
            catch (MsalUiRequiredException ex)
            {
                //sorry, don't want to deal with interaction here. 
                ResultText.Text += ResultText.Text + ex.Message;
            }
        }

        /// <summary>
        /// Perform an HTTP GET request to a URL using an HTTP Authorization header
        /// </summary>
        /// <param name="url">The URL</param>
        /// <param name="token">The token</param>
        /// <returns>String containing the results of the GET operation</returns>
        public async Task<string> GetHttpContentWithToken(string url, string token)
        {
            var httpClient = new System.Net.Http.HttpClient();
            System.Net.Http.HttpResponseMessage response;
            try
            {
                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
                //Add the token in Authorization header
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                response = await httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                return content;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
        private async void OnSignOutClick(object sender, RoutedEventArgs e)
        {
            var accounts = await App.PublicClientApp.GetAccountsAsync();
            if (accounts.Any())
            {
                try
                {
                    await App.PublicClientApp.RemoveAsync(accounts.FirstOrDefault());

                    this.AccountButton.Visibility = Visibility.Visible;
                    this.SignOutButton.Visibility = Visibility.Collapsed;
                    accountName.Text = "none";
                    this.ResultText.Text = "User has signed-out";
                }
                catch (MsalException ex)
                {
                    ResultText.Text = $"Error signing-out user: {ex.Message}";
                }
            }
        }
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            this.lastWindowState = WindowState;
            //hide the mainwindow if the user is already logged in (hidden start).
            //if there is no user logged in, always show the main screen
            //TODO build that logic
            //this.Hide();
        }
        protected override void OnStateChanged(EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();
            }
            else
            {
                this.lastWindowState = this.WindowState;
            }
        }
        private void OnNotifyIconDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.Show();
                this.WindowState = this.lastWindowState;
            }
        }
        private void OnOpenClick(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = this.lastWindowState;
        }
        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void EmbravaDeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            nSelectedDeviceIndex = EmbravaDeviceList.SelectedIndex;
        }
    }
}
