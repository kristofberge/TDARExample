using System.Collections.ObjectModel;
using System.Windows.Input;
using PropertyChanged;
using TDCARExample.Models;
using Xamarin.Forms;

namespace TDCARExample.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class ARViewPageViewModel
    {
        private Model model1;
        private Model model2;
        private float scaleModel1;
        private float scaleModel2;

        public ICommand Model1ClickedCommand { get; set; }
        public ICommand Model2ClickedCommand { get; set; }
        public ICommand PlaneTappedCommand { get; set; }

        public bool UseAlternativeModel { get; set; }
        public bool UseNormalModel { get; set; }

        public ObservableCollection<ARObject> VirtualObjects { get; set; }

        public ARViewPageViewModel()
        {
            Model1ClickedCommand = new Command(SetModelOne);
            Model2ClickedCommand = new Command(SetModelTwo);
            PlaneTappedCommand = new Command<WorldPosition>(OnPlaneTapped);
            VirtualObjects = new ObservableCollection<ARObject>();

            UseNormalModel = true;

            if (Device.RuntimePlatform == Device.iOS)
            {
                model1 = new Model
                {
                    Id = "ship",
                    Asset = "ship",
                    Texture = "godzilla"
                };
                scaleModel1 = 0.01f;

                model2 = new Model
                {
                    Id = "shark",
                    Asset = "sharkinho",
                    Texture = "Shark"
                };
                scaleModel2 = 0.0006f;
            }
            else
            {
                model1 = new Model
                {
                    Id = "Andy",
                    Asset = "andy",
                    Texture = "andy"
                };
                scaleModel1 = 0.5f;

                model2 = new Model
                {
                    Id = "Viking",
                    Asset = "viking",
                    Texture = "viking"
                };
                scaleModel2 = 0.25f;
            }
        }

        private void OnPlaneTapped(WorldPosition obj)
        {
            var arObject = new ARObject
            {
                Coordinates = obj,
                Model = UseNormalModel ? this.model1 : this.model2,
                Scale = UseNormalModel ? this.scaleModel1 : this.scaleModel2
            };
            VirtualObjects.Add(arObject);
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
