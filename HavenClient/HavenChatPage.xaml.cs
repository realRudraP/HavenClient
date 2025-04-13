using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Net.Http.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using HavenClient.Models;
using HavenClient.Services;
using HavenClient.Converters;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Linq;
using HavenClient;
using System.Net.NetworkInformation;


namespace HavenClient
{
    public sealed partial class HavenChatPage : Page
    {
        public ObservableCollection<Message> Messages { get; } = new ObservableCollection<Message>();
        public ObservableCollection<string> ActiveUsers { get; } = new ObservableCollection<string>();

        private static readonly HttpClient _httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        private ChatPageNavigationParameter? _navParameter;
        private long _lastKnownMessageId = 0;
        private DispatcherTimer? _pollingTimer;

        public HavenChatPage()
        {
            this.InitializeComponent();
            MessagesListView.ItemsSource = Messages;
            UsersListView.ItemsSource = ActiveUsers; // Assume UsersListView exists in XAML
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is ChatPageNavigationParameter navParam)
            {
                System.Diagnostics.Debug.WriteLine($"Navigated to HavenChatPage with parameter: {e.Parameter}");
                _navParameter = navParam;

                if (_navParameter.IsHosting)
                {
                    string? localIp = GetLocalIPAddress();
                    if (!string.IsNullOrEmpty(localIp))
                    {
                        HostInfoTextBlock.Text = $"Hosting. Others connect to: {localIp}:{App.HostingPort}";
                        HostInfoBorder.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        HostInfoTextBlock.Text = "Hosting, but local IP could not be determined.";
                        HostInfoBorder.Visibility = Visibility.Visible;
                    }
                    ConnectionStatusText.Text = $"Hosting session as '{_navParameter.Nickname}'";
                    ActiveUsers.Add($"{_navParameter.Nickname} (Host)"); // Add host to user list
                }
                else
                {
                    HostInfoBorder.Visibility = Visibility.Collapsed;
                    if (!string.IsNullOrEmpty(_navParameter.PeerAddress))
                    {
                        ConnectionStatusText.Text = $"Connected to {_navParameter.PeerAddress} as '{_navParameter.Nickname}'";
                        SetupPollingTimer();
                        _pollingTimer?.Start();
                        await PollForUpdatesAsync(); // Initial fetch
                    }
                    else
                    {
                        ConnectionStatusText.Text = $"Not connected";
                        if (this.Frame.CanGoBack) this.Frame.GoBack();
                    }
                }

                App.MessageService.UIMessageReceived += MessageService_MessageReceived;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("HavenChatPage received invalid navigation parameters.");
                if (this.Frame.CanGoBack) this.Frame.GoBack();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            App.MessageService.UIMessageReceived -= MessageService_MessageReceived;
            _pollingTimer?.Stop();
            _pollingTimer = null;
            base.OnNavigatedFrom(e);
        }

        private void MessageService_MessageReceived(object? sender, Message message)
        {
             System.Diagnostics.Debug.WriteLine($"DEBUG (Host UI Event): Received Message ID {message.Id}, Text: '{message.Text}', Raw Timestamp: {message.Timestamp:o}, Kind: {message.Timestamp.Kind}");
            DateTime checkLocalTime = message.Timestamp.ToLocalTime();
            System.Diagnostics.Debug.WriteLine($"DEBUG (Host UI Event): Calculated Local Time: {checkLocalTime:o}");
 
            if (_navParameter?.IsHosting ?? false) // Ensure only host UI adds via event
            {
                Messages.Add(message);
                MessagesListView.ScrollIntoView(message);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG: MessageService_MessageReceived called on CLIENT - IGNORING direct add.");
              }
        }

        private void SetupPollingTimer()
        {
            _pollingTimer = new DispatcherTimer();
            _pollingTimer.Interval = TimeSpan.FromSeconds(3); // Poll interval
            _pollingTimer.Tick += async (sender, e) => await PollForUpdatesAsync();
        }

        private async Task PollForUpdatesAsync()
        {
            if (_navParameter == null || _navParameter.IsHosting || string.IsNullOrWhiteSpace(_navParameter.PeerAddress))
            {
                _pollingTimer?.Stop();
                return;
            }

            string targetAddress = _navParameter.PeerAddress;
            Uri messagesUri;
            Uri usersUri;

            try
            {
                if (!targetAddress.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !targetAddress.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    targetAddress = "http://" + targetAddress;
                }
                var baseUri = new Uri(targetAddress, UriKind.Absolute);
                messagesUri = new Uri(baseUri, $"/api/message?since={_lastKnownMessageId}");
                usersUri = new Uri(baseUri, "/api/users");
            }
            catch (UriFormatException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Polling Error: Invalid peer address format: {targetAddress} - {ex.Message}");
                _pollingTimer?.Stop();
                 return;
            }

            bool messagesFetched = false;
            List<Message>? receivedMessages = null;
            try
            {
                System.Diagnostics.Debug.WriteLine($"Polling messages: {messagesUri}");
                receivedMessages = await _httpClient.GetFromJsonAsync<List<Message>>(messagesUri);
                messagesFetched = true;

                if (receivedMessages != null && receivedMessages.Any())
                {
                    if (receivedMessages != null && receivedMessages.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"DEBUG: Polled and received {receivedMessages.Count} message(s).");
                        long maxId = _lastKnownMessageId;
                        foreach (var message in receivedMessages.OrderBy(m => m.Id)) // Process in order
                        {
                            System.Diagnostics.Debug.WriteLine($"DEBUG: Processing polled message ID {message.Id}, Sender: '{message.SenderNickname}'");

                             bool alreadyExists = message.Id != 0 && Messages.Any(m => m.Id == message.Id);

                            if (!alreadyExists)
                            {
                                 bool isOwnMessage = message.SenderNickname == _navParameter?.Nickname;
                                message.IsSentByMe = isOwnMessage; // Set the flag for UI display

                                System.Diagnostics.Debug.WriteLine($"DEBUG: ADDING message ID {message.Id} to UI. IsOwn={isOwnMessage}");

                                Messages.Add(message); // Add the message (own or others)
                                MessagesListView.ScrollIntoView(message);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"DEBUG: SKIPPING already existing message ID {message.Id}.");
                            }

                             if (message.Id > maxId)
                            {
                                maxId = message.Id;
                            }
                        }
                        _lastKnownMessageId = maxId; // Update the marker for the next poll
                        System.Diagnostics.Debug.WriteLine($"DEBUG: Updated _lastKnownMessageId to {_lastKnownMessageId}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"DEBUG: Polled, but received no new messages or null/empty list.");
                    }
                }
            }
            catch (HttpRequestException httpEx) when (httpEx.InnerException is System.Net.Sockets.SocketException)
            {
                System.Diagnostics.Debug.WriteLine($"Polling Error (Messages): Host likely down - {httpEx.Message}");
                ConnectionStatusText.Text = "Connection lost...";
                _pollingTimer?.Stop();
            }
            catch (HttpRequestException httpEx)
            {
                System.Diagnostics.Debug.WriteLine($"Polling Error (Messages): {httpEx.StatusCode} - {httpEx.Message}");
             }
            catch (System.Text.Json.JsonException jsonEx)
            {
                System.Diagnostics.Debug.WriteLine($"Polling JSON Error (Messages): {jsonEx.Message}");
             }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine($"Polling Timeout (Messages): {messagesUri}");
             }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Polling Unexpected Error (Messages): {ex.GetType().Name} - {ex.Message}");
            }


             if (!messagesFetched && receivedMessages == null) return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"Polling users: {usersUri}");
                var userList = await _httpClient.GetFromJsonAsync<List<string>>(usersUri);

                if (userList != null)
                {
                    var usersToAdd = userList.Except(ActiveUsers).ToList();
                    var usersToRemove = ActiveUsers.Except(userList).ToList();

                    foreach (var user in usersToRemove) ActiveUsers.Remove(user);
                    foreach (var user in usersToAdd) ActiveUsers.Add(user);
                }
            }
            catch (HttpRequestException httpEx) when (httpEx.InnerException is System.Net.Sockets.SocketException)
            {
                System.Diagnostics.Debug.WriteLine($"Polling Error (Users): Host likely down - {httpEx.Message}");
             }
            catch (HttpRequestException httpEx)
            {
                System.Diagnostics.Debug.WriteLine($"Polling Error (Users): {httpEx.StatusCode} - {httpEx.Message}");
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                System.Diagnostics.Debug.WriteLine($"Polling JSON Error (Users): {jsonEx.Message}");
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine($"Polling Timeout (Users): {usersUri}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Polling Unexpected Error (Users): {ex.GetType().Name} - {ex.Message}");
            }
        }


        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();
        }

        private async void MessageInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                bool shiftDown = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift) == Windows.UI.Core.CoreVirtualKeyStates.Down);
                if (!shiftDown)
                {
                    await SendMessageAsync();
                    e.Handled = true;
                }
            }
        }

        public static string? GetLocalIPAddress()
        {
            string? selectedIp = null;
            try
            {
                 var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up && // Must be operational
                                (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || // WiFi
                                 ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet))      // Ethernet
                    .OrderByDescending(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211); // Prefer WiFi if available

                foreach (var ni in networkInterfaces)
                {
                    var ipProps = ni.GetIPProperties();
                     var ipv4AddressInfo = ipProps.UnicastAddresses
                        .FirstOrDefault(addrInfo => addrInfo.Address.AddressFamily == AddressFamily.InterNetwork && // IPv4
                                                    !System.Net.IPAddress.IsLoopback(addrInfo.Address)); // Not loopback

                    if (ipv4AddressInfo != null)
                    {
                        selectedIp = ipv4AddressInfo.Address.ToString();
                        System.Diagnostics.Debug.WriteLine($"Selected IP Address: {selectedIp} from Interface: {ni.Name} ({ni.Description})");
                        break; // Found a suitable IP, stop searching
                    }
                }

                 if (string.IsNullOrEmpty(selectedIp))
                {
                    System.Diagnostics.Debug.WriteLine("No preferred (WiFi/Ethernet) IP found, attempting fallback...");
                    var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                    selectedIp = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(ip))?.ToString();
                    if (!string.IsNullOrEmpty(selectedIp))
                    {
                        System.Diagnostics.Debug.WriteLine($"Fallback selected IP Address: {selectedIp}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting local IP address: {ex.Message}");
                return null; // Error occurred
            }

            if (string.IsNullOrEmpty(selectedIp))
            {
                System.Diagnostics.Debug.WriteLine("Could not determine a suitable local IP address.");
            }

            return selectedIp;
        }

        private async Task SendMessageAsync()
        {
            string messageText = MessageInput.Text?.Trim();
            if (string.IsNullOrWhiteSpace(messageText)) return;
            if (_navParameter == null) { ShowErrorDialog("Error: Navigation context lost."); return; }

            if (_navParameter.IsHosting)
            {
                SetSendingState(true);
                App.MessageService.CreateAndProcessLocalMessage(messageText, _navParameter.Nickname ?? "Host");
                MessageInput.Text = "";
                SetSendingState(false);
            }
            else
            {
                string? targetAddress = _navParameter.PeerAddress;
                if (string.IsNullOrWhiteSpace(targetAddress)) { ShowErrorDialog("Error: No peer address specified."); return; }

                UriBuilder uriBuilder;
                try
                {
                    if (!targetAddress.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !targetAddress.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        targetAddress = "http://" + targetAddress;
                    }
                    uriBuilder = new UriBuilder(targetAddress);
                    uriBuilder.Path = "/api/message";
                }
                catch (UriFormatException ex)
                {
                    ShowErrorDialog($"Invalid peer address format: {targetAddress}\n{ex.Message}");
                    return;
                }

                var messageToSend = new Message
                {
                    Id = 0, // Client doesn't know ID yet, server assigns it
                    Text = messageText,
                    SenderNickname = _navParameter.Nickname ?? "Peer"
                };

                SetSendingState(true);
                HttpResponseMessage? response = null;
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Attempting to POST to: {uriBuilder.Uri}");
                    response = await _httpClient.PostAsJsonAsync(uriBuilder.Uri, messageToSend);

                    if (response.IsSuccessStatusCode)
                    {
   
                            MessageInput.Text = "";
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        ShowErrorDialog($"Failed to send ({response.StatusCode}):\n{response.ReasonPhrase}\n{errorContent}");
                    }
                }
                catch (HttpRequestException httpEx) when (httpEx.InnerException is System.Net.Sockets.SocketException)
                {
                    ShowErrorDialog($"Connection error sending message: Host not reachable?\n{httpEx.Message}");
                    ConnectionStatusText.Text = "Connection lost...";
                    _pollingTimer?.Stop();
                }
                catch (HttpRequestException httpEx)
                {
                    var innerExMessage = httpEx.InnerException != null ? $"\nInner: {httpEx.InnerException.Message}" : "";
                    ShowErrorDialog($"Network error sending: {httpEx.Message}{innerExMessage}\nIs '{uriBuilder.Uri}' correct?");
                }
                catch (TaskCanceledException)
                {
                    ShowErrorDialog($"Timeout sending message to {uriBuilder.Uri}.\nIs the host running?");
                }
                catch (Exception ex)
                {
                    ShowErrorDialog($"Error sending: {ex.GetType().Name}\n{ex.Message}");
                }
                finally
                {
                    SetSendingState(false);
                    response?.Dispose();
                }
            }
        }

        private void SetSendingState(bool isSending)
        {
            MessageInput.IsEnabled = !isSending;
            SendButton.IsEnabled = !isSending;
            if (!isSending && MessageInput.IsEnabled)
            {
                MessageInput.Focus(FocusState.Programmatic);
            }
        }

        private async void ShowErrorDialog(string message)
        {
            if (ErrorDialog == null || ErrorDialogContent == null) return; // Ensure controls exist
            ErrorDialogContent.Text = message;
            ErrorDialog.XamlRoot = this.XamlRoot;
            try
            {
                await ErrorDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing dialog: {ex.Message}");
            }
        }
    }
}