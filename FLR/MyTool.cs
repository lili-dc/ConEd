using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Interactivity;
using System.ComponentModel.Composition;
using System.ComponentModel;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Extensibility;

namespace FLR.AddIns
{
    [Export(typeof(ICommand))]
    [DisplayName("FLR Tool")]
    public class MyTool : ICommand, ISupportsConfiguration
    {
        private MyConfigDialog configDialog = new MyConfigDialog();
        private FLRDialog dialog = new FLRDialog();

        #region ISupportsConfiguration members

        public void Configure()
        {
            // When the dialog opens, it shows the information saved from the last configuration
            MapApplication.Current.ShowWindow("Configuration", configDialog);
        }

        public void LoadConfiguration(string configData)
        {
            // Initialize the behavior's configuration with the saved configuration data. 
            // The dialog's textbox is used to store the configuration.
            configDialog.InputTextBox.Text = configData;
        }

        public string SaveConfiguration()
        {
            // Save the information from the configuration dialog
            return configDialog.InputTextBox.Text;
        }

        #endregion

        #region ICommand members
        public void Execute(object parameter)
        {
            // Show the configuration data.  Use ShowWindow instead of MessageBox.  
            // There is a bug with Firefox 3.6 that crashes Silverlight when using 
            // MessageBox.Show.
            /*
            MapApplication.Current.ShowWindow("My Tool", new TextBlock()
            {
                Text = "The saved configuration is: '" + configDialog.InputTextBox.Text + "'",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(30),
                MaxWidth = 480
            });
            
            */
            dialog.Map = MapApplication.Current.Map;
            dialog.serviceUrl = configDialog.InputTextBox.Text;
            // Open the dialog that shows the map extent
            MapApplication.Current.ShowWindow("FLR Tool", dialog);
            dialog.loadData();

        }

        public bool CanExecute(object parameter)
        {
            // Return true so that the command can always be executed
            return true;
        }

        public event EventHandler CanExecuteChanged;

        #endregion
    }
}
