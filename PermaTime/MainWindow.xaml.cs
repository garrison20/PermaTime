using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace PermaTime
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int TIME_UPDATE_INTERVAL = 500;
        private const double OPACITY_STEP = 0.02;
        private const double OPACITY_DEFAULT = 0.50;

        private bool _isRunning = false;
        private bool _mouseWasDown = false;
        private Point _lastMouseScreenPos;
        private Thread _clockThread;

        public MainWindow()
        {
            InitializeComponent();

            // Keep window on top of all other windows
            Topmost = true;

            // Put the window in the center-top of the screen on startup
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = ((SystemParameters.PrimaryScreenWidth - Width) / 2);
            Top = 0;

            // Update the clock
            _clockThread = new Thread(UpdateTime);
            _clockThread.Start();

            // Mouse events for moving the window around
            MouseMove += MainWindow_MouseMove;
            MouseDoubleClick += MainWindow_MouseDoubleClick;

            KeyDown += MainWindow_KeyDown;
        }

        #region Events
        /// <summary>
        /// Close the window when ESC key is pressed.
        /// </summary>
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) 
            {
                Close();
            }
            else if (e.Key == Key.Up)
            {
                if ((Background.Opacity + OPACITY_STEP) <= 1)
                    Background.Opacity += OPACITY_STEP;
            }
            else if (e.Key == Key.Down)
            {
                if ((Background.Opacity - OPACITY_STEP) >= OPACITY_STEP)
                    Background.Opacity -= OPACITY_STEP;
            }
            else if (e.Key == Key.Back)
            {
                Background.Opacity = OPACITY_DEFAULT;
            }
        }

        /// <summary>
        /// Enables the user to move the window around the screen by clicking and dragging.
        /// </summary>
        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Movement is relative to mouse XY pos from last frame
                if (!_mouseWasDown)
                {
                    _mouseWasDown = true;
                    _lastMouseScreenPos = PointToScreen(Mouse.GetPosition(this));
                    return;
                }

                // Get difference between mouse XY pos from last frame and this frame,
                // then add it to window's current screen pos to update it.
                Point prevMouseScreenPos = _lastMouseScreenPos;
                _lastMouseScreenPos = PointToScreen(Mouse.GetPosition(this));

                Left = Left + (_lastMouseScreenPos.X - prevMouseScreenPos.X);
                Top = Top + (_lastMouseScreenPos.Y - prevMouseScreenPos.Y);
            }
            else
            {
                _mouseWasDown = false;
            }
        }

        /// <summary>
        /// Send window back to middle-top of screen.
        /// </summary>
        private void MainWindow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Left = ((SystemParameters.PrimaryScreenWidth - Width) / 2);
            Top = 0;
        }
        #endregion Events

        /// <summary>
        /// Update the time label every TIME_UPDATE_INTERVAL milliseconds.
        /// </summary>
        private void UpdateTime()
        {
            _isRunning = true;
            while (_isRunning)
            {
                try
                {
                    Dispatcher.Invoke(() => { LblTime.Content = DateTime.Now.ToString("HH:mm"); });
                }
                catch (TaskCanceledException e)
                {
                    Debug.WriteLine(e.ToString());
                }

                Thread.Sleep(TIME_UPDATE_INTERVAL);
            }
        }

        /// <summary>
        /// Stop the time update thread on shutdown.
        /// </summary>
        private void Close_App(object sender, CancelEventArgs e)
        {
            _isRunning = false;
            _clockThread?.Join();
        }
    }
}