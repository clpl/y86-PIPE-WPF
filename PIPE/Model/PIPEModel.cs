using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using GalaSoft.MvvmLight;
using System.IO;

namespace Y86vmWpf.Model
{
    public class PIPEModel : ObservableObject
    {


        #region DefineControlParameter
        bool isRun = false;
        long runSpeed = 50;
        #endregion

        #region DefineData
        string code;
        const String filepath = "bcode.bin";
        ITools tools;

        const Int64 bit_width = 64;
        const int MAX_ICODE = 0xB;
        const Int64 MAX_MEM = 1024+1000;

        public string ccode = "";
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
        bool set_cc, imem_error, instr_valid;
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
        const byte RNONE = 0xF;//no register
        const byte RNOP = 0;//value when register in bubble

        public long RunSpeed { get => runSpeed; set => runSpeed = value; }
        public bool IsRun { get => isRun; set => isRun = value; }
        #endregion

        #region Init
        public PIPEModel()
        {
            
        }

        public void InitAll()
        {
            tools = new ITools();
            Init_PIPEModel();
            ReadFromFile();
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
            reg_file[4] = 300;

        }

        void ReadFromFile()
        {
            FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            long len = (long)fs.Length;
            for (long i = 0; i < len; i++)
            {
                mem.Write(i, br.ReadByte());
            }
            fs.Close();
            br.Close();
        }

        public void ReadFromText()
        {
            //jmp 487512
            mem.Clear();
            string code = ccode;
            for(int i=0;i+1<code.Length;)
            {
                int temp = 0;
                int precode, aftcode;
                if (char.IsDigit(code[i]))
                    precode = Convert.ToInt32(code[i] - '0');
                else if(char.IsUpper(code[i]))
                    precode = Convert.ToInt32(code[i] - 'A' + 10);
                else
                    precode = Convert.ToInt32(code[i] - 'a' + 10);
                if (char.IsDigit(code[i + 1]))
                    aftcode = Convert.ToInt32(code[i + 1] - '0');
                else if (char.IsUpper(code[i + 1]))
                    aftcode = Convert.ToInt32(code[i + 1] - 'A' + 10);
                else
                    aftcode = Convert.ToInt32(code[i + 1] - 'a' + 10);
                temp = temp | (precode << 4);
                temp = temp | aftcode;
                mem.Write(i / 2, (byte)temp);
                /*
                 * while(!char.IsDigit(code[i]))
                {
                    i++;
                }
                */
                i += 2;
                Console.WriteLine(temp);
            }
            string s;
            s = "wee";
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

            if(imem_icode > MAX_ICODE)
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

            if(W_stat != SHLT)
                f_pc += 1;

            f_valC = 0;

            if (need_regids)
            {
                temp = tools.ByteSplit(mem.Read(f_pc));
                f_rA = temp[0];
                f_rB = temp[1];
                f_pc += 1;
            }else
            {
                f_rB = f_rA = RNONE;
            }
            if (need_valC)
            {
                byte[] temp8 = new byte[8];
                for (int i = 0; i < 4; i++)
                {
                    temp8[i] = mem.Read(f_pc + i);
                    Console.WriteLine(f_pc + i);
                    Console.WriteLine(temp8[i].ToString("x16"));
                    f_valC <<= 8;
                    f_valC += temp8[i];    
                }
                f_pc += 4;
            }

            f_valP = f_pc;

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
                case IMRMOVL:
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
                               
                               
                               
            if (D_icode == ICALL)
            {
                d_valA = D_valP;
                Forwarding_A = "NULL";
            }
            else if(D_icode == IJXX)
            {
                d_valA = D_valC;
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
                case IPOPL:
                case IRET:
                    aluA = 4;
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
                        m_valM += mem.Read(mem_addr - i) << (8 * i);                      
                    }
                }
                else
                    m_valM = 0;
                if (mem_write)
                {
                    for (int i = 0; i < 4; i++)                       
                        mem.Write(mem_addr - i, (byte)((M_valA >> (8 * i)) & 0xff));
                }
            }
            //Update the status
            if (dmem_error)
                m_stat = SADR;
            else
                m_stat = M_stat;
            
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

        #region GeneratedControlSignal
        void GeneratedStateControlSignal()
        {
            F_bubble = false;
            if ((E_icode == IMRMOVL || E_icode == IPOPL) && (E_dstM == d_srcA || E_dstM == d_srcB) || (IRET == D_icode || IRET == E_icode || IRET == M_icode))
                F_stall = true;
            else
                F_stall = false;

            if ((E_icode == IMRMOVL || E_icode == IPOPL) && (E_dstM == d_srcA || E_dstM == d_srcB))
                D_stall = true;
            else
                D_stall = false;

            if ((E_icode == IJXX && !e_Cnd) || !((E_icode == IMRMOVL || E_icode == IPOPL) && (E_dstM == d_srcA || E_dstM == d_srcB)) && (IRET == D_icode || IRET == E_icode || IRET == M_icode))
                D_bubble = true;
            else
                D_bubble = false;

            E_stall = false;
            if ((E_icode == IJXX && !e_Cnd) || ((E_icode == IMRMOVL || E_icode == IPOPL) && (E_dstM == d_srcA || E_dstM == d_srcB)))
                E_bubble = true;
            else
                E_bubble = false;

            M_stall = false;
            if (m_stat == SADR || m_stat == SINS || m_stat == SHLT || W_stat == SADR || W_stat == SINS || W_stat == SHLT)
                M_bubble = true;
            else
                M_bubble = false;

            W_bubble = false;
            if (W_stat == SADR || W_stat == SINS || W_stat == SHLT)
                W_stall = true;
            else
                W_stall = false;
            
        }
        #endregion

        #region GeneratedNextPipeStateByControlSignal
        void GeneratedNextPipeStateByControlSignal()
        {
            if(!F_stall)
            {
                F_predPC = f_predPC;
            }
            
            if(!D_stall)
            {
                D_stat = f_stat;
                D_icode = f_icode;
                D_ifun = f_ifun;
                D_rA = f_rA;
                D_rB = f_rB;
                D_valC = f_valC;
                D_valP = f_valP;
            }
            if(D_bubble)
            {
                D_stat = SBUB;
                D_icode = INOP;
                D_ifun = FNONE;
                D_rA = RNONE;
                D_rB = RNONE;
                D_valC = 0;
                D_valP = 0;
            }

            if (E_bubble)
            {
                E_stat = SBUB;
                E_icode = INOP;
                E_ifun = FNONE;
                E_valC = E_valA = E_valB = RNOP;
                E_dstE = E_dstM = E_srcA = E_srcB = RNONE;
            }
            else
            {
                E_stat = d_stat;
                E_icode = d_icode;
                E_ifun = d_ifun;
                E_valC = d_valC;
                E_valA = d_valA;
                E_valB = d_valB;
                E_dstE = d_dstE;
                E_dstM = d_dstM;
                E_srcA = d_srcA;
                E_srcB = d_srcB;
            }

            if (M_bubble)
            {
                M_stat = SBUB;
                M_icode = INOP;
                M_Cnd = false;
                M_valE = M_valA = RNOP;
                M_dstE = M_dstM = RNONE;
            }
            else
            {
                M_stat = e_stat;
                M_icode = e_icode;
                M_Cnd = e_Cnd;
                M_valE = e_valE;
                M_valA = e_valA;
                M_dstE = e_dstE;
                M_dstM = e_dstM;
            }

            if (!W_stall)
            {
                W_stat = m_stat;
                W_icode = m_icode;
                W_valE = m_valE;
                W_valM = m_valM;
                W_dstE = m_dstE;
                W_dstM = m_dstM;
            }
        }
        #endregion

        #region GenerateNormalState
        void GenerateNormalState()
        {
            Write_back();
            Memory();
            Execute();
            Decode();
            Fetch();    
        }
        #endregion

        public void Run()
        {
            cycle_cnt++;
            ConvertData();
            GenerateNormalState();
            GeneratedStateControlSignal();
            GeneratedNextPipeStateByControlSignal();
            ConvertViewModel();
        }

        #region display
        public string vD_icode;
        public string vM_icode;
        public string vW_icode;
        public string vE_icode;
        public string vD_stat;
        public string vD_ifun;
        public string vD_rA;
        public string vD_rB;
        public string vD_valC;
        public string vD_valP;
        public string vE_stat;
        public string vE_ifun;
        public string vE_valC;
        public string vE_valA;
        public string vE_valB;
        public string vE_dstE;
        public string vE_dstM;
        public string vE_srcA;
        public string vE_srcB;
        public string vM_stat;
        public string vM_Cnd;
        public string vM_valE;
        public string vM_valA;
        public string vM_dstE;
        public string vM_dstM;
        public string vW_stat;
        public string vW_valE;
        public string vW_valM;
        public string vW_dstE;
        public string vW_dstM;
        public string vrunSpeed;
        public string vcode;
        public string vask;
        public string vans;

        public string veax;
        public string vecx;
        public string vedx;
        public string vebx;
        public string vesp;
        public string vebp;
        public string vesi;
        public string vedi;
        public string vcycle_cnt;

        public void ConvertViewModel()
        {
            VD_icode = D_icode.ToString("X");
            VE_icode = E_icode.ToString("X");
            //Console.WriteLine(D_icode);
            VM_icode = M_icode.ToString("X");
            VW_icode = W_icode.ToString("X");
            VD_stat = D_stat.ToString("X");
            VD_ifun = D_ifun.ToString("X");
            VD_rA = D_rA.ToString("X");
            VD_rB = D_rB.ToString("X");
            VD_valC = D_valC.ToString("X");
            VD_valP = D_valP.ToString("X");
            VE_stat = E_stat.ToString("X");
            VE_ifun = E_ifun.ToString("X");
            VE_valC = E_valC.ToString("X");
            VE_valA = E_valA.ToString("X");
            VE_valB = E_valB.ToString("X");
            VE_dstE = E_dstE.ToString("X");
            VE_dstM = E_dstM.ToString("X");
            VE_srcA = E_srcA.ToString("X");
            VE_srcB = E_srcB.ToString("X");
            VM_stat = M_stat.ToString("X");
            VM_Cnd = M_Cnd.ToString();
            VM_valE = M_valE.ToString("X");
            VM_valA = M_valA.ToString("X");
            VM_dstE = M_dstE.ToString("X");
            VM_dstM = M_dstM.ToString("X");
            VW_stat = W_stat.ToString("X");
            VW_valE = W_valE.ToString("X");
            VW_valM = W_valM.ToString("X");
            VW_dstE = W_dstE.ToString("X");
            VW_dstM = W_dstM.ToString("X");
            VrunSpeed = runSpeed.ToString();
            Vcycle_cnt = cycle_cnt.ToString();
            long eax = reg_file[0];
            long ecx = reg_file[1];
            long edx = reg_file[2];
            long ebx = reg_file[3];
            long esp = reg_file[4];
            long ebp = reg_file[5];
            long esi = reg_file[6];
            long edi = reg_file[7];

            Veax = eax.ToString("X");
            Vecx = ecx.ToString("X");
            Vedx = edx.ToString("X");
            Vebx = ebx.ToString("X");
            Vesp = esp.ToString("X");
            Vebp = ebp.ToString("X");
            Vesi = esi.ToString("X");
            Vedi = edi.ToString("X");


        }

        public void ConvertData()
        {
            //D_icode = Convert.ToInt64(VD_icode);
           // E_icode = Convert.ToInt64(VE_icode);
            //M_icode = Convert.ToInt64(VM_icode);
           // W_icode = Convert.ToInt64(VW_icode);
            //runSpeed = Convert.ToInt64(VrunSpeed);
            /*
            D_ifun = Convert.ToInt64(VD_ifun);
            D_rA = Convert.ToInt64(VD_rA);
            D_rB = Convert.ToInt64(VD_rB);
            D_valC = Convert.ToInt64(VD_valC);
            D_valP = Convert.ToInt64(VD_valP);     
            E_ifun = Convert.ToInt64(VE_ifun);
            E_valC = Convert.ToInt64(VE_valC);
            E_valA = Convert.ToInt64(VE_valA);
            E_valB = Convert.ToInt64(VE_valB);
            E_dstE = Convert.ToInt64(VE_dstE);
            E_dstM = Convert.ToInt64(VE_dstM);
            E_srcA = Convert.ToInt64(VE_srcA);
            E_srcB = Convert.ToInt64(VE_srcB);
            M_stat = Convert.ToInt64(VM_stat);
            M_valE = Convert.ToInt64(VM_valE);
            M_valA = Convert.ToInt64(VM_valA);
            M_dstE = Convert.ToInt64(VM_dstE);
            M_dstM = Convert.ToInt64(VM_dstM);        
            W_valE = Convert.ToInt64(VW_valE);
            W_valM = Convert.ToInt64(VW_valM);
            W_dstE = Convert.ToInt64(VW_dstE);
            W_dstM = Convert.ToInt64(VW_dstM);
            */
            /*
            reg_file[0] = Convert.ToInt64(Veax);
            reg_file[1] = Convert.ToInt64(vecx);
            reg_file[2] = Convert.ToInt64(Vedx);
            reg_file[3] = Convert.ToInt64(Vebx);
            reg_file[4] = Convert.ToInt64(Vesp);
            reg_file[5] = Convert.ToInt64(Vebp);
            reg_file[6] = Convert.ToInt64(Vesi);
            reg_file[7] = Convert.ToInt64(Vedi);
            */

        }


        public string VD_icode { get => vD_icode; set { vD_icode = value; RaisePropertyChanged(() => VD_icode); } }
        public string VE_icode { get => vE_icode; set { vE_icode = value; RaisePropertyChanged(() => VE_icode); } }
        public string VM_icode { get => vM_icode; set { vM_icode = value; RaisePropertyChanged(() => VM_icode); } }
        public string VW_icode { get => vW_icode; set { vW_icode = value; RaisePropertyChanged(() => VW_icode); } }
        public string VD_stat { get => vD_stat; set { vD_stat = value; RaisePropertyChanged(() => VD_stat); } }
        public string VD_ifun { get => vD_ifun; set { vD_ifun = value; RaisePropertyChanged(() => VD_ifun); } }
        public string VD_rA { get => vD_rA; set { vD_rA = value; RaisePropertyChanged(() => VD_rA); } }
        public string VD_rB { get => vD_rB; set { vD_rB = value; RaisePropertyChanged(() => VD_rB); } }
        public string VD_valC { get => vD_valC; set { vD_valC = value; RaisePropertyChanged(() => VD_valC); } }
        public string VD_valP { get => vD_valP; set { vD_valP = value; RaisePropertyChanged(() => VD_valP); } }
        public string VE_stat { get => vE_stat; set { vE_stat = value; RaisePropertyChanged(() => VE_stat); } }
        public string VE_ifun { get => vE_ifun; set { vE_ifun = value; RaisePropertyChanged(() => VE_ifun); } }
        public string VE_valC { get => vE_valC; set { vE_valC = value; RaisePropertyChanged(() => VE_valC); } }
        public string VE_valA { get => vE_valA; set { vE_valA = value; RaisePropertyChanged(() => VE_valA); } }
        public string VE_valB { get => vE_valB; set { vE_valB = value; RaisePropertyChanged(() => VE_valB); } }
        public string VE_dstE { get => vE_dstE; set { vE_dstE = value; RaisePropertyChanged(() => VE_dstE); } }
        public string VE_dstM { get => vE_dstM; set { vE_dstM = value; RaisePropertyChanged(() => VE_dstM); } }
        public string VE_srcA { get => vE_srcA; set { vE_srcA = value; RaisePropertyChanged(() => VE_srcA); } }
        public string VE_srcB { get => vE_srcB; set { vE_srcB = value; RaisePropertyChanged(() => VE_srcB); } }
        public string VM_stat { get => vM_stat; set { vM_stat = value; RaisePropertyChanged(() => VM_stat); } }
        public string VM_Cnd { get => vM_Cnd; set { vM_Cnd = value; RaisePropertyChanged(() => VM_Cnd); } }
        public string VM_valE { get => vM_valE; set { vM_valE = value; RaisePropertyChanged(() => VM_valE); } }
        public string VM_valA { get => vM_valA; set { vM_valA = value; RaisePropertyChanged(() => VM_valA); } }
        public string VM_dstE { get => vM_dstE; set { vM_dstE = value; RaisePropertyChanged(() => VM_dstE); } }
        public string VM_dstM { get => vM_dstM; set { vM_dstM = value; RaisePropertyChanged(() => VM_dstM); } }
        public string VW_stat { get => vW_stat; set { vW_stat = value; RaisePropertyChanged(() => VW_stat); } }
        public string VW_valE { get => vW_valE; set { vW_valE = value; RaisePropertyChanged(() => VW_valE); } }
        public string VW_valM { get => vW_valM; set { vW_valM = value; RaisePropertyChanged(() => VW_valM); } }
        public string VW_dstE { get => vW_dstE; set { vW_dstE = value; RaisePropertyChanged(() => VW_dstE); } }
        public string VW_dstM { get => vW_dstM; set { vW_dstM = value; RaisePropertyChanged(() => VW_dstM); } }
        public string VrunSpeed { get => vrunSpeed; set { vrunSpeed = value; RaisePropertyChanged(() => VrunSpeed); } }
        public string Vcode { get => vcode; set { vcode = value; RaisePropertyChanged(() => Vcode); } }
        public string Vask { get => vask; set { vask = value; RaisePropertyChanged(() => Vask); } }
        public string Vans { get => vans; set { vans = value; RaisePropertyChanged(() => Vans); } }

        public string Veax { get => veax; set { veax = value; RaisePropertyChanged(() => Veax); } }
        public string Vecx { get => vecx; set { vecx = value; RaisePropertyChanged(() => Vecx); } }
        public string Vedx { get => vedx; set { vedx = value; RaisePropertyChanged(() => Vedx); } }
        public string Vebx { get => vebx; set { vebx = value; RaisePropertyChanged(() => Vebx); } }
        public string Vesp { get => vesp; set { vesp = value; RaisePropertyChanged(() => Vesp); } }
        public string Vebp { get => vebp; set { vebp = value; RaisePropertyChanged(() => Vebp); } }
        public string Vesi { get => vesi; set { vesi = value; RaisePropertyChanged(() => Vesi); } }
        public string Vedi { get => vedi; set { vedi = value; RaisePropertyChanged(() => Vedi); } }

        public string Vcycle_cnt { get => vcycle_cnt; set { vcycle_cnt = value; RaisePropertyChanged(() => Vcycle_cnt); } }
        

        public void test()
        {
            icode = "5487";
        }

        public string Icode
        {
            get => icode; set
            {
                icode = value;
                RaisePropertyChanged(() => Icode);
            }
        }

        public void GetMemData()
        {
            Vans = mem.ReadADoubleWord(Convert.ToInt64(Vask)).ToString("X");
        }

        string icode = "i";
        #endregion
    }



    #region MEM
    //存储器接口
    interface IMem
    {
        byte Read(Int64 addr);
        void Write(Int64 addr, byte data);
    }

    //主存
    class MemArr : IMem
    {
        //内存大小
        private const Int64 MAX_MEM = 0x1000;
        private byte[] arr;

        //内存构造函数
        public MemArr()
        {
            //init
            arr = new byte[MAX_MEM];
           
            Array.Clear(arr, 0, arr.Length);
        }

        //内存清零，初始化
        public void Clear()
        {
            Array.Clear(arr, 0, arr.Length);
        }

        //整块写入内存
        public void SetMem(byte[] init_array)
        {
            Int64 len = init_array.Length;
            for(Int64 i = 0;i < len; i++)
            {
                Write(i, init_array[i]);
            }
        }

        //阅读addr地址的一个字节数据
        public byte Read(Int64 addr)
        {
            return arr[addr];
        }

        public Int64 ReadADoubleWord(Int64 addr)
        {
            Int64 m_valM = 0;
            for (int i = 0; i < 4; i++)
            {
                m_valM += this.Read(addr - i) << (8 * i);
            }
            return m_valM;

        }

        //写入一个字节到addr中
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
