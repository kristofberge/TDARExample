using Xamarin.Forms;

namespace TDCARExample.Views
{
    public class SelectableLabel : Label
    {
        public static readonly BindableProperty IsSelectedProperty =
            BindableProperty.Create(nameof(IsSelected), typeof(bool), typeof(SelectableLabel), false, propertyChanged: OnIsSelectedChanged);

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        private static void OnIsSelectedChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is SelectableLabel view)
            {
                view.UpdateIsSelected();
            }
        }

        public SelectableLabel()
        {
            UpdateIsSelected();
        }

        private void UpdateIsSelected()
        {
            if (IsSelected)
            {
                SetSelectedStyle();
            }
            else
            {
                SetUnselectedStyle();
            }
        }

        private void SetSelectedStyle()
        {
            TextColor = Color.Red;
            FontAttributes = FontAttributes.Bold;
        }

        private void SetUnselectedStyle()
        {
            TextColor = Color.White;
            FontAttributes = FontAttributes.None;
        }
    }
}
