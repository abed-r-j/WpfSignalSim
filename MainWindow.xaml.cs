using System;
using System.Collections.Generic; // For List
using System.Linq; // For LINQ
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes; // For Line

namespace WpfSignalSim
{
    public partial class MainWindow : Window
    {
        // --- Drag/Drop State ---
        private Point _startPoint; // General starting point for drag operations
        private FrameworkElement? _draggedElement = null; // Block being dragged on canvas
        private bool _isDraggingBlock = false; // Flag for dragging existing block
        private Point _toolboxDragStartPoint; // Start point for toolbox drag detection

        // --- Wiring State ---
        private bool _isDrawingWire = false;
        private Connector? _sourceConnector = null;
        private Line? _previewWire = null;

        // --- Simulation Model ---
        private List<SimBlock> _simBlocks = new List<SimBlock>(); // Keep track of blocks on canvas
        private List<Wire> _wires = new List<Wire>(); // Keep track of connections

        public MainWindow()
        {
            InitializeComponent();
        }

        // --- Toolbox Drag & Drop ---

        private void ToolboxItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Store the start point for drag detection
            _toolboxDragStartPoint = e.GetPosition(null); // Position relative to the screen
        }

        private void ToolboxItem_MouseMove(object sender, MouseEventArgs e)
        {
            // Check if the left mouse button is pressed
            if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement element)
            {
                Point currentPosition = e.GetPosition(null); // Position relative to the screen
                Vector diff = _toolboxDragStartPoint - currentPosition;

                // Check if the mouse has moved significantly enough to start a drag
                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    // Get the block type identifier from the Tag property
                    string? blockType = element.Tag as string;
                    if (blockType != null)
                    {
                        // Package the data to be dragged (the block type)
                        DataObject dragData = new DataObject("SimBlockType", blockType);

                        // Initiate the drag-and-drop operation
                        DragDrop.DoDragDrop(element, dragData, DragDropEffects.Copy);
                    }
                }
            }
        }

        // --- Canvas Drag/Drop Handling ---

        private void SimulationCanvas_DragEnter(object sender, DragEventArgs e)
        {
            // Check if the dragged data is of the expected format ("SimBlockType")
            if (e.Data.GetDataPresent("SimBlockType"))
            {
                e.Effects = DragDropEffects.Copy; // Show copy cursor
            }
            else
            {
                e.Effects = DragDropEffects.None; // Indicate invalid drop target
            }
            e.Handled = true;
        }

        private void SimulationCanvas_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("SimBlockType"))
            {
                string? blockType = e.Data.GetData("SimBlockType") as string;
                Point dropPosition = e.GetPosition(SimulationCanvas);

                if (blockType != null)
                {
                    // Create the appropriate SimBlock based on the type
                    SimBlock newBlock = CreateSimBlock(blockType);

                    // Add event handlers for dragging the block *within* the canvas
                    newBlock.MouseLeftButtonDown += SimBlock_MouseLeftButtonDown;
                    // Add handlers for connectors
                    SetupConnectorEventHandlers(newBlock);


                    // Position and add the block to the canvas
                    Canvas.SetLeft(newBlock, dropPosition.X - newBlock.Width / 2); // Center block roughly
                    Canvas.SetTop(newBlock, dropPosition.Y - newBlock.Height / 2);
                    SimulationCanvas.Children.Add(newBlock);
                    _simBlocks.Add(newBlock); // Add to our model list

                    e.Handled = true;
                }
            }
        }

        // Helper to create blocks (expand this)
        private SimBlock CreateSimBlock(string type)
        {
            // In a real app, you might have different UserControls or configure
            // the SimBlock differently based on type.
            // You would also load default parameters here.
            SimBlock block = new SimBlock();
            block.Title = type.Replace("_", " "); // Simple title generation
            // TODO: Set specific parameters, connector types based on 'type'
            // Example: If type is "Add_Noise", add a "SNR" parameter
            if (type == "Add_Noise")
            {
                block.Parameters.Add("SNR_dB", "30"); // Default value
            }
            else if (type == "Add_Jitter")
            {
                block.Parameters.Add("Amplitude_ps", "2");
                block.Parameters.Add("Frequency_MHz", "200");
            }
            // Add more parameter initialization based on block type...

            return block;
        }

        // --- Block Dragging within Canvas ---

        private void SimBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Don't drag block if clicking on a connector
            if (e.OriginalSource is Connector) return;

            if (sender is SimBlock element)
            {
                _isDraggingBlock = true;
                _draggedElement = element;
                _startPoint = e.GetPosition(SimulationCanvas); // Position relative to the canvas
                _draggedElement.CaptureMouse(); // Capture mouse events
                Panel.SetZIndex(_draggedElement, 10); // Bring to front
                ShowProperties(element); // Show properties when block is clicked
                e.Handled = true;
            }
        }

        private void SimulationCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            // --- Handle Block Dragging ---
            if (_isDraggingBlock && _draggedElement != null && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition(SimulationCanvas);
                double offsetX = currentPosition.X - _startPoint.X;
                double offsetY = currentPosition.Y - _startPoint.Y;

                double newLeft = Canvas.GetLeft(_draggedElement) + offsetX;
                double newTop = Canvas.GetTop(_draggedElement) + offsetY;

                Canvas.SetLeft(_draggedElement, newLeft);
                Canvas.SetTop(_draggedElement, newTop);

                _startPoint = currentPosition;

                // Update wires connected to this block
                UpdateWires(_draggedElement as SimBlock);
                e.Handled = true; // Prevent interference if over another element
            }
            // --- Handle Wire Drawing Preview ---
            else if (_isDrawingWire && _previewWire != null && _sourceConnector != null)
            {
                Point currentPosition = e.GetPosition(SimulationCanvas);
                _previewWire.X2 = currentPosition.X;
                _previewWire.Y2 = currentPosition.Y;
                e.Handled = true;
            }
        }

        private void SimulationCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Stop dragging block
            if (_isDraggingBlock && _draggedElement != null)
            {
                _draggedElement.ReleaseMouseCapture();
                Panel.SetZIndex(_draggedElement, 0); // Reset Z-index
                _draggedElement = null;
                _isDraggingBlock = false;
                e.Handled = true;
            }
        }

        // --- Wiring Logic ---

        private void SetupConnectorEventHandlers(SimBlock block)
        {
            // Find connectors within the block's visual tree
            foreach (var connector in FindVisualChildren<Connector>(block))
            {
                // Use PreviewMouseLeftButtonDown for starting wire draw
                connector.PreviewMouseLeftButtonDown += Connector_PreviewMouseLeftButtonDown;
                // Use MouseLeftButtonUp for ending wire draw
                connector.MouseLeftButtonUp += Connector_MouseLeftButtonUp;
                // Associate connector with its parent block
                connector.ParentBlock = block;
            }
        }

        private void Connector_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Connector connector && !_isDrawingWire)
            {
                _isDrawingWire = true;
                _sourceConnector = connector;
                Point startPoint = _sourceConnector.TranslatePoint(new Point(_sourceConnector.ActualWidth / 2, _sourceConnector.ActualHeight / 2), SimulationCanvas);

                _previewWire = new Line
                {
                    X1 = startPoint.X,
                    Y1 = startPoint.Y,
                    X2 = startPoint.X, // Initially same point
                    Y2 = startPoint.Y,
                    Stroke = Brushes.DarkGray,
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 2, 2 } // Dashed line for preview
                };
                SimulationCanvas.Children.Add(_previewWire);
                SimulationCanvas.CaptureMouse(); // Capture mouse on canvas for drawing
                e.Handled = true;
            }
        }

        // <<<< FIX: Added missing SimulationCanvas_PreviewMouseLeftButtonDown method >>>>
        private void SimulationCanvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // This handler is referenced in MainWindow.xaml for the Canvas.
            // Add logic here if you want to handle clicks directly on the canvas background,
            // for example, to deselect all blocks or cancel an operation.
            // Currently, clicking the background doesn't do anything specific.
            // We could potentially cancel wire drawing here too if needed.
            // if (_isDrawingWire)
            // {
            //    CancelWireDrawing();
            //    e.Handled = true;
            // }
        }


        // Need this on the Canvas to handle mouse up when not over a specific element
        private void SimulationCanvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // If drawing a wire and mouse is released over the canvas (not a connector)
            if (_isDrawingWire && _sourceConnector != null && !(e.OriginalSource is Connector))
            {
                CancelWireDrawing();
                e.Handled = true;
            }
        }


        private void Connector_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawingWire && _sourceConnector != null && sender is Connector targetConnector)
            {
                // Prevent connecting to self or incompatible types (e.g., output to output)
                // Basic check: Different blocks and different connector types (Input vs Output)
                if (_sourceConnector.ParentBlock != targetConnector.ParentBlock && _sourceConnector.Type != targetConnector.Type)
                {
                    // Finalize wire creation
                    Point startPoint = _sourceConnector.TranslatePoint(new Point(_sourceConnector.ActualWidth / 2, _sourceConnector.ActualHeight / 2), SimulationCanvas);
                    Point endPoint = targetConnector.TranslatePoint(new Point(targetConnector.ActualWidth / 2, targetConnector.ActualHeight / 2), SimulationCanvas);

                    // Create the final solid wire
                    Line finalWireLine = new Line
                    {
                        X1 = startPoint.X,
                        Y1 = startPoint.Y,
                        X2 = endPoint.X,
                        Y2 = endPoint.Y,
                        Stroke = Brushes.Black,
                        StrokeThickness = 2
                    };
                    SimulationCanvas.Children.Add(finalWireLine);

                    // Create a logical representation of the wire
                    // Ensure source is always Output and target is always Input for consistency
                    Wire newWire = (_sourceConnector.Type == ConnectorType.Output)
                                   ? new Wire(_sourceConnector, targetConnector, finalWireLine)
                                   : new Wire(targetConnector, _sourceConnector, finalWireLine); // Swap if started from Input

                    _wires.Add(newWire);

                    // Link connectors in the model
                    _sourceConnector.ConnectedWire = newWire;
                    targetConnector.ConnectedWire = newWire;

                    CancelWireDrawing(removePreview: true); // Clean up preview
                    e.Handled = true;
                }
                else
                {
                    // Invalid connection (e.g., self-connect, Input->Input, Output->Output)
                    CancelWireDrawing();
                    e.Handled = true;
                }
            }
            else if (_isDrawingWire) // Released not on a valid connector
            {
                CancelWireDrawing();
                e.Handled = true;
            }
        }

        private void CancelWireDrawing(bool removePreview = true)
        {
            if (_isDrawingWire)
            {
                if (removePreview && _previewWire != null)
                {
                    SimulationCanvas.Children.Remove(_previewWire);
                }
                SimulationCanvas.ReleaseMouseCapture();
                _isDrawingWire = false;
                _sourceConnector = null;
                _previewWire = null;
            }
        }


        // Update wires connected to a specific block
        private void UpdateWires(SimBlock? block)
        {
            if (block == null) return;

            // Find all connectors on this block
            foreach (var connector in FindVisualChildren<Connector>(block))
            {
                if (connector.ConnectedWire != null)
                {
                    Wire wire = connector.ConnectedWire;
                    Line wireLine = wire.WireLine;

                    // Recalculate the point on the canvas for this connector
                    Point newPoint = connector.TranslatePoint(new Point(connector.ActualWidth / 2, connector.ActualHeight / 2), SimulationCanvas);

                    // Update the appropriate end of the line
                    if (wire.Source == connector) // If this connector is the source (Output)
                    {
                        wireLine.X1 = newPoint.X;
                        wireLine.Y1 = newPoint.Y;
                    }
                    else // Must be the target (Input)
                    {
                        wireLine.X2 = newPoint.X;
                        wireLine.Y2 = newPoint.Y;
                    }
                }
            }
        }

        // --- Properties Panel ---
        private void ShowProperties(SimBlock block)
        {
            // Clear previous properties
            PropertiesContent.Content = null;
            PropertiesPlaceholder.Visibility = Visibility.Collapsed; // Hide placeholder

            // Create UI elements dynamically for the block's parameters
            StackPanel propertiesPanel = new StackPanel();

            // Add Title display/editor
            var titlePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
            titlePanel.Children.Add(new TextBlock { Text = "Title:", FontWeight = FontWeights.SemiBold, Width = 100 });
            var titleTextBox = new TextBox { Text = block.Title, Width = 130 };
            // Basic binding (in MVVM this would be better)
            titleTextBox.TextChanged += (s, e) => block.Title = ((TextBox)s).Text;
            titlePanel.Children.Add(titleTextBox);
            propertiesPanel.Children.Add(titlePanel);


            // Add specific parameters
            foreach (var kvp in block.Parameters)
            {
                var paramPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
                paramPanel.Children.Add(new TextBlock { Text = $"{kvp.Key}:", Width = 100 }); // Parameter Name

                var paramTextBox = new TextBox { Text = kvp.Value, Width = 130 }; // Parameter Value
                string key = kvp.Key; // Capture key for lambda
                paramTextBox.TextChanged += (s, e) => block.Parameters[key] = ((TextBox)s).Text; // Update dictionary on change

                paramPanel.Children.Add(paramTextBox);
                propertiesPanel.Children.Add(paramPanel);
            }

            PropertiesContent.Content = propertiesPanel;
        }


        // --- Helper Methods ---
        // Updated to include null check for child before recursive call
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    // Check if the child is not null and is of the requested type T
                    if (child != null && child is T t)
                    {
                        yield return t;
                    }

                    // <<<< FIX: Added null check for child before recursive call >>>>
                    if (child != null)
                    {
                        // Recursively search deeper in the visual tree
                        foreach (T childOfChild in FindVisualChildren<T>(child))
                        {
                            yield return childOfChild;
                        }
                    }
                }
            }
        }
    }

    // --- Logical Representation of a Wire ---
    public class Wire
    {
        // Ensure Source is always Output, Target is always Input
        public Connector Source { get; } // Output Connector
        public Connector Target { get; } // Input Connector
        public Line WireLine { get; } // The visual representation

        public Wire(Connector source, Connector target, Line line)
        {
            // Enforce Source=Output, Target=Input convention if possible during creation
            if (source.Type == ConnectorType.Output && target.Type == ConnectorType.Input)
            {
                Source = source;
                Target = target;
            }
            else if (source.Type == ConnectorType.Input && target.Type == ConnectorType.Output)
            {
                // Swap if created in reverse order
                Source = target;
                Target = source;
            }
            else
            {
                // Handle error: Invalid connection types (e.g., Input->Input)
                // This should ideally be prevented in Connector_MouseLeftButtonUp
                throw new ArgumentException("Wire must connect an Output connector to an Input connector.");
            }
            WireLine = line;
        }
    }
}