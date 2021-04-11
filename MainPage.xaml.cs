//  ---------------------------------------------------------------------------------
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
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// Begin "Step 2: Use InkCanvas to support basic inking"
////using directives for inking functionality.
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Windows.UI.Xaml.Shapes;
using Windows.Storage.Streams;

using System.Numerics;
using Windows.UI;
using Windows.Foundation;
using Windows.UI.Core;
using System.Linq;

// End "Step 2: Use InkCanvas to support basic inking"

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace GettingStarted_Ink
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {// Icon for escfritura táctil tool button.
        Symbol TouchWritingIcon = (Symbol)0xED5F;
        // Icon for custom selection tool button.
        Symbol SelectIcon = (Symbol)0xEF20;

        // Stroke selection tool.
        private Polyline lasso;
        // Stroke selection area.
        private Rect boundingRect;

        //Symbol symTouchInput = (Symbol)0xEC87;
        //Symbol symCompleted = (Symbol)0xE930;
        //const string toolTipStart = "Click to draw annotations";
        //const string toolTipComplete = "Click to end drawing";
        //string toggleToolTipText = toolTipStart;

        double xPegar = 0;
        double yPegar = 0;

        // Begin "Step 5: Support handwriting recognition"
        InkAnalyzer analyzerText = new InkAnalyzer();
        IReadOnlyList<InkStroke> strokesText = null;
        InkAnalysisResult resultText = null;
        IReadOnlyList<IInkAnalysisNode> words = null;
        // End "Step 5: Support handwriting recognition"

        // Begin "Step 6: Recognize shapes"
        InkAnalyzer analyzerShape = new InkAnalyzer();
        IReadOnlyList<InkStroke> strokesShape = null;
        InkAnalysisResult resultShape = null;
        // End "Step 6: Recognize shapes"
        
        public MainPage()
        {
            this.InitializeComponent();

            // Begin "Step 3: Support inking with touch and mouse"
            inkCanvas.InkPresenter.InputDeviceTypes =
                Windows.UI.Core.CoreInputDeviceTypes.Mouse |
            //    Windows.UI.Core.CoreInputDeviceTypes.Touch |
                Windows.UI.Core.CoreInputDeviceTypes.Pen;
            // End "Step 3: Support inking with touch and mouse"


            // Listen for new ink or erase strokes to clean up selection UI.
            inkCanvas.InkPresenter.StrokeInput.StrokeStarted +=
                StrokeInput_StrokeStarted;
            inkCanvas.InkPresenter.StrokesErased +=
                InkPresenter_StrokesErased;
            inkCanvas.InkPresenter.InputConfiguration.IsPrimaryBarrelButtonInputEnabled = true;//mirar esto
            // When the user finished to draw something on the InkCanvas
          //  inkCanvas.InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;
        }
        private void InkPresenter_StrokesCollected(
    Windows.UI.Input.Inking.InkPresenter sender,
    Windows.UI.Input.Inking.InkStrokesCollectedEventArgs args)
        {
            InkStroke stroke = inkCanvas.InkPresenter.StrokeContainer.GetStrokes().Last();

            // Action 1 = We use a function that we will implement just after to create the XAML Line
            Line line = ConvertStrokeToXAMLLine(stroke);
            // Action 2 = We add the Line in the second Canvas
            canvas.Children.Add(line);

            // We delete the InkStroke from the InkCanvas
            stroke.Selected = true;
            inkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
        }

        private Line ConvertStrokeToXAMLLine(InkStroke stroke)
        {
            var line = new Line();
            line.Stroke = new SolidColorBrush(Windows.UI.Colors.Green);
            line.StrokeThickness = 6;
            // The origin = (X1, Y1)
            line.X1 = stroke.GetInkPoints().First().Position.X;
            line.Y1 = stroke.GetInkPoints().First().Position.Y;
            // The end = (X2, Y2)
            line.X2 = stroke.GetInkPoints().Last().Position.X;
            line.Y2 = stroke.GetInkPoints().Last().Position.Y;

            return line;
        }
        //paso 8: listener boton escritura táctil
        private void CustomToggle_Click(object sender, RoutedEventArgs e)
        {
            if (toggleButton.IsChecked == true)
            {
                inkCanvas.InkPresenter.InputDeviceTypes |= Windows.UI.Core.CoreInputDeviceTypes.Touch;
            }
            else
            {
                inkCanvas.InkPresenter.InputDeviceTypes &= ~Windows.UI.Core.CoreInputDeviceTypes.Touch;
            }
        }
        //end paso 8
        // Begin "Step 5: Support handwriting recognition"
        private async void recognizeText_ClickAsync(object sender, RoutedEventArgs e)
        {
            strokesText = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            // Ensure an ink stroke is present.
            if (strokesText.Count > 0)
            {
                analyzerText.AddDataForStrokes(strokesText);

                // Force analyzer to process strokes as handwriting.
                foreach (var stroke in strokesText)
                {
                    analyzerText.SetStrokeDataKind(stroke.Id, InkAnalysisStrokeKind.Writing);
                }

                // Clear recognition results string.
                recognitionResult.Text = "";

                resultText = await analyzerText.AnalyzeAsync();

                if (resultText.Status == InkAnalysisStatus.Updated)
                {
                    var text = analyzerText.AnalysisRoot.RecognizedText;
                    words = analyzerText.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkWord);
                    foreach (var word in words)
                    {
                        InkAnalysisInkWord concreteWord = (InkAnalysisInkWord)word;
                        foreach (string s in concreteWord.TextAlternates)
                        {
                            recognitionResult.Text += s + " ";
                        }
                        recognitionResult.Text += " / ";
                    }
                }
                analyzerText.ClearDataForAllStrokes();
            }
        }
        // End "Step 5: Support handwriting recognition"

        // Begin "Step 6: Recognize shapes"
        private async void recognizeShape_ClickAsync(object sender, RoutedEventArgs e)
        {
            strokesShape = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();

            if (strokesShape.Count > 0)
            {
                analyzerShape.AddDataForStrokes(strokesShape);

                resultShape = await analyzerShape.AnalyzeAsync();

                if (resultShape.Status == InkAnalysisStatus.Updated)
                {
                    var drawings = analyzerShape.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing);

                    foreach (var drawing in drawings)
                    {
                        var shape = (InkAnalysisInkDrawing)drawing;
                        if (shape.DrawingKind == InkAnalysisDrawingKind.Drawing)
                        {
                            // Catch and process unsupported shapes (lines and so on) here.
                        }
                        else
                        {
                            // Process recognized shapes here.
                            if (shape.DrawingKind == InkAnalysisDrawingKind.Circle || shape.DrawingKind == InkAnalysisDrawingKind.Ellipse)
                            {
                                DrawEllipse(shape);
                            }
                            else
                            {
                                DrawPolygon(shape);
                            }
                            foreach (var strokeId in shape.GetStrokeIds())
                            {
                                var stroke = inkCanvas.InkPresenter.StrokeContainer.GetStrokeById(strokeId);
                                stroke.Selected = true;
                            }
                        }
                        analyzerShape.RemoveDataForStrokes(shape.GetStrokeIds());
                    }
                    inkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
                }
            }
        }

        private void DrawEllipse(InkAnalysisInkDrawing shape)
        {
            var points = shape.Points;
            Ellipse ellipse = new Ellipse();
            ellipse.Width = Math.Sqrt((points[0].X - points[2].X) * (points[0].X - points[2].X) +
                 (points[0].Y - points[2].Y) * (points[0].Y - points[2].Y));
            ellipse.Height = Math.Sqrt((points[1].X - points[3].X) * (points[1].X - points[3].X) +
                 (points[1].Y - points[3].Y) * (points[1].Y - points[3].Y));

            var rotAngle = Math.Atan2(points[2].Y - points[0].Y, points[2].X - points[0].X);
            RotateTransform rotateTransform = new RotateTransform();
            rotateTransform.Angle = rotAngle * 180 / Math.PI;
            rotateTransform.CenterX = ellipse.Width / 2.0;
            rotateTransform.CenterY = ellipse.Height / 2.0;

            TranslateTransform translateTransform = new TranslateTransform();
            translateTransform.X = shape.Center.X - ellipse.Width / 2.0;
            translateTransform.Y = shape.Center.Y - ellipse.Height / 2.0;

            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(rotateTransform);
            transformGroup.Children.Add(translateTransform);
            ellipse.RenderTransform = transformGroup;

            var brush = new SolidColorBrush(Windows.UI.ColorHelper.FromArgb(255, 0, 0, 255));
            ellipse.Stroke = brush;
            ellipse.StrokeThickness = 2;
            canvas.Children.Add(ellipse);
        }

        private void DrawPolygon(InkAnalysisInkDrawing shape)
        {
            var points = shape.Points;
            Polygon polygon = new Polygon();

            foreach (var point in points)
            {
                polygon.Points.Add(point);
            }

            var brush = new SolidColorBrush(Windows.UI.ColorHelper.FromArgb(255, 0, 0, 255));
            polygon.Stroke = brush;
            polygon.StrokeThickness = 2;
            canvas.Children.Add(polygon);
        }
        // End "Step 6: Recognize shapes"

        // Begin "Step 7: Save and load ink"
        private async void buttonSave_ClickAsync(object sender, RoutedEventArgs e)
        {
            // Get all strokes on the InkCanvas.
            IReadOnlyList<InkStroke> currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();

            if (currentStrokes.Count > 0)
            {
                // Use a file picker to identify ink file.
                Windows.Storage.Pickers.FileSavePicker savePicker =
                    new Windows.Storage.Pickers.FileSavePicker();
                savePicker.SuggestedStartLocation =
                    Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add(
                    "GIF with embedded ISF",
                    new List<string>() { ".gif" });
                savePicker.DefaultFileExtension = ".gif";
                savePicker.SuggestedFileName = "InkSample";

                // Show the file picker.
                Windows.Storage.StorageFile file =
                    await savePicker.PickSaveFileAsync();
                // When selected, picker returns a reference to the file.
                if (file != null)
                {
                    // Prevent updates to the file until updates are 
                    // finalized with call to CompleteUpdatesAsync.
                    Windows.Storage.CachedFileManager.DeferUpdates(file);
                    // Open a file stream for writing.
                    IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
                    // Write the ink strokes to the output stream.
                    using (IOutputStream outputStream = stream.GetOutputStreamAt(0))
                    {
                        await inkCanvas.InkPresenter.StrokeContainer.SaveAsync(outputStream);
                        await outputStream.FlushAsync();
                    }
                    stream.Dispose();

                    // Finalize write so other apps can update file.
                    Windows.Storage.Provider.FileUpdateStatus status =
                        await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);

                    if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
                    {
                        // File saved.
                    }
                    else
                    {
                        // File couldn't be saved.
                    }
                }
                // User selects Cancel and picker returns null.
                else
                {
                    // Operation cancelled.
                }
            }
        }

        private async void buttonLoad_ClickAsync(object sender, RoutedEventArgs e)
        {
            // Use a file picker to identify ink file.
            Windows.Storage.Pickers.FileOpenPicker openPicker =
                new Windows.Storage.Pickers.FileOpenPicker();
            openPicker.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".gif");
            // Show the file picker.
            Windows.Storage.StorageFile file = await openPicker.PickSingleFileAsync();
            // When selected, picker returns a reference to the file.
            if (file != null)
            {
                // Open a file stream for reading.
                IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                // Read from file.
                using (var inputStream = stream.GetInputStreamAt(0))
                {
                    await inkCanvas.InkPresenter.StrokeContainer.LoadAsync(inputStream);
                }
                stream.Dispose();
            }
            // User selects Cancel and picker returns null.
            else
            {
                // Operation cancelled.
            }
        }
        // End "Step 7: Save and load ink"

        //step 9 

        private void customToolButton_Click(object sender, RoutedEventArgs e)
        {
            // By default, the InkPresenter processes input modified by 
            // a secondary affordance (pen barrel button, right mouse 
            // button, or similar) as ink.
            // To pass through modified input to the app for custom processing 
            // on the app UI thread instead of the background ink thread, set 
            // InputProcessingConfiguration.RightDragAction to LeaveUnprocessed.
            //inkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction =
              //  InkInputRightDragAction.LeaveUnprocessed;

            // Listen for unprocessed pointer events from modified input.
            // The input is used to provide selection functionality.
            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed +=
                UnprocessedInput_PointerPressed;
            inkCanvas.InkPresenter.UnprocessedInput.PointerMoved +=
                UnprocessedInput_PointerMoved;
            inkCanvas.InkPresenter.UnprocessedInput.PointerReleased +=
                UnprocessedInput_PointerReleased;
        }

        // Handle new ink or erase strokes to clean up selection UI.
        private void StrokeInput_StrokeStarted(
            InkStrokeInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            ClearSelection();
        }

        private void InkPresenter_StrokesErased(
            InkPresenter sender, InkStrokesErasedEventArgs args)
        {
            ClearSelection();
        }
   
        private void cutButton_Click(object sender, RoutedEventArgs e)
        {
            inkCanvas.InkPresenter.StrokeContainer.CopySelectedToClipboard();
            inkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
            ClearSelection();
        }

        private void copyButton_Click(object sender, RoutedEventArgs e)
        {
            inkCanvas.InkPresenter.StrokeContainer.CopySelectedToClipboard();
        }

        private void pasteButton_Click(object sender, RoutedEventArgs e)
        {
 
            if (inkCanvas.InkPresenter.StrokeContainer.CanPasteFromClipboard())
            {
                inkCanvas.InkPresenter.StrokeContainer.PasteFromClipboard(
                    new Point(xPegar, yPegar));
            }
            else
            {
                // Cannot paste from clipboard.
            }
        }

        // Clean up selection UI.
        private void ClearSelection()
        {
            var strokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            foreach (var stroke in strokes)
            {
                stroke.Selected = false;
            }
            ClearBoundingRect();
        }
        private void MoveSelectionLeft()
        {
          var point= new Point (inkCanvas.InkPresenter.StrokeContainer.BoundingRect.X - 1, inkCanvas.InkPresenter.StrokeContainer.BoundingRect.Y);
            var strokes = inkCanvas.InkPresenter.StrokeContainer.MoveSelected(point);
            //var strokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            //foreach (var stroke in strokes)
            //{
            //    //stroke.Selected = false;
            //    stroke.BoundingRect.X=stroke.BoundingRect.X - 1;
            //    stroke.

            //}
           // ClearBoundingRect();
        }
        private void ClearBoundingRect()
        {
            if (canvas.Children.Any())
            {
                canvas.Children.Clear();
                boundingRect = Rect.Empty;
            }
        }

        // Handle unprocessed pointer events from modifed input.
        // The input is used to provide selection functionality.
        // Selection UI is drawn on a canvas under the InkCanvas.
        private void UnprocessedInput_PointerPressed(
            InkUnprocessedInput sender, PointerEventArgs args)
        {
            // Initialize a selection lasso.
            lasso = new Polyline()
            {
                Stroke = new SolidColorBrush(Windows.UI.Colors.Blue),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection() { 5, 2 },
            };

            lasso.Points.Add(args.CurrentPoint.RawPosition);

            canvas.Children.Add(lasso);
        }

        private void UnprocessedInput_PointerMoved(
            InkUnprocessedInput sender, PointerEventArgs args)
        {
            // Add a point to the lasso Polyline object.
            lasso.Points.Add(args.CurrentPoint.RawPosition);
        }

        private void UnprocessedInput_PointerReleased(
            InkUnprocessedInput sender, PointerEventArgs args)
        {
            // Add the final point to the Polyline object and 
            // select strokes within the lasso area.
            // Draw a bounding box on the selection canvas 
            // around the selected ink strokes.
            lasso.Points.Add(args.CurrentPoint.RawPosition);

            boundingRect =
                inkCanvas.InkPresenter.StrokeContainer.SelectWithPolyLine(
                    lasso.Points);

            DrawBoundingRect();
        }

        // Draw a bounding rectangle, on the selection canvas, encompassing 
        // all ink strokes within the lasso area.
        private void DrawBoundingRect()
        {
            // Clear all existing content from the selection canvas.
            canvas.Children.Clear();

            // Draw a bounding rectangle only if there are ink strokes 
            // within the lasso area.
            if (!((boundingRect.Width == 0) ||
                (boundingRect.Height == 0) ||
                boundingRect.IsEmpty))
            {
                var rectangle = new Rectangle()
                {
                    Stroke = new SolidColorBrush(Windows.UI.Colors.Blue),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection() { 5, 2 },
                    Width = boundingRect.Width,
                    Height = boundingRect.Height
                };

                Canvas.SetLeft(rectangle, boundingRect.X);
                Canvas.SetTop(rectangle, boundingRect.Y);

                canvas.Children.Add(rectangle);
            }
        }
        //pegar desde el menú contextual
        private void DrawingCanvas_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {

            xPegar = e.GetPosition(sender as UIElement).X;
            yPegar = e.GetPosition(sender as UIElement).Y;
            MenuFlyout menuFlyout = new MenuFlyout();
            MenuFlyoutItem btnPegar = new MenuFlyoutItem { Text = "Pegar" };
            menuFlyout.Items.Add(btnPegar);
            menuFlyout.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));
            btnPegar.Click += pasteButton_Click; 
            
        }

       

        private void InkCanvas_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse) {

            }
        }

        private void InkCanvas_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            MoveSelectionLeft();
        }

        private void InkCanvas_DropCompleted(UIElement sender, DropCompletedEventArgs args)
        {
             // var point = new Point(inkCanvas.InkPresenter.StrokeContainer.BoundingRect.X , inkCanvas.InkPresenter.StrokeContainer.BoundingRect.Y);

            var point = new Point(0,0);
            inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            //foreach (InkStroke stroke in inkCanvas.InkPresenter.StrokeContainer.GetStrokes())
            //{
            //    //stroke.DrawingAttributes.Color = Windows.UI.Colors.Yellow;
            //    InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();
            //    drawingAttributes.Color = Windows.UI.Colors.Yellow;
            //    stroke.DrawingAttributes = drawingAttributes;
            //}

        }

    }
}






