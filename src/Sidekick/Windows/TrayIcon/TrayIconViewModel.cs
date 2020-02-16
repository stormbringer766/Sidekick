using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.Options;
using Sidekick.Core.Initialization;
using Sidekick.Core.Settings;
using Sidekick.Localization;
using Sidekick.Localization.Tray;
using Sidekick.UI.Views;
using Sidekick.Windows.ApplicationLogs;
using Sidekick.Windows.Settings;

namespace Sidekick.Windows.TrayIcon
{
    public class TrayIconViewModel : IOnAfterInit, IDisposable
    {
        private readonly App application;
        private readonly IOptionsMonitor<SidekickSettings> settings;
        private readonly IUILanguageProvider uiLanguageProvider;
        private readonly IViewLocator viewLocator;
        private readonly ApplicationLogsController applicationLogsController;

        public TrayIconViewModel(
            App application,
            IOptionsMonitor<SidekickSettings> settings,
            IUILanguageProvider uiLanguageProvider,
            IViewLocator viewLocator,
            ApplicationLogsController applicationLogsController)
        {
            this.application = application;
            this.settings = settings;
            this.uiLanguageProvider = uiLanguageProvider;
            this.viewLocator = viewLocator;
            this.applicationLogsController = applicationLogsController;
        }

        private TaskbarIcon TrayIcon { get; set; }

        public ICommand ShowSettingsCommand => new RelayCommand(_ => viewLocator.Open<SettingsView>());

        public ICommand ShowLogsCommand => new RelayCommand(_ => applicationLogsController.Show());

        public ICommand ExitApplicationCommand => new RelayCommand(_ => application.Shutdown());

        public ContextMenu ContextMenu { get; set; }

        public Task OnAfterInit()
        {
            if (TrayIcon == null)
            {
                TrayIcon = (TaskbarIcon)application.FindResource("TrayIcon");
                TrayIcon.DataContext = this;

                uiLanguageProvider.UILanguageChanged += InitContextMenu;
                InitContextMenu();

                TrayIcon.ShowBalloonTip(
                    TrayResources.Notification_Title,
                    string.Format(TrayResources.Notification_Message, settings.CurrentValue.Key_CheckPrices.ToKeybindString(), settings.CurrentValue.Key_CloseWindow.ToKeybindString()),
                    TrayIcon.Icon,
                    largeIcon: true);
            }

            return Task.CompletedTask;
        }

        private void InitContextMenu()
        {
            var cultureInfo = new CultureInfo(uiLanguageProvider.Current.Name);
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;

            if (TrayIcon.ContextMenu == null)
            {
                TrayIcon.ContextMenu = new ContextMenu();
            }

            TrayIcon.ContextMenu.Items.Clear();
            TrayIcon.ContextMenu.Items.Add(new MenuItem()
            {
                Header = TrayResources.Settings,
                Command = new RelayCommand(_ => viewLocator.Open<SettingsView>())
            });
            TrayIcon.ContextMenu.Items.Add(new MenuItem()
            {
                Header = TrayResources.ShowLogs,
                Command = new RelayCommand(_ => applicationLogsController.Show())
            });
            TrayIcon.ContextMenu.Items.Add(new Separator());
            TrayIcon.ContextMenu.Items.Add(new MenuItem()
            {
                Header = TrayResources.Exit,
                Command = new RelayCommand(_ => application.Shutdown())
            });
        }

        public void Dispose()
        {
            if (TrayIcon != null)
            {
                TrayIcon.Dispose();
            }

            uiLanguageProvider.UILanguageChanged -= InitContextMenu;
        }
    }
}
