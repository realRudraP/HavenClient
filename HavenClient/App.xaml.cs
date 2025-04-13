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
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Threading.Tasks;
using HavenClient.Services;
using Microsoft.Extensions.Hosting;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Documents;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HavenClient
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private IHost? _webHost;
        private CancellationTokenSource? _cts;
        public static int HostingPort = 5000;
        public static MessageService MessageService { get; } = new MessageService();
        public static string UserNickname { get; set; } = "User";
        private Window? m_window;
        public static MainWindow? RootWindow { get; private set; }
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        

        public async Task StartServerAsync(string url = "http://*:5000")
        {
            if (_webHost != null)
            {
                throw new InvalidOperationException("Server is already running.");
            }
            _cts = new CancellationTokenSource();

            try
            {
                var builder = WebApplication.CreateBuilder();
                builder.WebHost.UseUrls(url);
                builder.Logging.ClearProviders();

               builder.Services.AddControllers();
                builder.Services.AddSingleton(MessageService);

                var app = builder.Build();

               app.MapControllers();

                _webHost = app;
                await _webHost.StartAsync(_cts.Token);


            }
            catch (Exception ex)
            {
                _webHost = null;
                _cts?.Dispose();
                _cts = null;
                // Handle exceptions (e.g., logging)
                Console.WriteLine($"Error starting server: {ex.Message}");
            }
        }

        public async Task StopServerAsync()
        {
            if (_webHost == null || _cts == null)
            {
                return;
            }
            try
            {
                _cts.Cancel();
                await _webHost.StopAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping server: {ex.Message}");
            }
            finally
            {
                _webHost.Dispose();
                _webHost = null;
                _cts.Dispose();
                _cts = null;    
            }
        }
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            RootWindow = m_window as MainWindow;

            MessageService.Initialize(m_window.DispatcherQueue);

            Frame rootFrame = EnsureRootFrame(m_window);
            if (rootFrame.Content == null)
            {
                rootFrame.Navigate(typeof(LoginPage), args.Arguments);
            }

            m_window.Activate();

            m_window.Closed+=async(s,e) =>
            {
                await StopServerAsync();
            };
        }
        private Frame EnsureRootFrame(Window window)
        {
            if (window.Content is Frame rootFrame)
            {
                return rootFrame;
            }
            else
            {
                // If MainWindow's XAML didn't define a Frame, create one programmatically
                rootFrame = new Frame();
                window.Content = rootFrame;
                return rootFrame;
            }
        }
        public static void NavigateToChatPage()
        {
            if (RootWindow?.AppFrame != null)
            {
                RootWindow.AppFrame.Navigate(typeof(ChatPage));
            }
        }

    }
}
