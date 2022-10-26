using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Scheduling
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
    }

    private PoSorter sorter = new PoSorter();

    private void OpenCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = true;
    }

    private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      // try{
        var dialog =
          new OpenFileDialog()
          {
            DefaultExt = ".po",
            Filter = "PartialOrdered Files|*.po|All Files|*.*"
          };
        if (dialog.ShowDialog() == true)
        {
          sorter.LoadPoFile(dialog.FileName);
        }

      //} catch (Exception ex){
      //  MessageBox.Show(ex.Message);
      //}

    }

    private void MenuItemExit_Click(object sender, RoutedEventArgs e)
    {
      Close();
    }


  }
}
