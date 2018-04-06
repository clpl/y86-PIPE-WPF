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


        #region DefineData
        Int64 cycle_cnt;
        MemArr mem;
        Int64[] reg_file;
        //Fetch
        Int64 F_predPC;
        Int64 f_stat, f_icode, f_ifun, f_valC, f_valP, f_pc, f_predPC;
        byte f_rA, f_rB;
        bool imem_error, instr_valid;
        //Decode
        Int64 D_stat, D_icode, D_ifun, D_rA, D_rB, D_valC, D_valP;
        Int64 d_stat, d_icode, d_ifun, d_valC, d_valA, d_valB, d_dstE, d_dstM, d_srcA, d_srcB;
        //Exectue
        Int64 E_stat, E_icode, E_ifun, E_valC, E_valA, E_valB, E_dstE, E_dstM, E_srcA, E_srcB;
        Int64 e_stat, e_icode, e_valE, e_valA, e_dstE, e_dstM;
        bool e_Cnd;
        //Memory
        Int64 M_stat, M_icode, M_valE, M_valA, M_dstE, M_dstM;
        bool M_Cnd;
        Int64 m_stat, m_icode, m_valE, m_valM, m_dstE, m_dstM;
        bool dmem_error;
        //Write
        Int64 W_stat, W_icode, W_valE, W_valM, W_dstE, W_dstM;
        Int64 Stat;
        byte imem_icode, imem_ifun;
        //Pipeline register control signals
        bool F_stall, F_bubble, D_stall, D_bubble, E_stall, E_bubble, M_stall, M_bubble, W_stall, W_bubble;
        string Forwarding_A, Forwarding_B;
        bool ZF, SF, OF;                        //condition code
        Int64 aluA, aluB;
        Int64 alufun;
        bool set_cc;
        Int64 mem_addr;
        bool mem_read, mem_write;
        public string writeDirectory;   //directory for writing the record documents
        Int64 ini_stack;                          //record the allocated stack space

        //HCL
        const byte INOP = 0;//nop
        const byte IHALT = 1;//halt
        const byte IRRMOVL = 2;//rrmovl
        const byte IIRMOVL = 3;//rimovl
        const byte IRMMOVL = 4;//rmmovl
        const byte IMRMOVL = 5;//mrmovl
        const byte IOPL = 6;//Int64eger arthmetic
        const byte IJXX = 7;//jump instruction
        const byte ICALL = 8;//call
        const byte IRET = 9;//ret
        const byte IPUSHL = 0xA;//pushl
        const byte IPOPL = 0xB;//popl
        const byte FNONE = 0;//default function
        const byte ALUADD = 0;//add
        const byte SAOK = 1;//normal
        const byte SADR = 2;//abnormal address 
        const byte SINS = 3;//illegal instruction
        const byte SHLT = 4;//halt
        const byte SBUB = 5;//bubble
        const byte SSTA = 6;//stalling
        const byte REAX = 0;//%eax
        const byte RECX = 1;//%ecx
        const byte REDX = 2;//%edx
        const byte REBX = 3;//%ebx
        const byte RESP = 4;//%esp
        const byte REBP = 5;//%ebp
        const byte RESI = 6;//%esi
        const byte REDI = 7;//%edi
        const Int64 RNONE = 0xF;//no register

        #endregion

        #region Init
        public PIPEModel()
        {
            Init_PIPEModel();
        }

        void Init_PIPEModel()
        {
            cycle_cnt = 0;
            F_predPC = f_icode = f_ifun = f_valC = f_valP = f_pc = f_predPC = 0;
            D_icode = D_ifun = D_valC = D_valP = 0;
            d_icode = d_ifun = d_valC = d_valA = d_valB = 0;
            E_icode = E_ifun = E_valC = E_valA = E_valB = 0;
            e_icode = e_valE = 0;
            M_icode = M_valE = M_valA = 0;
            m_icode = m_valE = m_valM = 0;
            W_icode = W_valE = W_valM = 0;
            aluA = aluB = alufun = mem_addr = 0;
            D_rA = D_rB = d_dstE = d_dstM = d_srcA = d_srcB = RNONE;
            E_dstE = E_dstM = E_srcA = E_srcB = e_dstE = e_dstM = RNONE;
            M_dstE = M_dstM = m_dstE = m_dstM = RNONE;
            W_dstE = W_dstM = RNONE;
            imem_icode = INOP;
            imem_ifun = FNONE;
            f_stat = D_stat = d_stat = E_stat = e_stat = M_stat = m_stat = W_stat = Stat = SAOK;
            f_rA = f_rB = REAX;
            imem_error = instr_valid = dmem_error = ZF = SF = OF = e_Cnd = M_Cnd = set_cc = mem_read = mem_write = false;
            F_stall = F_bubble = D_stall = D_bubble = E_stall = E_bubble = M_bubble = M_stall = W_bubble = W_stall = false;
            Forwarding_A = Forwarding_B = "N";
            mem = new MemArr();
            reg_file = new Int64[9];
            Array.Clear(reg_file, 0, reg_file.Length);

        }
        #endregion
    }

    #region MEM
    interface IMem
    {
        Int64 Read(Int64 addr);
        void Write(Int64 addr, Int64 data);
    }

    class MemArr : IMem
    {
        const Int64 MAX_MEM = 1 << 8;
        private Int64[] arr;
        
        public MemArr()
        {
            arr = new Int64[MAX_MEM];
            Array.Clear(arr, 0, arr.Length);
            
        }

        public Int64 Read(Int64 addr)
        {
            return arr[addr];
        }

        public void Write(Int64 addr, Int64 data)
        {
            arr[addr] = data;
        }

    }
    #endregion
}
