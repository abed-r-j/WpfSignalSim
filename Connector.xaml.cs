using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // For Brushes

namespace WpfSignalSim
{
    public enum ConnectorType { Input, Output }

    public partial class Connector : UserControl
    {
        // Dependency Property for Connector Type (Input/Output)
        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register("Type", typeof(ConnectorType), typeof(Connector),
                                        new PropertyMetadata(ConnectorType.Input, OnTypeChanged)); // Call OnTypeChanged when Type changes

        public ConnectorType Type
        {
            get { return (ConnectorType)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }

        // Reference to the parent SimBlock this connector belongs to
        public SimBlock? ParentBlock { get; set; }

        // Reference to the wire connected to this connector (if any)
        public Wire? ConnectedWire { get; set; }

        public Connector()
        {
            InitializeComponent();
            UpdateVisual(); // Set initial visual based on default type
        }

        // Update visual appearance based on type (e.g., color)
        private static void OnTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Connector connector)
            {
                connector.UpdateVisual();
            }
        }

        private void UpdateVisual()
        {
            // Example: Change color based on type
            ConnectorVisual.Fill = (Type == ConnectorType.Input) ? Brushes.Green : Brushes.Blue;
        }
    }
}
