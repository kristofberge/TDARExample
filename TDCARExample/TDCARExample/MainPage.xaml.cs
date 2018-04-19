using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace TDCARExample
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();
		}

        private void OnButtonClicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new ARPage());
        }

        private void OnViewButtonClicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new ARViewPage());
        }
    }
}
