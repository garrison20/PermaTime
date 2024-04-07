using System.ComponentModel;
using System.Diagnostics;
//using System.Drawing;
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
        private const int TIME_UPDATE_INTERVAL = 10; // ms
        private const double OPACITY_STEP = 0.01;
        private const double OPACITY_DEFAULT = 0.50;
        private const double PROGRESS_BAR_HEIGHT = 3;
        private const long OPACITY_TIME_CUTOFF = 1000; // ms

        // Background properties
        private double _progressBarYLoc;
        private double _bgOpacity = OPACITY_DEFAULT; // Starting background opacity must be default value
        private long _keyInputTime;
        private Rect _bgRect;
        private SolidColorBrush _bg = new SolidColorBrush(Colors.DarkSlateGray);
        private SolidColorBrush _pb = new SolidColorBrush(Colors.Magenta);

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

            // Key press event for changing background opacity
            KeyDown += MainWindow_KeyDown;

            // Background whose opacity we will change with keypresses
            _bgRect = new Rect(0, 0, Width, Height);

            // Set the progress bar y location in memory
            _progressBarYLoc = Height - PROGRESS_BAR_HEIGHT;
        }

        /// <summary>
        /// OnRender override draws the geometry of the text and optional highlight.
        /// </summary>
        /// <param name="drawingContext">Drawing context of the OutlineText control.</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            // Set (possibly) updated background opacity 
            _bg.Opacity = _bgOpacity;

            // Draw the background
            drawingContext.DrawRectangle(_bg, null, _bgRect);

            // Create the time text with outline
            FormattedText formattedText = new FormattedText(
                DateTime.Now.ToString("HH:mm"),
                CultureInfo.GetCultureInfo("en-us"),
                FlowDirection.LeftToRight,
                new Typeface(
                    new FontFamily("Arial"),
                    FontStyles.Normal,
                    FontWeights.ExtraBold,
                    FontStretches.Normal),
                30,
                Brushes.Black, // This brush does not matter since we use the geometry of the text.
                VisualTreeHelper.GetDpi(this).PixelsPerDip // Needed because https://stackoverflow.com/questions/45765980/formattedtext-formttedtext-is-obsolete-use-the-pixelsperdip-override
            );

            // Get the x/y vals needed to center the time text in the window
            double xLoc = (Width / 2) - (formattedText.Width / 2);
            double yLoc = (Height / 2) - (formattedText.Height / 2);

            // Build the geometry object that represents the text.
            Geometry textGeometry = formattedText.BuildGeometry(new Point(xLoc, yLoc));

            // Draw the time text
            drawingContext.DrawGeometry(Brushes.White, null, textGeometry);

            // Draw the opacity percentage bar if the last opacity change was within our time threshold
            if ((GetTimeMs() - _keyInputTime) < OPACITY_TIME_CUTOFF)
            {
                double progressBarWidth = Width * _bgOpacity;
                Rect opacityBar = new Rect(0, _progressBarYLoc, progressBarWidth, PROGRESS_BAR_HEIGHT);
                drawingContext.DrawRectangle(_pb, null, opacityBar);
            }
        }

        #region Events
        /// <summary>
        /// ESC key: Close the app
        /// Up key: Increase background opacity
        /// Down key: Decrease background opacity
        /// Back key: Revert background opacity to start value
        /// </summary>
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            else if (e.Key == Key.Up)
            {
                if ((_bgOpacity + OPACITY_STEP) <= 1)
                    _bgOpacity += OPACITY_STEP;

                _keyInputTime = GetTimeMs();
            }
            else if (e.Key == Key.Down)
            {
                if ((_bgOpacity - OPACITY_STEP) >= OPACITY_STEP)
                    _bgOpacity -= OPACITY_STEP;

                _keyInputTime = GetTimeMs();
            }
            else if (e.Key == Key.Back)
            {
                _bgOpacity = OPACITY_DEFAULT;

                _keyInputTime = GetTimeMs();
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
                    Dispatcher.Invoke(() => { InvalidateVisual(); }); // InvalidateVisual() forces OnRender to be called
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

        private long GetTimeMs()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }
    }
}