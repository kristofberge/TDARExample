using System.Windows.Input;
using Xamarin.Forms;
using PropertyChanged;

namespace TDCARExample.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class ARViewPageViewModel
    {
        public ICommand Model1ClickedCommand { get; set; }
        public ICommand Model2ClickedCommand { get; set; }

        public bool UseAlternativeModel { get; set; }
        public bool UseNormalModel { get; set; }

        public ARViewPageViewModel()
        {
            Model1ClickedCommand = new Command(SetModelOne);
            Model2ClickedCommand = new Command(SetModelTwo);

            UseNormalModel = true;
        }

        private void SetModelOne()
        {
            UseAlternativeModel = false;
            UseNormalModel = true;
        }

        private void SetModelTwo()
        {
            UseAlternativeModel = true;
            UseNormalModel = false;
        }
    }
}
