using System.Collections.Generic; // For Dictionary
using System.Windows;
using System.Windows.Controls;

namespace WpfSignalSim
{
    public partial class SimBlock : UserControl
    {
        // Dependency Property for the Title
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(SimBlock), new PropertyMetadata("Sim Block"));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // Simple dictionary to hold block-specific parameters (like SNR, Jitter Amplitude)
        // In a real app, you might use a more structured approach (e.g., specific classes per block type)
        public Dictionary<string, string> Parameters { get; private set; }


        public SimBlock()
        {
            InitializeComponent();
            Parameters = new Dictionary<string, string>();
            // Set DataContext for potential bindings within the UserControl itself if needed
            // this.DataContext = this;
        }

        // Add methods here if the block needs internal logic,
        // though most logic will likely be triggered by the main window/simulation engine.
    }
}