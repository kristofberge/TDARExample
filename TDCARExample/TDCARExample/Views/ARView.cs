using System.Collections.ObjectModel;
using System.Windows.Input;
using TDCARExample.Models;
using Xamarin.Forms;

namespace TDCARExample.Views
{
    public class ARView : ContentView
    {
        public static readonly BindableProperty UseAlternativeModelProperty =
            BindableProperty.Create(nameof(UseAlternativeModel), typeof(bool), typeof(ARView), default(bool));

        public static readonly BindableProperty PlaneTappedCommandProperty =
            BindableProperty.Create(nameof(PlaneTappedCommand), typeof(ICommand), typeof(ARView), default(Command));

        public static readonly BindableProperty VirtualObjectsProperty =
            BindableProperty.Create(nameof(VirtualObjects), typeof(ObservableCollection<ARObject>), typeof(ARView), default(ObservableCollection<ARObject>));

        public ObservableCollection<ARObject> VirtualObjects
        {
            get { return (ObservableCollection<ARObject>)GetValue(VirtualObjectsProperty); }
            set { SetValue(VirtualObjectsProperty, value); }
        }

        public bool UseAlternativeModel
        {
            get { return (bool)GetValue(UseAlternativeModelProperty); }
            set { SetValue(UseAlternativeModelProperty, value); }
        }

        public ICommand PlaneTappedCommand
        {
            get { return (ICommand)GetValue(PlaneTappedCommandProperty); }
            set { SetValue(PlaneTappedCommandProperty, value); }
        }

        public void OnPlaneTapped(WorldPosition coordinates)
        {
            if (PlaneTappedCommand?.CanExecute(coordinates) ?? false)
            {
                PlaneTappedCommand.Execute(coordinates);
            }
        }
    }
}

