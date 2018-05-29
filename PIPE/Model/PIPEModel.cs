using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using GalaSoft.MvvmLight;
using System.IO;
using System.Windows;

namespace Y86vmWpf.Model
{
    public class PIPEModel : ObservableObject
    {


        #region DefineControlParameter
        //启停控制
        bool isRun = false;
        //用此变量控制运行速度
        long runSpeed = 200;
        #endregion

        #region DefineData
        //数据冒险解决方式计数
        Int64 forward_num, stall_num, bubble_num;
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
        //流水线控制信号
        bool F_stall, F_bubble, D_stall, D_bubble, E_stall, E_bubble, M_stall, M_bubble, W_stall, W_bubble;
        //条件码
        bool ZF, SF, OF;                        
        Int64 aluA, aluB;
        Int64 alufun;
        bool set_cc, imem_error, instr_valid;
        Int64 mem_addr;
        bool mem_read, mem_write;
       
        
        const byte INOP = 0;
        const byte IHALT = 1;
        const byte IRRMOVL = 2;
        const byte IIRMOVL = 3;
        const byte IRMMOVL = 4;
        const byte IMRMOVL = 5;
        const byte IOPL = 6;
        const byte IJXX = 7;
        const byte ICALL = 8;
        const byte IRET = 9;
        const byte IPUSHL = 0xA;
        const byte IPOPL = 0xB;
        const byte FNONE = 0;
        const byte ALUADD = 0;
        const byte SAOK = 1;
        const byte SADR = 2;
        const byte SINS = 3;
        const byte SHLT = 4;
        const byte SBUB = 5;
        const byte SSTA = 6;
        const byte REAX = 0;
        const byte RECX = 1;
        const byte REDX = 2;
        const byte REBX = 3;
        const byte RESP = 4;
        const byte REBP = 5;
        const byte RESI = 6;
        const byte REDI = 7;
        const byte RNONE = 0xF;
        const byte RNOP = 0;

        //与界面ui的通信
        public long RunSpeed { get => runSpeed; set => runSpeed = value; }
        public bool IsRun { get => isRun; set => isRun = value; }
        #endregion

        #region Init
        public PIPEModel()
        {
            
        }

        //模拟器初始化
        public void InitAll()
        {
            tools = new ITools();
            Init_PIPEModel();
            ReadFromFile();
        }

        //初始化模型数值
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
            //无寄存器
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
            mem = new MemArr();
            reg_file = new Int64[9];
            Array.Clear(reg_file, 0, reg_file.Length);
            //栈指针初始化
            reg_file[4] = 300;
            Vesp = vesp = "300";
            bubble_num = stall_num = forward_num = 0;

            
        }

        //从二进制文件中读取代码
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

        //从输入框中读取代码
        public void ReadFromText()
        {
            //jmp 487512
            mem.Clear();
            string code = ccode;
            for(int i=0;i+1<code.Length;)
            {
                int temp = 0;
                int precode, aftcode;
                //做十六进制处理
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
                //加一个字节
                i += 2;
                Console.WriteLine(temp);
            }
        }
        #endregion

        #region Fetch
        void Fetch()
        {

            //选择PC值
            if (M_icode == IJXX && !M_Cnd)
                f_pc = M_valA;
            else if (W_icode == IRET)
                f_pc = W_valM;
            else
                f_pc = F_predPC;

            //得到指令
            byte imem = mem.Read(f_pc);
            byte[] temp = new byte[2];
            temp = tools.ByteSplit(imem);
            imem_icode = temp[0];
            imem_ifun = temp[1];

            bool instr_valid, imem_error, need_regids,need_valC;

            //异常处理
            if(imem_icode > MAX_ICODE)
            {
                imem_error = true;
                MessageBox.Show("操作码错误");
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
                MessageBox.Show("内存访问越界");
                instr_valid = false;
                f_icode = INOP;
                f_ifun = FNONE;
            }

            if (imem_error)
            {
                f_stat = SADR;
                MessageBox.Show("内存访问越界");
            }
            else if (!instr_valid)
            {
                f_stat = SINS;
                MessageBox.Show("指令无效");
            }
            else if (f_icode == IHALT)
                f_stat = SHLT;
            else
                f_stat = SAOK;

            //异常处理，指令是否有效
            if (f_icode == INOP || f_icode == IHALT || f_icode == IRRMOVL || f_icode == IIRMOVL || f_icode == IRMMOVL || f_icode == IMRMOVL || f_icode == IOPL || f_icode == IJXX || f_icode == ICALL || f_icode == IRET || f_icode == IPUSHL || f_icode == IPOPL)
                instr_valid = true;
            else
                instr_valid = false;
            //此指令是否需要寄存器
            if (f_icode == IRRMOVL || f_icode == IOPL || f_icode == IPUSHL || f_icode == IPOPL || f_icode == IIRMOVL || f_icode == IRMMOVL || f_icode == IMRMOVL)
                need_regids = true;
            else
                need_regids = false;
            //指令是否需要立即数
            if (f_icode == IIRMOVL || f_icode == IRMMOVL || f_icode == IMRMOVL || f_icode == IJXX || f_icode == ICALL)
                need_valC = true;
            else
                need_valC = false;

            if (W_stat != SHLT)
                f_pc += 1;

            f_valC = 0;

            //读寄存器
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

            //读立即数
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

            //预测PC值
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
            //选择源A                
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
            //选择源B
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
            //选择源E
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

            //选择源M，取决于是否是间接寻址
            if (D_icode == IMRMOVL || D_icode == IPOPL)
                d_dstM = D_rA;
            else
                d_dstM = RNONE;


            

            //检测是否需要数据转发
            if (D_icode == ICALL)
            {
                d_valA = D_valP;
               
            }
            else if(D_icode == IJXX)
            {
                d_valA = D_valC;
            }
            else if (d_srcA == e_dstE)
            {
                d_valA = e_valE;
               
            }
            else if (d_srcA == M_dstM)
            {
                d_valA = m_valM;
            }
            else if (d_srcA == M_dstE)
            {
                d_valA = M_valE;
            }
            else if (d_srcA == W_dstM)
            {
                d_valA = W_valM;
            }
            else if (d_srcA == W_dstE)
            {
                d_valA = W_valE;
            }
            else
            {
                forward_num--;
                d_valA = reg_file[d_srcA];
            }

            forward_num++;
            //检测是否需要数据转发
            if (d_srcB == e_dstE)
            {
                d_valB = e_valE;
            }
            else if (d_srcB == M_dstM)
            {
                d_valB = m_valM;
            }
            else if (d_srcB == M_dstE)
            {
                d_valB = M_valE;
            }
            else if (d_srcB == W_dstM)
            {
                d_valB = W_valM;
            }
            else if (d_srcB == W_dstE)
            {
                d_valB = W_valE;
            }
            else
            {
                forward_num--;
                d_valB = reg_file[d_srcB];            }
        }
        #endregion

        #region Execute
        void Execute()
        {
            e_stat = E_stat;
            e_icode = E_icode;

            //选择运算数A
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
            //选择运算数B
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
            //ALU进行运算
            switch (alufun)
            {
                case 0: e_valE = aluB + aluA; break;
                case 1: e_valE = aluB - aluA; break;
                case 2: e_valE = aluB & aluA; break;
                case 3: e_valE = aluB ^ aluA; break;
                default: e_valE = 0; break;
            }

            //是否需要条件码
            if (E_icode == IOPL && m_stat != SADR && m_stat != SINS && m_stat != SHLT && W_stat != SADR && W_stat != SINS && W_stat != SHLT)
                set_cc = true;
            else
                set_cc = false;
            //产生下一个状态的valA
            e_valA = E_valA;
            //若不是条件移动，dstE置为0
            if (E_icode == IRRMOVL && !e_Cnd)
                e_dstE = RNONE;
            else
                e_dstE = E_dstE;
            //设置条件码
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
            //根据指令决定是否符合条件           
            if (E_icode == IJXX || E_icode == IRRMOVL)
            {
                switch (E_ifun)
                {
                    case 0: e_Cnd = true; break;//jmp rrmovl
                    case 1: if (ZF || SF) e_Cnd = true; else e_Cnd = false; break;//jle cmovle
                    case 2: if (SF) e_Cnd = true; else e_Cnd = false; break;//jl cmovl
                    case 3: if (ZF) e_Cnd = true; else e_Cnd = false; break;//je comve
                    case 4: if (!ZF) e_Cnd = true; else e_Cnd = false; break;//jne cmovne
                    case 5: if (!SF) e_Cnd = true; else e_Cnd = false; break;//jge comvge
                    case 6: if (!ZF && !SF) e_Cnd = true; else e_Cnd = false; break;//jg comvg
                }
            }
            else
                e_Cnd = false;
            //传递数值
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
            //选择访存的地址
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
           
            //是否需要读                    
            if (M_icode == IMRMOVL || M_icode == IPOPL || M_icode == IRET)
                mem_read = true;
            else
                mem_read = false;
            //是否需要写
            if (M_icode == IRMMOVL || M_icode == IPUSHL || M_icode == ICALL)
                mem_write = true;
            else
                mem_write = false;
            //异常检查
            if (mem_addr > MAX_MEM)
                dmem_error = true;
            else
            {
                dmem_error = false;
                //读一个数
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

                //写一个数
                if (mem_write)
                {
                    for (int i = 0; i < 4; i++)                       
                        mem.Write(mem_addr - i, (byte)((M_valA >> (8 * i)) & 0xff));
                }
            }
            //异常表示
            if (dmem_error)
                m_stat = SADR;
            else
                m_stat = M_stat;
            
        }
        #endregion

        #region Write back
        void Write_back()
        {
            //使dstE为寄存器id
            w_dstE = W_dstE;
            //设置E的值
            w_valE = W_valE;
            //同E
            w_dstM = W_dstM;
            //同M
            w_valM = W_valM;
            //气泡状态的处理
            if (W_stat == SBUB)
                Stat = SAOK;
            else
                Stat = W_stat;

            //正常情况下，更新寄存器
            if (Stat == SAOK)
            {
                if (W_dstE != RNONE)
                    reg_file[W_dstE] = W_valE;
                if (W_dstM != RNONE)
                    reg_file[W_dstM] = W_valM;
            }
        }
        #endregion

        #region GeneratedControlSignal
        //产生控制信号
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

            bubble_num += Convert.ToInt32(F_bubble) + Convert.ToInt32(D_bubble) + Convert.ToInt32(E_bubble) + Convert.ToInt32(M_bubble) + Convert.ToInt32(W_bubble);

            stall_num += Convert.ToInt32(F_stall) + Convert.ToInt32(D_stall) + Convert.ToInt32(E_stall) + Convert.ToInt32(M_stall) + Convert.ToInt32(W_stall);


        }
        #endregion

        #region GeneratedNextPipeStateByControlSignal
        void GeneratedNextPipeStateByControlSignal()
        {
            //若F正常
            if(!F_stall)
            {
                F_predPC = f_predPC;
            }
            //若D正常
            if(!D_stall)
            {
                //传正常值
                D_stat = f_stat;
                D_icode = f_icode;
                D_ifun = f_ifun;
                D_rA = f_rA;
                D_rB = f_rB;
                D_valC = f_valC;
                D_valP = f_valP;
            }
            //若D需要置入气泡
            if(D_bubble)
            {
                //传入空值，对接下来的状态没有任何作用
                D_stat = SBUB;
                D_icode = INOP;
                D_ifun = FNONE;
                D_rA = RNONE;
                D_rB = RNONE;
                D_valC = RNOP;
                D_valP = RNOP;
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
            //写回
            Write_back();
            //访存
            Memory();
            //执行
            Execute();
            //解码
            Decode();
            //取指令
            Fetch();    
        }
        #endregion

        public void Run()
        {
            //周期数加一
            cycle_cnt++;
            //将显示区更改的数据写入内存区
            ConvertData();
            //产生流水线寄存器数值
            GenerateNormalState();
            //根据数据冒险条件产生控制信号（气泡，暂停）
            GeneratedStateControlSignal();
            //根据控制信号产生传递各寄存器的数值
            GeneratedNextPipeStateByControlSignal();
            //将内存区更改的数据写入显示区
            ConvertViewModel();
        }

        //用于和ui通信
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

        public string vW_s;
        public string vM_s;
        public string vE_s;
        public string vD_s;
        public string vF_s;

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
            VM_icode = M_icode.ToString("X");
            VW_icode = W_icode.ToString("X");
            VD_stat = D_stat.ToString("X");
            VD_ifun = D_ifun.ToString("X");
            VD_rA = D_rA.ToString();
            VD_rB = D_rB.ToString();
            VD_valC = D_valC.ToString();
            VD_valP = D_valP.ToString();
            VE_stat = E_stat.ToString();
            VE_ifun = E_ifun.ToString();
            VE_valC = E_valC.ToString();
            VE_valA = E_valA.ToString();
            VE_valB = E_valB.ToString();
            VE_dstE = E_dstE.ToString();
            VE_dstM = E_dstM.ToString();
            VE_srcA = E_srcA.ToString();
            VE_srcB = E_srcB.ToString();
            VM_stat = M_stat.ToString();
            VM_Cnd = M_Cnd.ToString();
            VM_valE = M_valE.ToString();
            VM_valA = M_valA.ToString();
            VM_dstE = M_dstE.ToString();
            VM_dstM = M_dstM.ToString();
            VW_stat = W_stat.ToString();
            VW_valE = W_valE.ToString();
            VW_valM = W_valM.ToString();
            VW_dstE = W_dstE.ToString();
            VW_dstM = W_dstM.ToString();
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

            Veax = eax.ToString();
            Vecx = ecx.ToString();
            Vedx = edx.ToString();
            Vebx = ebx.ToString();
            Vesp = esp.ToString();
            Vebp = ebp.ToString();
            Vesi = esi.ToString();
            Vedi = edi.ToString();



            

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
           
            reg_file[0] = Convert.ToInt64(Veax);
            reg_file[1] = Convert.ToInt64(vecx);
            reg_file[2] = Convert.ToInt64(Vedx);
            reg_file[3] = Convert.ToInt64(Vebx);
            reg_file[4] = Convert.ToInt64(Vesp);
            reg_file[5] = Convert.ToInt64(Vebp);
            reg_file[6] = Convert.ToInt64(Vesi);
            reg_file[7] = Convert.ToInt64(Vedi);
            

            VW_s = VM_s = VE_s = VD_s = VF_s = "normal".ToString();


            if (W_bubble)
                VW_s = "bubble";
            if (W_stall)
                VW_s = "stall";

            if (M_bubble)
                VM_s = "bubble";
            if (M_stall)
                VM_s = "stall";

            if (E_bubble)
                VE_s = "bubble";
            if (E_stall)
                VE_s = "stall";

            if (D_bubble)
                VD_s = "bubble";
            if (D_stall)
                VD_s = "stall";

            if (F_bubble)
                VF_s = "bubble";
            if (F_stall)
                VF_s = "stall";

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

        public string VW_s { get => vW_s; set { vW_s = value; RaisePropertyChanged(() => VW_s); } }
        public string VM_s { get => vM_s; set { vM_s = value; RaisePropertyChanged(() => VM_s); } }
        public string VE_s { get => vE_s; set { vE_s = value; RaisePropertyChanged(() => VE_s); } }
        public string VD_s { get => vD_s; set { vD_s = value; RaisePropertyChanged(() => VD_s); } }
        public string VF_s { get => vF_s; set { vF_s = value; RaisePropertyChanged(() => VF_s); } }

        
        
        
        
        

        public string Veax { get => veax; set { veax = value; RaisePropertyChanged(() => Veax); } }
        public string Vecx { get => vecx; set { vecx = value; RaisePropertyChanged(() => Vecx); } }
        public string Vedx { get => vedx; set { vedx = value; RaisePropertyChanged(() => Vedx); } }
        public string Vebx { get => vebx; set { vebx = value; RaisePropertyChanged(() => Vebx); } }
        public string Vesp { get => vesp; set { vesp = value; RaisePropertyChanged(() => Vesp); } }
        public string Vebp { get => vebp; set { vebp = value; RaisePropertyChanged(() => Vebp); } }
        public string Vesi { get => vesi; set { vesi = value; RaisePropertyChanged(() => Vesi); } }
        public string Vedi { get => vedi; set { vedi = value; RaisePropertyChanged(() => Vedi); } }

        public string Vcycle_cnt { get => vcycle_cnt; set { vcycle_cnt = value; RaisePropertyChanged(() => Vcycle_cnt); } }
        

        public void GetMemData()
        {
            Vans = mem.ReadADoubleWord(Convert.ToInt64(Vask)).ToString("X");
        }
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

    //工具类
    public class ITools
    {
        //一字节拆分为两个半字节数字
        public byte[] ByteSplit(Byte num)
        {
            byte[] res = new byte[2];
            res[0] = (byte)((num >> 4) & (0xF));
            res[1] = (byte)(num & 0xF);
            return res;
        }
    }

}
