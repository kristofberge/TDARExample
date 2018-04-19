using System;
using Xamarin.Forms;

namespace TDCARExample.Views
{
    public class ARView : ContentView
    {
        public static readonly BindableProperty UseAlternativeModelProperty =
            BindableProperty.Create(nameof(UseAlternativeModel), typeof(bool), typeof(ARView), default(bool));

        public bool UseAlternativeModel
        {
            get { return (bool)GetValue(UseAlternativeModelProperty); }
            set { SetValue(UseAlternativeModelProperty, value); }
        }

        public event Action OnTapped;

        public ARView()
        {
            GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(LaunchTappedEvent)
            });
        }

        private void LaunchTappedEvent()
        {
            OnTapped?.Invoke();
        }
    }
}

