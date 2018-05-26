using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System.Threading.Tasks;
using System.Threading;
using Y86vmWpf.Model;
using PIPE.Model;
using System;
using System.IO;

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

        private RelayCommand loadCode;

        public RelayCommand LoadCode
        {
            get
            {
                if (loadCode == null) return new RelayCommand(() => LoadCodeExcuteValidForm(), CanExcute);
                return loadCode;
            }
            set { loadCode = value; }
        }

        private RelayCommand showmem;

        public RelayCommand Showmem
        {
            get
            {
                if (showmem == null) return new RelayCommand(() => ShowmemExcuteValidForm(), CanExcute);
                return showmem;
            }
            set { showmem = value; }
        }

        private RelayCommand openTxt;

        public RelayCommand OpenTxt
        {
            get
            {
                if (openTxt == null) return new RelayCommand(() => OpenTxtExcuteValidForm(), CanExcute);
                return openTxt;
            }
            set { openTxt = value; }
        }

        private RelayCommand reset;

        public RelayCommand Reset
        {
            get
            {
                if (reset == null) return new RelayCommand(() => ResetExcuteValidForm(), CanExcute);
                return reset;
            }
            set { reset = value; }
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

        public void LoadCodeExcuteValidForm()
        {
            Assembler asm = new Assembler();
            string code = Y86.Vcode.Trim();
            string result="";
            int i = 0;
            string disresult = "";
            int wei = 0;
            int cnt = 0;
            while(i<code.Length)
            {
                cnt++;
                wei = result.Length;
                string temp ="";
                string tempresult = "";
                if (i >= code.Length)
                    break;
                while (i < code.Length && !(char.IsDigit(code[i]) || char.IsLetter(code[i])))
                    i++;

                while (i < code.Length && code[i]!='\r')
                {
                    temp += code[i];
                    i++;
                }
                temp += ' ';
                tempresult = asm.assemble(temp);
                result += tempresult;
                disresult += string.Format("{0}:0x{1} {2,-20} | {3}",cnt, (wei / 2).ToString("x16").Substring(12), tempresult.Trim(), temp);
                //disresult += cnt + ": " + "0x"+ (wei/2).ToString("x16").Substring(12) + "  "+ tempresult + "  |  " + temp;
                disresult += '\n';
            }
            //Console.WriteLine(Y86.Vcode.Trim());
            Y86.ccode = result;
            Y86.vcode = disresult;
            Y86.Vcode = Y86.vcode;
            Y86.ReadFromText();
            Console.WriteLine(Y86.vcode);
        }

        public void ShowmemExcuteValidForm()
        {
            Y86.GetMemData();
        }

        public void OpenTxtExcuteValidForm()
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.DefaultExt = ".txt";
            
            if (ofd.ShowDialog() == true)
            {
                
                Y86.Vcode = File.ReadAllText(ofd.FileName);
                Console.WriteLine(Y86.Vcode);
            }
        }

        public void ResetExcuteValidForm()
        {
            Y86.InitAll();
        }

        private bool running = true; 

        public void RunRunRun()
        {
            //Y86.test();
            Assembler asm = new Assembler();
            string s = asm.assemble("jne 487");
            Console.WriteLine(s);
            
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
                    Thread.Sleep(5);
                }
                
            });
            
        }
    }
}