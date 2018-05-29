using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PIPE.Model
{
    public class Assembler
    {
        string[] four_bits;
        string[] instr_data;
        string[] two_val;
        char[] disass;
        string[] reg = { "%eax", "%ecx", "%edx", "%ebx", "%esp", "%ebp", "%esi", "%edi" };
        string[] op = { "addl", "subl", "andl", "xorl" };
        string[] cmp = { "le", "l", "e", "ne", "ge", "g" };


        //汇编器各部分
        public Assembler()
        {
            four_bits = new string[12];
            instr_data = new string[2];
            two_val = new string[2];
            disass = new char[12];
        }

        //选择操作吗
        void split_instr_data(string s)
        {
            string[] a = new string[2];
            int idx = s.IndexOf(' ');
            instr_data[0] = a[0] = s.Substring(0, idx).Trim();
            if (a[0] == "halt")
            {
                four_bits[0] = "1";
                instr_data[1] = "";
                return;
            }
            else if (a[0] == "nop")
            {
                four_bits[0] = "0";
                instr_data[1] = "";
                return;
            }
            else if (a[0] == "ret")
            {
                four_bits[0] = "9";
                instr_data[1] = "";
                return;
            }
            instr_data[1] = s.Substring(idx).Trim();
            if (a[0] == "rrmovl")
                four_bits[0] = "2";
            else if (a[0] == "irmovl")
                four_bits[0] = "3";
            else if (a[0] == "rmmovl")
                four_bits[0] = "4";
            else if (a[0] == "mrmovl")
                four_bits[0] = "5";
            else if (a[0] == "addl" || a[0] == "subl" || a[0] == "andl" || a[0] == "xorl")
                four_bits[0] = "6";
            else if (a[0][0] == 'j')
                four_bits[0] = "7";
            else if (a[0] == "call")
                four_bits[0] = "8";
            else if (a[0].Substring(0, 4) == "cmov")
                four_bits[0] = "2";
            else if (a[0] == "pushl")
                four_bits[0] = "a";
            else if (a[0] == "popl")
                four_bits[0] = "b";
            else MessageBox.Show("指令无法识别");
        }

        //对于j，comv选择操作码的解释
        string get_ifun(string instr)
        {
            if (instr == "jmp" || instr == "rrmovl") return "0";
            else if (instr.Contains("le")) return "1";
            else if (instr.Contains("l")) return "2";
            else if (instr.Contains("ne")) return "4";
            else if (instr.Contains("ge")) return "5";
            else if (instr.Contains("g")) return "6";
            else if (instr.Contains("e")) return "3";
            else return "";
        }

        //选择操作码的解释
        void assemble_rest1()
        {
            switch (Convert.ToInt32(four_bits[0], 16))
            {
                case 0:
                case 1:
                case 3:
                case 4:
                case 5:
                case 8:
                case 9:
                case 10:
                case 11:
                    four_bits[1] = "0";
                    break;
                case 6:
                    {
                        if (instr_data[0] == "addl") four_bits[1] = "0";
                        else if (instr_data[0] == "subl") four_bits[1] = "1";
                        else if (instr_data[0] == "andl") four_bits[1] = "2";
                        else if (instr_data[0] == "xorl") four_bits[1] = "3";
                    }
                    break;
                case 2:
                case 7:
                    four_bits[1] = get_ifun(instr_data[0]);
                    break;
            }
        }

        //给寄存器编号
        string trans_reg_file_addr(string reg)
        {
            if (reg == "%eax") return "0";
            else if (reg == "%ecx") return "1";
            else if (reg == "%edx") return "2";
            else if (reg == "%ebx") return "3";
            else if (reg == "%esp") return "4";
            else if (reg == "%ebp") return "5";
            else if (reg == "%esi") return "6";
            else if (reg == "%edi") return "7";
            else return "";
        }

        //区分直接寻址，简介寻址
        void get_reg_file(string data)
        {
            int idx = data.IndexOf(',');
            two_val[0] = data.Substring(0, idx).Trim();
            two_val[1] = data.Substring(idx + 1).Trim();
            string ra, rb;
            ra = two_val[0];
            rb = two_val[1];
            if (ra.Contains('('))
            {
                int i = ra.IndexOf('(');
                ra = ra.Substring(i + 1, 4);
            }
            if (rb.Contains('('))
            {
                int i = rb.IndexOf('(');
                rb = rb.Substring(i + 1, 4);
            }
            four_bits[2] = trans_reg_file_addr(ra);
            four_bits[3] = trans_reg_file_addr(rb);
        }

        //汇编寄存器号
        void assemble_rest2_3()
        {
            switch (Convert.ToInt32(four_bits[0], 16))
            {
                case 0:
                case 1:
                case 7:
                case 8:
                case 9:
                    return;
                case 2:
                case 4:
                case 5:
                case 6:
                    get_reg_file(instr_data[1]);
                    break;
                case 3:
                    get_reg_file(instr_data[1]);
                    four_bits[2] = "F";
                    break;
                case 10:
                case 11:
                    four_bits[2] = trans_reg_file_addr(instr_data[1].Trim());
                    four_bits[3] = "F";
                    break;
            }
        }

        //读立即数
        void get_constant_val(string val, ref string[] bits)
        {
            int temp = Convert.ToInt32(val);
            val = temp.ToString("x8");
            for (int i = 0; i < 7; i += 2)
            {
                bits[4 + i] = val[i].ToString();
                bits[4 + i + 1] = val[i+1].ToString();
            }
        }

        //读访存的地址
        void get_mem_addr(string addr, ref string[] bits)
        {
            addr = addr.Substring(2);
            int temp = Convert.ToInt32(addr, 16);
            addr = temp.ToString("x8");
            for (int i = 0; i < 7; i += 2)
            {
                bits[2 + i] = addr[i].ToString();
                bits[2 + i + 1] = addr[i+1].ToString();
            }
        }

        //区分是访存地址，还是立即数
        void assemble_rest_all()
        {
            switch (Convert.ToInt32(four_bits[0], 16))
            {
                case 3:
                    get_constant_val(two_val[0].Replace('$', ' ').Trim(), ref four_bits);
                    break;
                case 4:
                    int idx = two_val[1].IndexOf('(');
                    get_constant_val(two_val[1].Substring(0, idx), ref four_bits);
                    break;
                case 5:
                    idx = two_val[0].IndexOf('(');
                    get_constant_val(two_val[0].Substring(0, idx), ref four_bits);
                    break;
                case 7:
                    get_mem_addr(instr_data[1], ref four_bits);
                    break;
                case 8:
                    get_mem_addr(instr_data[1], ref four_bits);
                    break;
                default:
                    return;
            }
        }

        //汇编主流程
        public string assemble(string code)
        {
            string result = "";
            for (int i = 0; i < 12; i++)
            {
                four_bits[i] = "";
            }
            instr_data[0] = "";
            instr_data[1] = "";
            split_instr_data(code);
            assemble_rest1();
            assemble_rest2_3();
            assemble_rest_all();
            for (int i = 0; i < 12; i++)
            {
                result += four_bits[i];
            }
            return result;
        }


    }
}
