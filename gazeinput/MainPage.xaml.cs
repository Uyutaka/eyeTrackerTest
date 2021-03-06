﻿//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using System;
using Windows.Devices.Input.Preview;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.Foundation;
using System.Collections.Generic;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Popups;
using System.IO;
using System.Diagnostics;
namespace gazeinput
{
    public sealed partial class MainPage : Page
    {
        /*
        // My Memo
        eyeGazePositionEllipse is user's gaze point
        GazeRadialProgressBar is bar
        Width
        Height
        */
        // TODO store num of enter/exit

        private const int MAX_TRIAL = 10;

        /// <summary>
        /// Reference to the user's eyes and head as detected
        /// by the eye-tracking device.
        /// </summary>
        private GazeInputSourcePreview gazeInputSource;

        /// <summary>
        /// Dynamic store of eye-tracking devices.
        /// </summary>
        /// <remarks>
        /// Receives event notifications when a device is added, removed, 
        /// or updated after the initial enumeration.
        /// </remarks>
        private GazeDeviceWatcherPreview gazeDeviceWatcher;

        /// <summary>
        /// Eye-tracking device counter.
        /// </summary>
        private int deviceCounter = 0;

        /// <summary>
        /// Timer for gaze focus on RadialProgressBar.
        /// </summary>
        DispatcherTimer timerGaze = new DispatcherTimer();

        /// <summary>
        /// Tracker used to prevent gaze timer restarts.
        /// </summary>
        bool timerStarted = false;

        /// <For recoding>
        private int numTrial = 0;
        private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        private double[] intervalArr = new double[MAX_TRIAL];
        private int[,] targetPositionArr = new int[MAX_TRIAL, 2];
        private int[] targetSizeArr = new int[] { 500, 500, 400, 400, 300, 300, 200, 200, 100, 100 };



        /// <summary>
        /// Initialize the app.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Override of OnNavigatedTo page event starts GazeDeviceWatcher.
        /// </summary>
        /// <param name="e">Event args for the NavigatedTo event</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Start listening for device events on navigation to eye-tracking page.
            StartGazeDeviceWatcher();
        }

        /// <summary>
        /// Override of OnNavigatedFrom page event stops GazeDeviceWatcher.
        /// </summary>
        /// <param name="e">Event args for the NavigatedFrom event</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            // Stop listening for device events on navigation from eye-tracking page.
            StopGazeDeviceWatcher();
        }

        /// <summary>
        /// Tick handler for gaze focus timer.
        /// </summary>
        /// <param name="sender">Source of the gaze entered event</param>
        /// <param name="e">Event args for the gaze entered event</param>
        private void TimerGaze_Tick(object sender, object e)
        {
            // Increment progress bar.
            GazeRadialProgressBar.Value += 1;

            // If progress bar reaches maximum value, reset and relocate.
            if (GazeRadialProgressBar.Value == 100)
            {
                if (numTrial < MAX_TRIAL)
                {
                    ManageStopWatch();
                    ShowTrialResult();
                    SetGazeTargetLocation();
                    numTrial++;
                }
                else {
                    sw.Stop();
                    ShowDialog();
                }
            }
        }
        private void ShowTrialResult() {


            string result = "";

            result += "Target\n";
            result += numTrial.ToString() + "\tsize: " + targetSizeArr[numTrial];
            result += "\t(X,Y) = (" + targetPositionArr[numTrial, 0].ToString() + ", " + targetPositionArr[numTrial, 1].ToString() + ")\n";

            result += "interval\n";
            result += "\t" + intervalArr[numTrial].ToString() + " sec\n";
            Debug.WriteLine(result);

        }
        private void ManageStopWatch()
        {
            sw.Stop();
            intervalArr[numTrial] = sw.Elapsed.TotalSeconds;
            sw.Reset();
        }

        private async void ShowDialog()
        {
            string result = "";
            result += "Target\n";
            for (int i = 0; i < MAX_TRIAL; i++) {
                result += i.ToString() + "\tsize: " + targetSizeArr[i] + "\n";
                result += "\t(X,Y) = (" + targetPositionArr[i, 0].ToString() + ", " + targetPositionArr[i,1].ToString() + ")\n";
            }
            result += "interval\n";
            for (int i = 0; i < MAX_TRIAL; i++) {
                result += i.ToString();
                result += "\t" + intervalArr[i].ToString() + " sec\n";
            }
            var messageDialog = new MessageDialog(result);
            await messageDialog.ShowAsync();
        }

        /// <summary>
        /// Set/reset the screen location of the progress bar.
        /// </summary>
        private void SetGazeTargetLocation()
        {
            // Setup target size
            GazeRadialProgressBar.Width = targetSizeArr[numTrial];
            GazeRadialProgressBar.Height = targetSizeArr[numTrial];

            // Ensure the gaze timer restarts on new progress bar location.
            timerGaze.Stop();
            timerStarted = false;

            // Get the bounding rectangle of the app window.
            Rect appBounds = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().VisibleBounds;

            // Translate transform for moving progress bar.
            TranslateTransform translateTarget = new TranslateTransform();

            // Calculate random location within gaze canvas.
            Random random = new Random();
            int randomX = 
                random.Next(
                    0, 
                    (int)appBounds.Width - (int)GazeRadialProgressBar.Width);
            int randomY = 
                random.Next(
                    0, 
                    (int)appBounds.Height - (int)GazeRadialProgressBar.Height - (int)Header.ActualHeight);
            targetPositionArr[numTrial, 0] = randomX;
            targetPositionArr[numTrial, 1] = randomY;

            translateTarget.X = randomX;
            translateTarget.Y = randomY;

            GazeRadialProgressBar.RenderTransform = translateTarget;

            // Show progress bar.
            GazeRadialProgressBar.Visibility = Visibility.Visible;
            GazeRadialProgressBar.Value = 0;
            sw.Start();
        }

        /// <summary>
        /// GazeEntered handler.
        /// </summary>
        /// <param name="sender">Source of the gaze entered event</param>
        /// <param name="e">Event args for the gaze entered event</param>
        private void GazeEntered(
            GazeInputSourcePreview sender, 
            GazeEnteredPreviewEventArgs args)
        {
            // Show ellipse representing gaze point.
            eyeGazePositionEllipse.Visibility = Visibility.Visible;

            // Mark the event handled.
            args.Handled = true;
        }

        /// <summary>
        /// GazeExited handler.
        /// Call DisplayRequest.RequestRelease to conclude the 
        /// RequestActive called in GazeEntered.
        /// </summary>
        /// <param name="sender">Source of the gaze exited event</param>
        /// <param name="e">Event args for the gaze exited event</param>
        private void GazeExited(
            GazeInputSourcePreview sender, 
            GazeExitedPreviewEventArgs args)
        {
            // Hide gaze tracking ellipse.
            eyeGazePositionEllipse.Visibility = Visibility.Collapsed;

            // Mark the event handled.
            args.Handled = true;
        }

        /// <summary>
        /// GazeMoved handler translates the ellipse on the canvas to reflect gaze point.
        /// </summary>
        /// <param name="sender">Source of the gaze moved event</param>
        /// <param name="e">Event args for the gaze moved event</param>
        private void GazeMoved(GazeInputSourcePreview sender, GazeMovedPreviewEventArgs args)
        {
            // Update the position of the ellipse corresponding to gaze point.
            if (args.CurrentPoint.EyeGazePosition != null)
            {
                double gazePointX = args.CurrentPoint.EyeGazePosition.Value.X;
                double gazePointY = args.CurrentPoint.EyeGazePosition.Value.Y;

                double ellipseLeft = 
                    gazePointX - 
                    (eyeGazePositionEllipse.Width / 2.0f);
                double ellipseTop = 
                    gazePointY - 
                    (eyeGazePositionEllipse.Height / 2.0f) - 
                    (int)Header.ActualHeight;

                // Translate transform for moving gaze ellipse.
                TranslateTransform translateEllipse = new TranslateTransform
                {
                    X = ellipseLeft,
                    Y = ellipseTop
                };

                eyeGazePositionEllipse.RenderTransform = translateEllipse;

                // The gaze point screen location.
                Point gazePoint = new Point(gazePointX, gazePointY);

                // Basic hit test to determine if gaze point is on progress bar.
                bool hitRadialProgressBar = 
                    DoesElementContainPoint(
                        gazePoint, 
                        GazeRadialProgressBar.Name, 
                        GazeRadialProgressBar); 

                // Use progress bar thickness for visual feedback.
                if (hitRadialProgressBar)
                {
                    GazeRadialProgressBar.Thickness = 10;
                }
                else
                {
                    GazeRadialProgressBar.Thickness = 4;
                }

                // Mark the event handled.
                args.Handled = true;
            }
        }

        /// <summary>
        /// Return whether the gaze point is over the progress bar.
        /// </summary>
        /// <param name="gazePoint">The gaze point screen location</param>
        /// <param name="elementName">The progress bar name</param>
        /// <param name="uiElement">The progress bar UI element</param>
        /// <returns></returns>
        private bool DoesElementContainPoint(
            Point gazePoint, string elementName, UIElement uiElement)
        {
            // Use entire visual tree of progress bar.
            IEnumerable<UIElement> elementStack = 
              VisualTreeHelper.FindElementsInHostCoordinates(gazePoint, uiElement, true);
            foreach (UIElement item in elementStack)
            {
                //Cast to FrameworkElement and get element name.
                if (item is FrameworkElement feItem)
                {
                    if (feItem.Name.Equals(elementName))
                    {
                        if (!timerStarted)
                        {
                            // Start gaze timer if gaze over element.
                            timerGaze.Start();
                            timerStarted = true;
                        }
                        return true;
                    }
                }
            }

            // Stop gaze timer and reset progress bar if gaze leaves element.
            timerGaze.Stop();
            GazeRadialProgressBar.Value = 0;
            timerStarted = false;
            return false;
        }

        /// <summary>
        /// Start gaze watcher and declare watcher event handlers.
        /// </summary>
        private void StartGazeDeviceWatcher()
        {
            if (gazeDeviceWatcher == null)
            {
                gazeDeviceWatcher = GazeInputSourcePreview.CreateWatcher();
                gazeDeviceWatcher.Added += this.DeviceAdded;
                gazeDeviceWatcher.Updated += this.DeviceUpdated;
                gazeDeviceWatcher.Removed += this.DeviceRemoved;
                gazeDeviceWatcher.Start();
            }
        }

        /// <summary>
        /// Shut down gaze watcher and stop listening for events.
        /// </summary>
        private void StopGazeDeviceWatcher()
        {
            if (gazeDeviceWatcher != null)
            {
                gazeDeviceWatcher.Stop();
                gazeDeviceWatcher.Added -= this.DeviceAdded;
                gazeDeviceWatcher.Updated -= this.DeviceUpdated;
                gazeDeviceWatcher.Removed -= this.DeviceRemoved;
                gazeDeviceWatcher = null;
            }
        }

        /// <summary>
        /// Eye-tracking device connected (added, or available when watcher is initialized).
        /// </summary>
        /// <param name="sender">Source of the device added event</param>
        /// <param name="e">Event args for the device added event</param>
        private void DeviceAdded(GazeDeviceWatcherPreview source, 
            GazeDeviceWatcherAddedPreviewEventArgs args)
        {
            if (IsSupportedDevice(args.Device))
            {
                deviceCounter++;
                TrackerCounter.Text = deviceCounter.ToString();
            }
            // Set up gaze tracking.
            TryEnableGazeTrackingAsync(args.Device);
        }

        /// <summary>
        /// Initial device state might be uncalibrated, 
        /// but device was subsequently calibrated.
        /// </summary>
        /// <param name="sender">Source of the device updated event</param>
        /// <param name="e">Event args for the device updated event</param>
        private void DeviceUpdated(GazeDeviceWatcherPreview source,
            GazeDeviceWatcherUpdatedPreviewEventArgs args)
        {
            // Set up gaze tracking.
            TryEnableGazeTrackingAsync(args.Device);
        }

        /// <summary>
        /// Handles disconnection of eye-tracking devices.
        /// </summary>
        /// <param name="sender">Source of the device removed event</param>
        /// <param name="e">Event args for the device removed event</param>
        private void DeviceRemoved(GazeDeviceWatcherPreview source,
            GazeDeviceWatcherRemovedPreviewEventArgs args)
        {
            // Decrement gaze device counter and remove event handlers.
            if (IsSupportedDevice(args.Device))
            {
                deviceCounter--;
                TrackerCounter.Text = deviceCounter.ToString();

                if (deviceCounter == 0)
                {
                    gazeInputSource.GazeEntered -= this.GazeEntered;
                    gazeInputSource.GazeMoved -= this.GazeMoved;
                    gazeInputSource.GazeExited -= this.GazeExited;
                }
            }
        }

        /// <summary>
        /// Initialize gaze tracking.
        /// </summary>
        /// <param name="gazeDevice"></param>
        private async void TryEnableGazeTrackingAsync(GazeDevicePreview gazeDevice)
        {
            // If eye-tracking device is ready, declare event handlers and start tracking.
            if (IsSupportedDevice(gazeDevice))
            {
                timerGaze.Interval = new TimeSpan(0, 0, 0, 0, 20);
                timerGaze.Tick += TimerGaze_Tick;

                SetGazeTargetLocation();

                // This must be called from the UI thread.
                gazeInputSource = GazeInputSourcePreview.GetForCurrentView();

                gazeInputSource.GazeEntered += GazeEntered;
                gazeInputSource.GazeMoved += GazeMoved;
                gazeInputSource.GazeExited += GazeExited;
            }
            // Notify if device calibration required.
            else if (gazeDevice.ConfigurationState ==
                     GazeDeviceConfigurationStatePreview.UserCalibrationNeeded ||
                     gazeDevice.ConfigurationState ==
                     GazeDeviceConfigurationStatePreview.ScreenSetupNeeded)
            {
                // Device isn't calibrated, so invoke the calibration handler.
                System.Diagnostics.Debug.WriteLine(
                    "Your device needs to calibrate. Please wait for it to finish.");
                await gazeDevice.RequestCalibrationAsync();
            }
            // Notify if device calibration underway.
            else if (gazeDevice.ConfigurationState == 
                GazeDeviceConfigurationStatePreview.Configuring)
            {
                // Device is currently undergoing calibration.  
                // A device update is sent when calibration complete.
                System.Diagnostics.Debug.WriteLine(
                    "Your device is being configured. Please wait for it to finish"); 
            }
            // Device is not viable.
            else if (gazeDevice.ConfigurationState == GazeDeviceConfigurationStatePreview.Unknown)
            {
                // Notify if device is in unknown state.  
                // Reconfigure/recalbirate the device.  
                System.Diagnostics.Debug.WriteLine(
                    "Your device is not ready. Please set up your device or reconfigure it."); 
            }
        }

        /// <summary>
        /// Check if eye-tracking device is viable.
        /// </summary>
        /// <param name="gazeDevice">Reference to eye-tracking device.</param>
        /// <returns>True, if device is viable; otherwise, false.</returns>
        private bool IsSupportedDevice(GazeDevicePreview gazeDevice)
        {
            TrackerState.Text = gazeDevice.ConfigurationState.ToString();
            return (gazeDevice.CanTrackEyes &&
                     gazeDevice.ConfigurationState == 
                     GazeDeviceConfigurationStatePreview.Ready);
        }
    }
}
