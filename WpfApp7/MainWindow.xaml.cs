using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace WpfApp7
{
    public partial class MainWindow : Window
    {
        private bool isDragging = false;
        private Point startMousePosition;
        private double startLeft, startTop;

        public MainWindow()
        {
            InitializeComponent();

            // Slider maximális méretének beállítása a kép mérete alapján
            BackgroundImage.Loaded += (s, e) =>
            {
                double maxSize = Math.Min(BackgroundImage.ActualWidth, BackgroundImage.ActualHeight);
                SizeSlider.Maximum = maxSize;
            };

            // Egér kattintás - mozgatás indítása
            MovableRect.MouseDown += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    isDragging = true;
                    startMousePosition = e.GetPosition(MainCanvas);
                    startLeft = Canvas.GetLeft(MovableRect);
                    startTop = Canvas.GetTop(MovableRect);
                    MovableRect.CaptureMouse();
                }
            };

            // Egér mozgása közben - mozgatás végrehajtása
            MovableRect.MouseMove += (s, e) =>
            {
                if (isDragging)
                {
                    Point currentMousePosition = e.GetPosition(MainCanvas);
                    double deltaX = currentMousePosition.X - startMousePosition.X;
                    double deltaY = currentMousePosition.Y - startMousePosition.Y;

                    double newLeft = startLeft + deltaX;
                    double newTop = startTop + deltaY;

                    // Képméret és négyzet méret figyelembevételével határokat szabunk
                    double imageLeft = Canvas.GetLeft(BackgroundImage);
                    double imageTop = Canvas.GetTop(BackgroundImage);
                    double imageRight = imageLeft + BackgroundImage.ActualWidth;
                    double imageBottom = imageTop + BackgroundImage.ActualHeight;
                    double rectSize = MovableRect.Width;

                    // Korlátozzuk a mozgást a képen belül
                    newLeft = Math.Max(imageLeft, Math.Min(newLeft, imageRight - rectSize));
                    newTop = Math.Max(imageTop, Math.Min(newTop, imageBottom - rectSize));

                    // Új pozíció beállítása
                    Canvas.SetLeft(MovableRect, newLeft);
                    Canvas.SetTop(MovableRect, newTop);
                }
            };

            // Egér felengedése - mozgatás befejezése
            MovableRect.MouseUp += (s, e) =>
            {
                isDragging = false;
                MovableRect.ReleaseMouseCapture();
            };

            // Slider változásának kezelése (négyzet méretének beállítása)
            SizeSlider.ValueChanged += (s, e) =>
            {
                double newSize = SizeSlider.Value;

                // Képszéleken belüli mozgás biztosítása
                double imageLeft = Canvas.GetLeft(BackgroundImage);
                double imageTop = Canvas.GetTop(BackgroundImage);
                double imageRight = imageLeft + BackgroundImage.ActualWidth;
                double imageBottom = imageTop + BackgroundImage.ActualHeight;

                // Jelenlegi pozíció
                double currentLeft = Canvas.GetLeft(MovableRect);
                double currentTop = Canvas.GetTop(MovableRect);

                // Középre igazítás, hogy ne mozduljon el hirtelen
                double centerX = currentLeft + (MovableRect.Width / 2);
                double centerY = currentTop + (MovableRect.Height / 2);

                double newLeft = centerX - (newSize / 2);
                double newTop = centerY - (newSize / 2);

                // Korlátozzuk, hogy ne lógjon ki a kép keretéből
                newLeft = Math.Max(imageLeft, Math.Min(newLeft, imageRight - newSize));
                newTop = Math.Max(imageTop, Math.Min(newTop, imageBottom - newSize));

                // Új méret és pozíció beállítása
                MovableRect.Width = newSize;
                MovableRect.Height = newSize;
                Canvas.SetLeft(MovableRect, newLeft);
                Canvas.SetTop(MovableRect, newTop);
            };
        }
    }
}
