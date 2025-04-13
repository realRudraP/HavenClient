// Haven.UI.LoginPage.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation; // Needed if navigating away
using System;
using System.Linq;
using System.Threading.Tasks;
// Add necessary using directives from your previous App.xaml.cs setup
using HavenClient; // Assuming App.xaml.cs is in this namespace
using HavenClient.Services; // If MessageService is used directly here

namespace HavenClient
{
    // Define a simple class to pass parameters during navigation
    public class ChatPageNavigationParameter
    {
        public string Nickname { get; set; }
        public bool IsHosting { get; set; }
        public string? PeerAddress { get; set; } // Nullable if hosting
    }

    public sealed partial class LoginPage : Page
    {
        private string? _userNickname; // Store the nickname

        public LoginPage()
        {
            this.InitializeComponent();
            // Ensure XamlRoot is available for dialogs after the page is loaded
            this.Loaded += (s, e) =>
            {
                FirewallErrorDialog.XamlRoot = this.XamlRoot;
                ErrorDialog.XamlRoot = this.XamlRoot;
            };
        }

        private void ProceedButton_Click(object sender, RoutedEventArgs e)
        {
            _userNickname = NameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(_userNickname))
            {
                ShowError("Please enter a nickname!");
                NameTextBox.Focus(FocusState.Programmatic);
                return;
            }

            // Update welcome text in the next panel
            WelcomeUserText.Text = $"Alright, {_userNickname}!";

            // Hide initial login, show connection choice
            InitialLoginPanel.Visibility = Visibility.Collapsed;
            ConnectionChoicePanel.Visibility = Visibility.Visible;
            StatusTextBlock.Visibility = Visibility.Collapsed; // Hide any previous status
        }

        private async void HostButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable buttons to prevent double-clicks
            SetConnectionChoiceButtonsEnabled(false);
            StatusTextBlock.Text = "Starting server...";
            StatusTextBlock.Visibility = Visibility.Visible;

            try
            {
                // Optional: Find and display local IP for easy sharing
                string localIp = GetLocalIPAddress() ?? "?.?.?.?"; // Use helper from previous step
                string listenUrl = $"http://0.0.0.0:5000"; // Listen on all interfaces
                string displayUrl = $"http://{localIp}:5000"; // For user info

                // Get the App instance and start the server
                var appInstance = Application.Current as HavenClient.App; // Use correct namespace
                if (appInstance == null)
                {
                    ShowError("Internal application error.");
                    SetConnectionChoiceButtonsEnabled(true);
                    return;
                }

                await appInstance.StartServerAsync(listenUrl);
                await Task.Delay(200);
                StatusTextBlock.Text = $"Server started! Others can join at: {displayUrl}";
                StatusTextBlock.Visibility = Visibility.Visible; // Keep visible

                DispatcherQueue.TryEnqueue(() =>
                {

                    this.Frame.Navigate(typeof(HavenChatPage), new ChatPageNavigationParameter
                    {
                        Nickname = _userNickname, // We know it's not null here
                        IsHosting = true,
                        PeerAddress = null
                    });

                });
            }
            catch (Exception ex)
            {
                // Handle specific exceptions like port binding failure if possible
                ShowError($"Failed to start server: {ex.Message}");
                StatusTextBlock.Text = "Failed to start server.";
                StatusTextBlock.Visibility = Visibility.Visible;
                SetConnectionChoiceButtonsEnabled(true); // Re-enable buttons on failure
                // Consider showing the FirewallErrorDialog specifically for certain exceptions
                // if (ex is System.Net.Sockets.SocketException socketEx && socketEx.SocketErrorCode == System.Net.Sockets.SocketError.AccessDenied) { ... }
            }
            // Don't re-enable buttons here if navigation was successful
        }

        private void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            string peerAddress = PeerAddressInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(peerAddress))
            {
                ShowError("Please enter the host's address.");
                PeerAddressInput.Focus(FocusState.Programmatic);
                return;
            }

            // Basic validation - could be more robust (Uri.TryCreate)
            if (!peerAddress.StartsWith("http://") && !peerAddress.StartsWith("https://"))
            {
                ShowError("Address should start with http:// or https://");
                PeerAddressInput.Focus(FocusState.Programmatic);
                return;
            }

            // Disable buttons
            SetConnectionChoiceButtonsEnabled(false);
            StatusTextBlock.Text = $"Attempting to connect to {peerAddress}..."; // Optional status update
            StatusTextBlock.Visibility = Visibility.Visible;

            // Navigate to the Chat Page/View, providing the peer address
            // Replace 'ChatPage' with the actual name of your chat view/page
            this.Frame.Navigate(typeof(HavenChatPage), new ChatPageNavigationParameter
            {
                Nickname = _userNickname!, // We know it's not null here
                IsHosting = false,
                PeerAddress = peerAddress
            });

            // Note: Actual connection test happens when sending the first message
            // You could add a preliminary ping/GET request here if desired, but
            // for minimalism, we proceed directly to the chat page.
            // If navigation fails or you add a connection test that fails, re-enable buttons.
            // SetConnectionChoiceButtonsEnabled(true);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide connection choice, show initial login
            ConnectionChoicePanel.Visibility = Visibility.Collapsed;
            InitialLoginPanel.Visibility = Visibility.Visible;
            StatusTextBlock.Visibility = Visibility.Collapsed;
            _userNickname = null; // Clear stored nickname
        }

        private void SetConnectionChoiceButtonsEnabled(bool isEnabled)
        {
            HostButton.IsEnabled = isEnabled;
            JoinButton.IsEnabled = isEnabled;
            PeerAddressInput.IsEnabled = isEnabled;
            BackButton.IsEnabled = isEnabled; // Also disable back while processing
        }

        private async void ShowError(string message)
        {
            StatusTextBlock.Text = message; // Also show in status bar?
            StatusTextBlock.Visibility = Visibility.Visible;

            ErrorDialogTextBlock.Text = message;
            if (ErrorDialog.XamlRoot == null) ErrorDialog.XamlRoot = this.XamlRoot; // Ensure XamlRoot is set
            await ErrorDialog.ShowAsync();
        }

        // Keep the GetLocalIPAddress helper method from the previous example
        public static string? GetLocalIPAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }
                return host.AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString();
            }
            catch { return null; }
        }

        // Placeholder for the actual Chat Page Type - replace with your actual chat page class
        private class ChatPage : Page { /* ... Your chat page implementation ... */ }
    }
}