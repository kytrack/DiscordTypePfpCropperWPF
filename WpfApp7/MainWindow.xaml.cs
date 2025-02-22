using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using Microsoft.Win32;

namespace WpfApp7
{
    public partial class MainWindow : Window
    {
        private bool isDragging = false;
        private Point startMousePosition;
        private double startX, startY;
        private double imageWidth, imageHeight;
        private double selectionSize = 400;

        public MainWindow()
        {
            InitializeComponent();
            LoadImageOnStartup();

            BackgroundImage.Loaded += (s, e) =>
            {
                imageWidth = BackgroundImage.ActualWidth;
                imageHeight = BackgroundImage.ActualHeight;

                double requiredZoomWidth = selectionSize / imageWidth;
                double requiredZoomHeight = selectionSize / imageHeight;
                double minZoom = Math.Max(requiredZoomWidth, requiredZoomHeight);

                ZoomSlider.Minimum = minZoom;
                ZoomSlider.Value = minZoom;

                RestrictImageMovement();
            };

            BackgroundImage.MouseDown += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    isDragging = true;
                    startMousePosition = e.GetPosition(MainCanvas);
                    startX = ImageTranslateTransform.X;
                    startY = ImageTranslateTransform.Y;
                    BackgroundImage.CaptureMouse();
                }
            };

            BackgroundImage.MouseMove += (s, e) =>
            {
                if (isDragging)
                {
                    Point currentMousePosition = e.GetPosition(MainCanvas);
                    double deltaX = currentMousePosition.X - startMousePosition.X;
                    double deltaY = currentMousePosition.Y - startMousePosition.Y;

                    double newX = startX + deltaX;
                    double newY = startY + deltaY;

                    RestrictImageMovement(ref newX, ref newY);

                    ImageTranslateTransform.X = newX;
                    ImageTranslateTransform.Y = newY;
                }
            };

            BackgroundImage.MouseUp += (s, e) =>
            {
                isDragging = false;
                BackgroundImage.ReleaseMouseCapture();
            };

            ZoomSlider.ValueChanged += (s, e) =>
            {
                double zoomFactor = ZoomSlider.Value;
                ImageScaleTransform.ScaleX = zoomFactor;
                ImageScaleTransform.ScaleY = zoomFactor;
                RestrictImageMovement();
            };
        }

        private void LoadImageOnStartup()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Képfájlok|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Válasszon ki egy képet"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadSelectedImage(openFileDialog.FileName);
            }
            else
            {
                MessageBox.Show("Nincs kiválasztva kép!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
            }
        }

        private void LoadSelectedImage(string filePath)
        {
            var bitmap = new BitmapImage(new Uri(filePath));
            BackgroundImage.Source = bitmap;

            BackgroundImage.Loaded += (s, e) =>
            {
                imageWidth = BackgroundImage.ActualWidth;
                imageHeight = BackgroundImage.ActualHeight;
                InitializeZoomAndPosition();
            };
        }

        private void InitializeZoomAndPosition()
        {
            ImageTranslateTransform.X = (550 - imageWidth) / 2;
            ImageTranslateTransform.Y = (400 - imageHeight) / 2;
            ZoomSlider.Minimum = 0.1;
            ZoomSlider.Maximum = 5;
            ZoomSlider.Value = 1;
        }

        private void RestrictImageMovement()
        {
            double zoomFactor = ZoomSlider.Value;
            double currentWidth = imageWidth * zoomFactor;
            double currentHeight = imageHeight * zoomFactor;

            double selectionLeft = Canvas.GetLeft(SelectionRect);
            double selectionTop = Canvas.GetTop(SelectionRect);

            double minX = selectionLeft + selectionSize - currentWidth;
            double maxX = selectionLeft;
            double minY = selectionTop + selectionSize - currentHeight;
            double maxY = selectionTop;

            if (currentWidth < selectionSize)
            {
                minX = maxX = selectionLeft + (selectionSize - currentWidth) / 2;
            }
            if (currentHeight < selectionSize)
            {
                minY = maxY = selectionTop + (selectionSize - currentHeight) / 2;
            }

            double currentX = ImageTranslateTransform.X;
            double currentY = ImageTranslateTransform.Y;

            ImageTranslateTransform.X = Math.Max(minX, Math.Min(maxX, currentX));
            ImageTranslateTransform.Y = Math.Max(minY, Math.Min(maxY, currentY));
        }

        private void RestrictImageMovement(ref double x, ref double y)
        {
            double zoomFactor = ZoomSlider.Value;
            double currentWidth = imageWidth * zoomFactor;
            double currentHeight = imageHeight * zoomFactor;

            double selectionLeft = Canvas.GetLeft(SelectionRect);
            double selectionTop = Canvas.GetTop(SelectionRect);

            double minX = selectionLeft + selectionSize - currentWidth;
            double maxX = selectionLeft;
            double minY = selectionTop + selectionSize - currentHeight;
            double maxY = selectionTop;

            if (currentWidth < selectionSize)
            {
                minX = maxX = selectionLeft + (selectionSize - currentWidth) / 2;
            }
            if (currentHeight < selectionSize)
            {
                minY = maxY = selectionTop + (selectionSize - currentHeight) / 2;
            }

            x = Math.Max(minX, Math.Min(maxX, x));
            y = Math.Max(minY, Math.Min(maxY, y));
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var transform = new Matrix(ImageScaleTransform.ScaleX, 0, 0, ImageScaleTransform.ScaleY,
                                     ImageTranslateTransform.X, ImageTranslateTransform.Y);

            double circleX = Canvas.GetLeft(SelectionRect) + SelectionRect.Width / 2;
            double circleY = Canvas.GetTop(SelectionRect) + SelectionRect.Height / 2;
            double radius = SelectionRect.Width / 2;

            transform.Invert();
            Point centerInImage = transform.Transform(new Point(circleX, circleY));

            Rect cropRect = new Rect(
                centerInImage.X - radius / ImageScaleTransform.ScaleX,
                centerInImage.Y - radius / ImageScaleTransform.ScaleY,
                SelectionRect.Width / ImageScaleTransform.ScaleX,
                SelectionRect.Height / ImageScaleTransform.ScaleY);

            BitmapImage source = new BitmapImage(new Uri(BackgroundImage.Source.ToString()));
            CroppedBitmap cropped = new CroppedBitmap(source, new Int32Rect(
                (int)cropRect.X, (int)cropRect.Y, (int)cropRect.Width, (int)cropRect.Height));

            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                dc.DrawImage(cropped, new Rect(0, 0, radius * 2, radius * 2));
                dc.DrawGeometry(Brushes.Transparent, null,
                    new EllipseGeometry(new Point(radius, radius), radius, radius));
            }

            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)(radius * 2), (int)(radius * 2), 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);

            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "PNG fájl|*.png";
            if (saveDialog.ShowDialog() == true)
            {
                using (FileStream stream = new FileStream(saveDialog.FileName, FileMode.Create))
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                    encoder.Save(stream);
                }
            }
        }
    }
}