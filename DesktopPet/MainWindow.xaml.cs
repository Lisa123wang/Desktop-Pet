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
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

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
        /// Context menu handler to exit the application.
        /// Called when user selects "Exit" from right-click menu.
        /// </summary>
        /// <param name="sender">The menu item that was clicked</param>
        /// <param name="e">Routed event arguments</param>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop(); // Stop the animation timer
            Application.Current.Shutdown(); // Close the application
        }

        #endregion
    }
}
