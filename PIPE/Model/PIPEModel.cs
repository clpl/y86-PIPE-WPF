﻿using System;
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

        ITools tools;

        const Int64 bit_width = 64;
        const int MAX_ICODE = 0xB;
        const Int64 MAX_MEM = 1 << 8;

        Int64 cycle_cnt;
        MemArr mem;
        Int64[] reg_file;
        //Fetch
        Int64 F_predPC;
        Int64 f_stat, f_icode, f_ifun, f_valC, f_valP, f_pc, f_predPC;
        byte f_rA, f_rB;
   
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
        Int64 w_dstE, w_valE, w_dstM, w_valM;
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
            tools = new ITools();
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

        #region Fetch
        void Fetch()
        {
            //Select f_pc from the source
            if (M_icode == IJXX && !M_Cnd)
                f_pc = M_valA;
            else if (W_icode == IRET)
                f_pc = W_valM;
            else
                f_pc = F_predPC;

            //Fetch the instruction
            byte imem = mem.Read(f_pc);
            byte[] temp = new byte[2];
            temp = tools.ByteSplit(imem);
            imem_icode = temp[0];
            imem_ifun = temp[1];

            bool instr_valid, imem_error, need_regids,need_valC;

            if(imem_icode <= MAX_ICODE)
            {
                imem_error = true;
            }
            else
            {
                imem_error = false;
            }

            if( (f_pc < MAX_MEM && f_pc >=0) )
            {
                instr_valid = true;
                f_icode = imem_icode;
                f_ifun = imem_ifun;
            }
            else
            {
                instr_valid = false;
                f_icode = INOP;
                f_ifun = FNONE;
            }

            if (imem_error)
                f_stat = SADR;
            else if (!instr_valid)
                f_stat = SINS;
            else if (f_icode == IHALT)
                f_stat = SHLT;
            else
                f_stat = SAOK;

            //Is instruction valid?
            if (f_icode == INOP || f_icode == IHALT || f_icode == IRRMOVL || f_icode == IIRMOVL || f_icode == IRMMOVL || f_icode == IMRMOVL || f_icode == IOPL || f_icode == IJXX || f_icode == ICALL || f_icode == IRET || f_icode == IPUSHL || f_icode == IPOPL)
                instr_valid = true;
            else
                instr_valid = false;
            //Does fetched instruction require a regid byte?
            if (f_icode == IRRMOVL || f_icode == IOPL || f_icode == IPUSHL || f_icode == IPOPL || f_icode == IIRMOVL || f_icode == IRMMOVL || f_icode == IMRMOVL)
                need_regids = true;
            else
                need_regids = false;
            //Does fetched instruction require a constant word?
            if (f_icode == IIRMOVL || f_icode == IRMMOVL || f_icode == IMRMOVL || f_icode == IJXX || f_icode == ICALL)
                need_valC = true;
            else
                need_valC = false;

            if (need_regids)
            {
                temp = tools.ByteSplit(mem.Read(f_pc + 1));
                f_rA = temp[0];
                f_rB = temp[1];           
            }
            if (need_valC)
            {
                int t = 0;
                if(need_regids)
                {
                    t = 1;
                }
                byte[] temp8 = new byte[8];
                f_valC = 0;
                for (int i = 0; i < 8; i++)
                {
                    temp[i] = mem.Read(f_pc + t + i);
                    f_valC += temp[i];
                    f_valC <<= 8;
                }
            }
            
            f_valP = f_pc+1;
            if(need_regids)
            {
                f_valP += 1;
            }
            if(need_valC)
            {
                f_valP += 8;
            }

            //Predict next value of PC
            if (f_icode == IJXX || f_icode == ICALL)
                f_predPC = f_valC;
            else
                f_predPC = f_valP;

        }
        #endregion

        #region Decode
        void Decode()
        {
            d_stat = D_stat;
            d_icode = D_icode;
            d_ifun = D_ifun;
            d_valC = D_valC;
            //select d_srcA                  
            switch(D_icode)
            {
                case IRRMOVL:
                case IRMMOVL:
                case IOPL:
                case IPUSHL:
                    d_srcA = D_rA;
                    break;
                case IPOPL:
                case IRET:
                    d_srcA = RESP;
                    break;
                default:
                    d_srcA = RNONE;
                    break;
            }
            //select d_srcB
            switch (D_icode)
            {
                case IRRMOVL:
                case IRMMOVL:
                case IOPL:
                    d_srcB = D_rB;
                    break;
                case IPUSHL:                    
                case IPOPL:
                case IRET:
                case ICALL:
                    d_srcB = RESP;
                    break;
                default:
                    d_srcB = RNONE;
                    break;
            }
            //select d_srcE
            switch (D_icode)
            {
                case IRRMOVL:
                case IIRMOVL:
                case IOPL:
                    d_dstE = D_rB;
                    break;
                case IPUSHL:
                case IPOPL:
                case IRET:
                case ICALL:
                    d_dstE = RESP;
                    break;
                default:
                    d_dstE = RNONE;
                    break;
            }

            
            if (D_icode == IMRMOVL || D_icode == IPOPL)
                d_dstM = D_rA;
            else
                d_dstM = RNONE;
                               
                               
                               
            if (D_icode == ICALL || D_icode == IJXX)
            {
                d_valA = D_valP;
                Forwarding_A = "NULL";
            }
            else if (d_srcA == e_dstE)
            {
                d_valA = e_valE;
                Forwarding_A = "e_valE:0x" + e_valE.ToString("x8");
            }
            else if (d_srcA == M_dstM)
            {
                d_valA = m_valM;
                Forwarding_A = "m_valM:0x" + m_valM.ToString("x8");
            }
            else if (d_srcA == M_dstE)
            {
                d_valA = M_valE;
                Forwarding_A = "M_valE:0x" + M_valE.ToString("x8");
            }
            else if (d_srcA == W_dstM)
            {
                d_valA = W_valM;
                Forwarding_A = "W_valM:0x" + W_valM.ToString("x8");
            }
            else if (d_srcA == W_dstE)
            {
                d_valA = W_valE;
                Forwarding_A = "W_valE:0x" + W_valE.ToString("x8");
            }
            else
            {
                d_valA = reg_file[d_srcA];
                Forwarding_A = "NULL";
            }
            //Fwd B module
            if (d_srcB == e_dstE)
            {
                d_valB = e_valE;
                Forwarding_B = "e_valE:0x" + e_valE.ToString("x8");
            }
            else if (d_srcB == M_dstM)
            {
                d_valB = m_valM;
                Forwarding_B = "m_valM:0x" + m_valM.ToString("x8");
            }
            else if (d_srcB == M_dstE)
            {
                d_valB = M_valE;
                Forwarding_B = "M_valE:0x" + M_valE.ToString("x8");
            }
            else if (d_srcB == W_dstM)
            {
                d_valB = W_valM;
                Forwarding_B = "W_valM:0x" + W_valM.ToString("x8");
            }
            else if (d_srcB == W_dstE)
            {
                d_valB = W_valE;
                Forwarding_B = "W_valE:0x" + W_valE.ToString("x8");
            }
            else
            {
                d_valB = reg_file[d_srcB];
                Forwarding_B = "NULL";
            }
        }
        #endregion

        #region Execute
        void Execute()
        {
            e_stat = E_stat;
            e_icode = E_icode;

            //Select aluA
            switch (E_icode)
            {
                case IRRMOVL:
                case IOPL:
                    aluA = E_valA;
                    break;
                case IIRMOVL:
                case IRMMOVL:
                case IMRMOVL:
                    aluA = E_valC;
                    break;
                case ICALL:
                case IPUSHL:
                    aluA = -4;
                    break;
                default:
                    aluA = 0;
                    break;
            }
            //Select aluB
            switch (E_icode)
            {
                case IRMMOVL:
                case IMRMOVL:
                case IOPL:
                case ICALL:
                case IPUSHL:                   
                case IRET:
                case IPOPL:
                    aluB = E_valB;
                    break;
                default:
                    aluB = 0;
                    break;
            }
            
            if (E_icode == IOPL)
                alufun = E_ifun;
            else
                alufun = ALUADD;
            //ALU module
            switch (alufun)
            {
                case 0: e_valE = aluB + aluA; break;
                case 1: e_valE = aluB - aluA; break;
                case 2: e_valE = aluB & aluA; break;
                case 3: e_valE = aluB ^ aluA; break;
                default: e_valE = 0; break;
            }
            //Should the condition codes be updated?
            //State changes only during normal operation
            if (E_icode == IOPL && m_stat != SADR && m_stat != SINS && m_stat != SHLT && W_stat != SADR && W_stat != SINS && W_stat != SHLT)
                set_cc = true;
            else
                set_cc = false;
            //Generate valA in execute stage
            e_valA = E_valA;
            //Set dstE to RNONE in event of not-taken conditional move
            if (E_icode == IRRMOVL && !e_Cnd)
                e_dstE = RNONE;
            else
                e_dstE = E_dstE;
            //cc
            if (set_cc)
            {
                if (e_valE < 0)
                    SF = true;
                else
                    SF = false;
                if (e_valE == 0)
                    ZF = true;
                else
                    ZF = false;
                if ((aluA < 0 == aluB < 0) && (aluA < 0 != e_valE < 0))
                    OF = true;
                else
                    OF = false;
            }
            //e_Cnd module            
            if (E_icode == IJXX || E_icode == IRRMOVL)
            {
                switch (E_ifun)
                {
                    case 0: e_Cnd = true; break;//jmp or rrmovl
                    case 1: if (ZF || SF) e_Cnd = true; else e_Cnd = false; break;//jle or cmovle
                    case 2: if (SF) e_Cnd = true; else e_Cnd = false; break;//jl or cmovl
                    case 3: if (ZF) e_Cnd = true; else e_Cnd = false; break;//je or comve
                    case 4: if (!ZF) e_Cnd = true; else e_Cnd = false; break;//jne or cmovne
                    case 5: if (!SF) e_Cnd = true; else e_Cnd = false; break;//jge or comvge
                    case 6: if (!ZF && !SF) e_Cnd = true; else e_Cnd = false; break;//jg or comvg
                }
            }
            else
                e_Cnd = false;
            e_dstM = E_dstM;
        }
        #endregion

        #region Memory
        void Memory()
        {
            m_icode = M_icode;
            m_valE = M_valE;
            m_dstE = M_dstE;
            m_dstM = M_dstM;
            //Select memory address
            switch (M_icode)
            {
                case IRMMOVL:
                case IPUSHL:
                case ICALL:
                case IMRMOVL:
                    mem_addr = M_valE;
                    break;
                case IPOPL:
                case IRET:
                    mem_addr = M_valA;
                    break;             
                default:                
                    break;
            }
           
                                  //Set read control signal
            if (M_icode == IMRMOVL || M_icode == IPOPL || M_icode == IRET)
                mem_read = true;
            else
                mem_read = false;
            //Set write control signal
            if (M_icode == IRMMOVL || M_icode == IPUSHL || M_icode == ICALL)
                mem_write = true;
            else
                mem_write = false;
            //read / write memory
            if (mem_addr > MAX_MEM)
                dmem_error = true;
            else
            {
                dmem_error = false;
                if (mem_read)
                {
                    m_valM = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        m_valM <<= 8;
                        m_valM += mem.Read(mem_addr + i);                       
                    }
                }
                else
                    m_valM = 0;
                if (mem_write)
                {
                    for (int i = 0; i < 4; i++)                       
                        mem.Write(mem_addr, (byte)((M_valA >> (8 * i)) & 0xff));
                }
            }
            //Update the status
            m_stat = dmem_error ? SADR : M_stat;
        }
        #endregion

        #region Write back
        void Write_back()
        {
            //Set E port register ID
            w_dstE = W_dstE;
            //## Set E port value
            w_valE = W_valE;
            //Set M port register ID
            w_dstM = W_dstM;
            //Set M port value
            w_valM = W_valM;
            //Update processor status
            if (W_stat == SBUB)
                Stat = SAOK;
            else
                Stat = W_stat;
            if (Stat == SAOK)//only if stat is AOK can we update register file
            {
                if (W_dstE != RNONE)
                    reg_file[W_dstE] = W_valE;
                if (W_dstM != RNONE)
                    reg_file[W_dstM] = W_valM;
            }
        }
        #endregion
    }



    #region MEM
    interface IMem
    {
        byte Read(Int64 addr);
        void Write(Int64 addr, byte data);
    }

    class MemArr : IMem
    {
        private const Int64 MAX_MEM = 1 << 8;
        private byte[] arr;
        
        public MemArr()
        {
            arr = new byte[MAX_MEM];
            Array.Clear(arr, 0, arr.Length);
            
        }

        public byte Read(Int64 addr)
        {
            return arr[addr];
        }

        public void Write(Int64 addr, byte data)
        {
            arr[addr] = data;
        }

    }
    #endregion

    public class ITools
    {
        public byte[] ByteSplit(Byte num)
        {
            byte[] res = new byte[2];
            res[0] = (byte)((num >> 4) & (0xF));
            res[1] = (byte)(num & 0xF);
            return res;
        }
    }

}
