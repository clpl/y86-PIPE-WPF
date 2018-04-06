using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Y86vmWpf.Model;

namespace Y86vmWpf.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}
            Y86 = new PIPEModel() { Icode = "hellp" };
        }

        private PIPEModel y86;

        public PIPEModel Y86 { get => y86; set => y86 = value; }

        private RelayCommand changebb;

        public RelayCommand Changebb
        {
            get
             {
                    if (changebb == null) return new RelayCommand(() => ExcuteValidForm(), CanExcute);
                    return changebb;
             }
             set { changebb = value; }
         }

        private void ExcuteValidForm()
        {
            Y86.Icode = "success";
        }

        private bool CanExcute()
        {
            return true;
        }
    }
}