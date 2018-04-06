using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;

namespace Y86vmWpf.Model
{
    public class PIPEModel : ObservableObject
    {
        string icode;

        public string Icode
        {
            get
            {
                return icode;
            }

            set
            {
                icode = value;
                RaisePropertyChanged(() => Icode);
            }
        }
    }
}
