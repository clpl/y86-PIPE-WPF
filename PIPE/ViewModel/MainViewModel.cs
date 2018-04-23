using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System.Threading.Tasks;
using System.Threading;
using Y86vmWpf.Model;
using PIPE.Model;
using System;

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
        /// 

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
            Y86 = new PIPEModel();
            Y86.InitAll();
            //Y86.RunSpeed = 50;
            //Y86.Run();

        }

        private PIPEModel y86;

        public PIPEModel Y86 { get => y86; set { y86 = value; RaisePropertyChanged(() => Y86);} }

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
            //Y86.Icode = 16;
            RunRunRun();
        }

        private RelayCommand pause;

        public RelayCommand Pause
        {
            get
            {
                if (pause == null) return new RelayCommand(() => PauseExcuteValidForm(), CanExcute);
                return Pause;
            }
            set { pause = value; }
        }

        private void PauseExcuteValidForm()
        {
            running = false;
        }

        private RelayCommand astep;

        public RelayCommand Astep
        {
            get
            {
                if (astep == null) return new RelayCommand(() => AstepExcuteValidForm(), CanExcute);
                return astep;
            }
            set { astep = value; }
        }

        private void AstepExcuteValidForm()
        {
            RunAStep();
        }

        private bool CanExcute()
        {
            return true;
        }

        public void RunAStep()
        {
            Y86.Run();
        }

        private bool running = true; 

        public void RunRunRun()
        {
            //Y86.test();
            Assembler asm = new Assembler();
            string s = asm.assemble("jne target");
            Console.WriteLine(s);
            /*
            Task.Factory.StartNew(() =>
            {
                running = true;
                int i = 0;
                while (true)
                {
                    if (i == Y86.RunSpeed)
                    {
                        RunAStep();
                        Y86.ConvertViewModel();
                        i = 0;
                    }
                    i++;
                    
                    if (!running)
                        break;
                    Thread.Sleep(10);
                }
                
            });
            */
        }
    }
}