/*
 * MainWindow.xaml.cs - Desktop Pet Behavior and Logic
 * 
 * This file contains all the code-behind logic for the desktop pet window.
 * It implements the core functionality that makes the pet interactive and animated.
 * 
 * Key Features:
 * - Static desktop pet with animated GIF 
 * - Drag functionality for manual repositioning anywhere on screen
 * - Right-click context menu for user controls (Exit)
 * - Transparent window that stays on top without taskbar presence
 * - WpfAnimatedGif integration for proper GIF animation support
 */

using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using WpfAnimatedGif;

namespace DesktopPet
{
    /// <summary>
    /// MainWindow class that implements the desktop pet behavior.
    /// Inherits from Window to provide WPF window functionality.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Fields
        
        /// <summary>
        /// Legacy field - no longer used since auto movement was removed
        /// </summary>
        private double dx = 2;
        
        /// <summary>
        /// Legacy timer - no longer used since auto movement was removed  
        /// </summary>
        private DispatcherTimer timer;
        
        /// <summary>
        /// Flag to track if the pet is currently being dragged by the user
        /// </summary>
        private bool isDragging = false;

        /// <summary>
        /// HTTP client for fetching random cat GIFs from API
        /// </summary>
        private static readonly HttpClient httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

    /// <summary>
    /// Keeps the current GIF stream alive while animating; disposed on replace/exit
    /// </summary>
    private MemoryStream? _currentGifStream;

    /// <summary>
    /// Prevent overlapping loads from multiple rapid clicks
    /// </summary>
    private bool _isLoadingRandomCat = false;
    private string? _lastGifId;

        /// <summary>
        /// Disposes a stream after a short delay to give the GIF decoder time to detach
        /// </summary>
        private void DisposeStreamLater(Stream? stream)
        {
            if (stream == null) return;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            timer.Tick += (_, __) =>
            {
                try { stream.Dispose(); } catch { /* ignore */ }
                finally { timer.Stop(); }
            };
            timer.Start();
        }
        
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// Sets up the initial position for the static desktop pet.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Initial position - at screen bottom
            Left = 100;
            Top = SystemParameters.PrimaryScreenHeight - Height - 50; // 50 pixels from bottom

            // Timer configuration - DISABLED for static pet with GIF animation only
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(30); // 30ms = ~33 FPS for smooth animation
            timer.Tick += MovePet; // Subscribe to timer tick event
            // timer.Start(); // AUTO MOVEMENT DISABLED - pet stays in place

            // Ensure a basic User-Agent so some CDNs/APIs don't reject the request
            try { httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DesktopPet/1.0"); } catch { /* ignore */ }
        }

        #endregion

        #region Movement Logic

        /// <summary>
        /// Timer tick event handler that moves the pet horizontally across the screen.
        /// Called every 30ms to create smooth animation.
        /// </summary>
        /// <param name="sender">The timer object that triggered this event</param>
        /// <param name="e">Event arguments (not used)</param>
        private void MovePet(object? sender, EventArgs e)
        {
            // Only move automatically when not being dragged
            if (!isDragging)
            {
                // Move horizontally by dx pixels
                Left += dx;

                // Keep the current vertical position (don't force to bottom)
                // Top = SystemParameters.PrimaryScreenHeight - Height - 50;

                // Bounce off screen boundaries
                if (Left + Width >= SystemParameters.PrimaryScreenWidth || Left <= 0)
                {
                    dx = -dx; // Reverse direction
                }
            }
        }

        #endregion

        #region Random Cat GIF Loading

        /// <summary>
        /// Loads a random cat GIF from TheCatAPI and replaces the current image.
        /// Uses the same display method as the local cat.gif.
        /// </summary>
        private async Task LoadRandomCatGifAsync()
        {
            if (_isLoadingRandomCat) return;
            _isLoadingRandomCat = true;
            try
            {
                // TheCatAPI - free, no API key required for basic usage
                string apiUrl = "https://api.thecatapi.com/v1/images/search?mime_types=gif&limit=1";
                
                App.LogInfo($"Requesting random cat: {apiUrl}", nameof(LoadRandomCatGifAsync));
                using var metaResponse = await httpClient.GetAsync(apiUrl);
                if (!metaResponse.IsSuccessStatusCode)
                {
                    App.LogInfo($"Meta request failed: {(int)metaResponse.StatusCode} {metaResponse.ReasonPhrase}", nameof(LoadRandomCatGifAsync));
                    return; // keep current image
                }

                var responseText = await metaResponse.Content.ReadAsStringAsync();
                var json = JArray.Parse(responseText);
                
                if (json.Count > 0)
                {
                    var gifUrl = json[0]["url"]?.ToString();
                    var gifId = json[0]["id"]?.ToString();
                    
                    // If API returns same GIF id as last time, try once more to get a different one
                    if (!string.IsNullOrEmpty(gifId) && gifId == _lastGifId)
                    {
                        try
                        {
                            using var second = await httpClient.GetAsync(apiUrl);
                            if (second.IsSuccessStatusCode)
                            {
                                var text2 = await second.Content.ReadAsStringAsync();
                                var json2 = JArray.Parse(text2);
                                if (json2.Count > 0)
                                {
                                    gifUrl = json2[0]["url"]?.ToString() ?? gifUrl;
                                    gifId = json2[0]["id"]?.ToString() ?? gifId;
                                }
                            }
                        }
                        catch { /* ignore */ }
                    }

                    if (!string.IsNullOrEmpty(gifUrl))
                    {
                        // Cache-bust the URL used for download
                        var builder = new UriBuilder(gifUrl);
                        var cb = $"cb={Guid.NewGuid():N}";
                        if (string.IsNullOrEmpty(builder.Query)) builder.Query = cb; else builder.Query = builder.Query.TrimStart('?') + "&" + cb;
                        var downloadUri = builder.Uri;

                        // Download bytes off the UI thread to avoid BitmapImage async download events
                        byte[] bytes = await httpClient.GetByteArrayAsync(downloadUri);

                        // Now switch image on the UI thread
                        await Dispatcher.InvokeAsync(async () =>
                        {
                            try
                            {
                                // Detach current animation and yield a render
                                ImageBehavior.SetAnimatedSource(PetImage, null);
                                PetImage.Source = null;
                            }
                            catch { /* ignore */ }
                            await Dispatcher.Yield(DispatcherPriority.Render);

                            try
                            {
                                // Build new stream and image; keep stream alive for animation
                                var newStream = new MemoryStream(bytes);
                                var bitmapImage = new BitmapImage();
                                bitmapImage.BeginInit();
                                bitmapImage.StreamSource = newStream;
                                bitmapImage.CacheOption = BitmapCacheOption.None;
                                bitmapImage.EndInit();

                                // Swap streams safely
                                var oldStream = _currentGifStream;
                                _currentGifStream = newStream;

                                ImageBehavior.SetAnimatedSource(PetImage, bitmapImage);
                                _lastGifId = gifId ?? _lastGifId;
                                App.LogInfo($"Loaded random cat (stream): id={_lastGifId}, bytes={bytes.Length}", nameof(LoadRandomCatGifAsync));

                                // Dispose previous stream after a short delay
                                DisposeStreamLater(oldStream);
                            }
                            catch (Exception imageEx)
                            {
                                App.LogError(imageEx, nameof(LoadRandomCatGifAsync) + " display");
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error and keep current image
                App.LogError(ex, nameof(LoadRandomCatGifAsync));
            }
            finally
            {
                _isLoadingRandomCat = false;
            }
        }

        #endregion

        #region User Interaction

        /// <summary>
        /// Handles mouse clicks on the pet for drag repositioning.
        /// Allows the user to drag the pet to reposition it anywhere on screen.
        /// </summary>
        /// <param name="sender">The UI element that was clicked (Grid)</param>
        /// <param name="e">Mouse button event arguments</param>
        private void Pet_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                isDragging = true;
                this.DragMove(); // Use built-in drag functionality
                isDragging = false;
                
                // DO NOT restart automatic movement after dragging
                // User wants pet to stay where they place it
                // if (!timer.IsEnabled)
                // {
                //     timer.Start();
                // }
            }
        }

        #endregion

        #region Context Menu Event Handlers

        /// <summary>
        /// Context menu handler to load a new random cat GIF.
        /// Called when user selects "New Random Cat" from right-click menu.
        /// </summary>
        /// <param name="sender">The menu item that was clicked</param>
        /// <param name="e">Routed event arguments</param>
        private async void NewRandomCat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadRandomCatGifAsync();
            }
            catch (Exception ex)
            {
                // As a safety net, ensure any unexpected exception here doesn't crash the app
                System.Diagnostics.Debug.WriteLine($"Unhandled error in NewRandomCat_Click: {ex}");
#if DEBUG
                var detail = $"Failed to load a new random cat. Keeping the current one.\n\nDetails: {ex.Message}";
#else
                var detail = "Failed to load a new random cat. Keeping the current one.";
#endif
                MessageBox.Show(
                    detail,
                    "Desktop Pet",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Context menu handler to exit the application.
        /// Called when user selects "Exit" from right-click menu.
        /// </summary>
        /// <param name="sender">The menu item that was clicked</param>
        /// <param name="e">Routed event arguments</param>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop(); // Stop the animation timer
            // Dispose any kept stream
            try { _currentGifStream?.Dispose(); } catch { /* ignore */ }
            Application.Current.Shutdown(); // Close the application
        }


        #endregion
    }
}
