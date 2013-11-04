using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ESRI.ArcGIS.Client.Extensibility;

namespace Maximo.AddIns
{
    [Export(typeof(ICommand))]
    [DisplayName("My Tool")]
    public class MyTool : ICommand, ISupportsConfiguration
    {
        private MyConfigDialog configDialog = new MyConfigDialog();
        private MaximoDialog dialog = new MaximoDialog();

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
            configDialog.InputTextBox.Text = configData ?? "";
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
            dialog.Map = MapApplication.Current.Map;
            dialog.serviceUrl = configDialog.InputTextBox.Text;
            // Open the dialog that shows the map extent
            MapApplication.Current.ShowWindow("Transmission Operation Status", dialog);
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
