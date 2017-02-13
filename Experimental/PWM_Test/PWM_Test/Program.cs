using Microsoft.SPOT;
using Microsoft.SPOT.Input;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;


namespace PWM_Test
{
    public class Program : Microsoft.SPOT.Application
    {
        // Holds the object that represents the program's main window.
        private Window mainWindow;

        public static void Main()
        {
            // Instantiate the application class object.
            Program myApplication = new Program();

            // Create the program's main window.
            Window mainWindow = myApplication.CreateWindow();

            // Start the application
            myApplication.Run(mainWindow);
        }

        public Window CreateWindow()
        {
            // Create a window object and set its size to the
            // size of the display.
            mainWindow = new Window();
            mainWindow.Height = SystemMetrics.ScreenHeight;
            mainWindow.Width = SystemMetrics.ScreenWidth;

            // Create a single text control.
            Text text = new Text();

            text.Font = Resources.GetFont(Resources.FontResources.small);
            text.TextContent = "Hello, World";
            text.HorizontalAlignment =
                Microsoft.SPOT.Presentation.HorizontalAlignment.Center;
            text.VerticalAlignment =
                Microsoft.SPOT.Presentation.VerticalAlignment.Center;

            // Add the text control to the window.
            mainWindow.Child = text;

            // Set the window visibility to visible.
            mainWindow.Visibility = Visibility.Visible;

            // Attach the button focus to the window.
            Buttons.Focus(mainWindow);

            return mainWindow;
        }
    }
}