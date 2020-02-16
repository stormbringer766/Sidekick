using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Microsoft.Extensions.Options;
using Sidekick.Business.Apis.Poe.Models;
using Sidekick.Business.Apis.PoeNinja;
using Sidekick.Business.Apis.PoePriceInfo.Models;
using Sidekick.Business.Parsers;
using Sidekick.Business.Trades;
using Sidekick.Business.Trades.Results;
using Sidekick.Core.Loggers;
using Sidekick.Core.Natives;
using Sidekick.Core.Settings;
using Sidekick.Localization;
using Application = System.Windows.Application;
using Cursor = System.Windows.Forms.Cursor;

namespace Sidekick.Windows.Overlay
{
    public class OverlayController
    {
        private readonly ITradeClient tradeClient;
        private readonly INativeProcess nativeProcess;
        private readonly IKeybindEvents events;
        private readonly INativeClipboard clipboard;
        private readonly ILogger logger;
        private readonly IItemParser itemParser;
        private readonly IPoeNinjaCache poeNinjaCache;
        private readonly IOptionsMonitor<SidekickSettings> settings;
        private readonly OverlayWindow overlayWindow;

        private static readonly int WINDOW_WIDTH = 480;
        private static readonly int WINDOW_HEIGHT = 320;
        private static readonly int WINDOW_PADDING = 5;

        public OverlayController(
            IUILanguageProvider uiLanguageProvider,
            IPoePriceInfoClient poePriceInfoClient,
            ITradeClient tradeClient,
            INativeProcess nativeProcess,
            IKeybindEvents events,
            INativeClipboard clipboard,
            ILogger logger,
            IItemParser itemParser,
            IPoeNinjaCache poeNinjaCache,
            IOptionsMonitor<SidekickSettings> settings)
        {
            this.tradeClient = tradeClient;
            this.nativeProcess = nativeProcess;
            this.events = events;
            this.clipboard = clipboard;
            this.logger = logger;
            this.itemParser = itemParser;
            this.poeNinjaCache = poeNinjaCache;
            this.settings = settings;

            overlayWindow = new OverlayWindow(WINDOW_WIDTH, WINDOW_HEIGHT, uiLanguageProvider.Current.Name, poePriceInfoClient);
            overlayWindow.MouseDown += Window_OnHandleMouseDrag;
            overlayWindow.ItemScrollReachedEnd += Window_ItemScrollReachedEnd;

            events.OnCloseWindow += OnCloseWindow;
            events.OnPriceCheck += OnPriceCheck;
            events.OnMouseClick += MouseClicked;
        }




        public bool IsDisplayed => overlayWindow.IsDisplayed;
        public void SetQueryResult(QueryResult<ListingResult> queryResult) => overlayWindow.SetQueryResult(queryResult);
        public void Show() => overlayWindow.ShowWindow();
        public void Hide() => overlayWindow.HideWindowAndClearData();

        private async void Window_ItemScrollReachedEnd(Business.Parsers.Models.Item item, int displayedItemsCount)
        {
            var page = (int)Math.Ceiling(displayedItemsCount / 10d);
            var queryResult = await tradeClient.GetListingsForSubsequentPages(item, page);
            if (queryResult.Result.Any())
            {
                overlayWindow.AppendQueryResult(queryResult);
            }
        }

        public void Dispose()
        {
            overlayWindow?.Close();
        }

        /// <summary>
        /// Opens the window at the current cursor position.
        /// Ensures that the window will be inside the current screen bounds.
        /// </summary>
        public void Open()
        {
            var scale = 96f / nativeProcess.ActiveWindowDpi;
            var xScaled = (int)(Cursor.Position.X * scale);
            var yScaled = (int)(Cursor.Position.Y * scale);

            EnsureBounds(xScaled, yScaled, scale);
            Show();
        }

        public Point GetOverlayPosition()
        {
            Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
            return Application.Current.Dispatcher.Invoke(() => new Point(overlayWindow.Left, overlayWindow.Top));
        }

        public Size GetOverlaySize()
        {
            return new Size(overlayWindow.ActualWidth, overlayWindow.ActualHeight);
        }

        /// <summary>
        /// Ensures that the window stays within width and height of the display.
        /// </summary>
        private void EnsureBounds(int desiredX, int desiredY, float scale)
        {
            var screenRect = Screen.FromPoint(Cursor.Position).Bounds;
            var xMidScaled = (screenRect.X + (screenRect.Width / 2)) * scale;
            var yMidScaled = (screenRect.Y + (screenRect.Height / 2)) * scale;

            var positionX = desiredX + (desiredX < xMidScaled ? WINDOW_PADDING : -WINDOW_WIDTH - WINDOW_PADDING);
            var positionY = desiredY + (desiredY < yMidScaled ? WINDOW_PADDING : -WINDOW_HEIGHT - WINDOW_PADDING);

            overlayWindow.SetWindowPosition(positionX, positionY);
        }

        /// <summary>
        /// Handles dragging for the window when pressing any control that does NOT handle the event <see cref="RoutedEventArgs.Handled"/>.
        /// Does not apply clamping of the windows position.
        /// </summary>
        private void Window_OnHandleMouseDrag(object sender, MouseButtonEventArgs e)
        {
            // automatically detects mouse up/down states
            if (e.ChangedButton == MouseButton.Left)
                overlayWindow.DragMove();
        }

        private Task<bool> OnCloseWindow()
        {
            var handled = false;

            if (IsDisplayed)
            {
                Hide();
                handled = true;
            }

            return Task.FromResult(handled);
        }

        private async Task<bool> OnPriceCheck()
        {
            logger.Log("Hotkey for pricing item triggered.");

            var text = await clipboard.Copy();
            if (!string.IsNullOrWhiteSpace(text))
            {
                var item = await itemParser.ParseItem(text);

                if (item != null)
                {
                    Open();

                    var queryResult = await tradeClient.GetListings(item);
                    if (queryResult != null)
                    {
                        var poeNinjaItem = poeNinjaCache.GetItem(item);
                        if (poeNinjaItem != null)
                        {
                            queryResult.PoeNinjaItem = poeNinjaItem;
                            queryResult.LastRefreshTimestamp = poeNinjaCache.LastRefreshTimestamp;
                        }

                        SetQueryResult(queryResult);
                        return true;
                    }

                    Hide();
                    return true;
                }
            }

            return false;
        }

        private Task MouseClicked(int x, int y)
        {
            if (!IsDisplayed || !settings.CurrentValue.CloseOverlayWithMouse) return Task.CompletedTask;

            var overlayPos = GetOverlayPosition();
            var overlaySize = GetOverlaySize();

            if (x < overlayPos.X || x > overlayPos.X + overlaySize.Width
                                 || y < overlayPos.Y || y > overlayPos.Y + overlaySize.Height)
            {
                Hide();
            }

            return Task.CompletedTask;
        }
    }
}
