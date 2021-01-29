using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using System.Numerics;

namespace DAQ168
{
    public partial class Main_Frm : Form
    {
        static string FilePaths = "D:/testfile/";
        static string ipstring;
        static string ipstring2;
        static string setmaskstring;
        static string setgatestring;
        static string FileLenths;
        static long FileLenth = 0;
        static string[] Beizhu = new string[16];
        static bool[] ChanSel = new bool[16];
        static bool Collect = new bool();

        static byte[] ChanNum1 = new byte[10000];

        static byte[] chamod = new byte[16];
        static Int32[] chacnt = new Int32[16];
        static int OldPocketSn = 0;
        static int RePocketSn = 0;
        static int LostPocketSn;
        static BigInteger TotalPacketSn;

        static FileStream[] fs1 = new FileStream[16];
        StreamReader sr = null;

        static bool NetStateFlag = false;
        static byte[] IpSetFrm = new byte[20];
        static bool NetSetIPFlag;
        static bool NetSetSampleFlag;
        static int NetDevIP1life = 0;


        public Main_Frm()
        {
            InitializeComponent();
            try
            {
                sr = new StreamReader("config.ini", System.Text.Encoding.Default);
            }
            catch
            {
                System.IO.File.Create("config.ini");
            }

            ChCnt = new System.Windows.Forms.Label[8] { ChCnt1, ChCnt2, ChCnt3, ChCnt4, ChCnt5, ChCnt6, ChCnt7, ChCnt8 };
            ChMod = new System.Windows.Forms.Label[8] { ChMod1, ChMod2, ChMod3, ChMod4, ChMod5, ChMod6, ChMod7, ChMod8 };
            DatLightCH = new System.Windows.Forms.Button[8] { DatLightCH1, DatLightCH2, DatLightCH3, DatLightCH4, DatLightCH5, DatLightCH6, DatLightCH7, DatLightCH8 };
            ChSel = new System.Windows.Forms.CheckBox[8] { ChSel1, ChSel2, ChSel3, ChSel4, ChSel5, ChSel6, ChSel7, ChSel8 };
            chtextBox = new System.Windows.Forms.TextBox[8] { chtextBox1, chtextBox2, chtextBox3, chtextBox4, chtextBox5, chtextBox6, chtextBox7, chtextBox8 };

            foreach (var DatLightCH_i in DatLightCH)
            {
                DatLightCH_i.BackgroundImage = Properties.Resources.rising_edge;
                DatLightCH_i.BackgroundImageLayout = ImageLayout.Stretch;
                DatLightCH_i.FlatStyle = FlatStyle.Flat;
                DatLightCH_i.FlatAppearance.BorderSize = 0;
            }

            ipstring = sr.ReadLine();
            FilePaths = sr.ReadLine();
            FileLenths = sr.ReadLine();
            Masktext.Text = sr.ReadLine();
            GateText.Text = sr.ReadLine();
            chtextBox1.Text = sr.ReadLine();
            chtextBox2.Text = sr.ReadLine();
            chtextBox3.Text = sr.ReadLine();
            chtextBox4.Text = sr.ReadLine();
            chtextBox5.Text = sr.ReadLine();
            chtextBox6.Text = sr.ReadLine();
            chtextBox7.Text = sr.ReadLine();
            chtextBox8.Text = sr.ReadLine();
            chtextBox9.Text = sr.ReadLine();
            chtextBox10.Text = sr.ReadLine();
            chtextBox11.Text = sr.ReadLine();
            chtextBox12.Text = sr.ReadLine();
            chtextBox13.Text = sr.ReadLine();
            chtextBox14.Text = sr.ReadLine();
            chtextBox15.Text = sr.ReadLine();
            chtextBox16.Text = sr.ReadLine();

            if (ipstring != null)
            {
                DeviceIPTBox.Text = ipstring;
            }
            else
            {
                DeviceIPTBox.Text = "0.0.0.0";
            }
            if (FilePaths != null)
            {
                FileSavePathTB.Text = FilePaths;
            }
            else
            {
                FileSavePathTB.Text = "";
            }
            if (FileLenths != null)
            {
                FileLengthTbox.Text = FileLenths;
            }
            else
            {
                FileLengthTbox.Text = "";
            }
            sr.Close();

            this.demoThread = new Thread(new ThreadStart(this.refreshUDPdat));

            this.demoThread.Start();
        }
       
        static void DataToSave(int lenth)
        {
            int i = 0;
            int mod = 0;

            bool docycl = true;
            int datalen = 0;
            int chazhi;

            TotalPacketSn ++;

            if (ChanNum1[0] != 0xaa)
            {
                RePocketSn += 1;
                while (docycl)
                {
                    switch (mod)
                    {
                        case 0: mod = (ChanNum1[i] == 0xaa) ? 1 : 0; i = i + 1; break;      //第一个字头
                        case 1: mod = (ChanNum1[i] == 0xbb) ? 2 : 0; i = i + 1; break;      //第二个字头
                        case 2: mod = (ChanNum1[i] == 0xff) ? 3 : 0; i = i + 1; break;      //是否为采集数据
                        case 3: docycl = false; break;
                        case 6: docycl = false; break;
                        default: docycl = false; break;
                    }
                    if (i >= lenth) //没有找到可用的文件头
                    {
                        docycl = false;
                    }
                }
                if (mod != 3)
                {
                    LostPocketSn++;
                    return;
                }
            }
            else
            {
                i += 3;
            }

            if ((ChanNum1[3] & 0x0f) == 8)
            { 
                chazhi = ChanNum1[1419] - OldPocketSn;
                if (chazhi != 1 && chazhi != -255)
                { 
                    LostPocketSn += 1;
                    RePocketSn = ChanNum1[1419] * 255 + ChanNum1[1419];
                }
                OldPocketSn = ChanNum1[1419];
            }
            
            /* 给数据显示刷新用的参数*/
            chamod[(ChanNum1[i] & 0x0F)] = (byte)(ChanNum1[i] & 0x80);
            chacnt[(ChanNum1[i] & 0x0F)] += 1408;

            /* 文件长度达到高时结束文件写入，并创建一个新的文件*/
            int ch = (ChanNum1[i] & 0x0F);
            if (chacnt[(ChanNum1[i] & 0x0F)] > (FileLenth))
            {
                fs1[ch].Flush();
                fs1[ch].Close();
                chacnt[ch] = 0;
                string NowTine = DateTime.Now.ToString("_yyyy_MM_dd_hhmmss");
                fs1[ch] = new FileStream(FilePaths + "ch" + (ch + 1)+ "_" + Beizhu[ch] + NowTine + ".dat", FileMode.Create, FileAccess.Write);
            }

            byte a;
            byte b = 0x55;
            byte c = 0x33;
            byte d = 0x0f;

            for (i = 10; i <= 1420; i++)
            {
                 a = ChanNum1[i];
                 a = (byte)(((a >> 1) & b) | ((a & b) << 1));
                 a = (byte)(((a >> 2) & c) | ((a & c) << 2));
                 a = (byte)(((a >> 4) & d) | ((a & d) << 4));
                 ChanNum1[i] = a;
            }

            datalen = ChanNum1[i + 6] * 0x100 + ChanNum1[7];
            if (fs1[ChanNum1[3] & 0x0f] == null)
                return;
            fs1[ChanNum1[3] & 0x0f].Write(ChanNum1, 10, 1408);
            fs1[ChanNum1[3] & 0x0f].Flush();

            return;
        }

        static void TcpClientRecDat()
        {
            int i;
            TcpClient tcpClient;
            try
            {
                tcpClient = new TcpClient(ipstring, 8000);
            }
            catch
            {
                MessageBox.Show("不能建立TCP连接");
                NetStateFlag = false;
                return;
            }
            tcpClient.ReceiveBufferSize = 100000;
            NetStateFlag = true;
            
            NetworkStream ns = tcpClient.GetStream();
            while (Collect)
            {         
                try
                {
                    ns.Read(ChanNum1, 0, 1446);
                    DataToSave(1446);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    Collect = false;
                    return;
                }
            }
            ns.Close();
            NetStateFlag = false;
            for (i = 0; i < 16; i++)       //创建需要创建的文件
            {
                if (ChanSel[i])
                {
                    fs1[i].Flush();
                    fs1[i].Close();
                    fs1[i] = null;
                }
            }
        }

        static void IpstringToChar(string Ipstring, byte[] Chars)
        {
            byte[] temp = new byte[20];
            IPAddress ip;
            try
            {
                ip = IPAddress.Parse(Ipstring);
                temp = ip.GetAddressBytes();
            }
            catch (Exception error)
            {
                Console.WriteLine("<IP地址输入格式错误> " + error.Message);
            }
            Chars[0] = temp[0];
            Chars[1] = temp[1];
            Chars[2] = temp[2];
            Chars[3] = temp[3];
        }

        void GenSampleModeDat(byte[] IpSetFrm, byte type, int mask)
        {
            int SampleState;
            int Mask = 1 << mask;
            if (0 != (SampleMode & Mask))
                SampleState = SampleMode & (~Mask);
            else
                SampleState = SampleMode | Mask;
            int sum = 0;
            IpSetFrm[0] = 0xaa;
            IpSetFrm[1] = 0xbb;
            IpSetFrm[2] = type;
            IpSetFrm[3] = 0x01;    // IP_TYPE
            IpSetFrm[4] = (byte)((SampleState >> 8) & 0xFF);
            IpSetFrm[5] = (byte)(SampleState & 0xFF);

            int i;
            for (i = 6; i < 18; i++)
            {
                sum += IpSetFrm[i];
            }
            i = sum / 0xff;
            IpSetFrm[18] = (byte)i;
            IpSetFrm[19] = (byte)sum;
        }

        static void GenIpSetDat(string Ipstring, string maskstring,string gate, byte[] IpSetFrm, byte type)
        {
            byte[] ipbytes = new byte[20];
            byte[] maskbytes = new byte[20];
            byte[] gatebytes = new byte[20];
            int i = 0;
            int sum = 0;

            IpSetFrm[0] = 0xaa;
            IpSetFrm[1] = 0xbb;
            IpSetFrm[2] = type;
            IpSetFrm[3] = 0x00;    // IP_TYPE
            IpSetFrm[4] = 0x00;
            IpSetFrm[5] = 0x00;

            IpstringToChar(Ipstring, ipbytes);
            IpSetFrm[6] = ipbytes[0];
            IpSetFrm[7] = ipbytes[1];
            IpSetFrm[8] = ipbytes[2];
            IpSetFrm[9] = ipbytes[3];

            IpstringToChar(maskstring, maskbytes);
            IpSetFrm[10] = maskbytes[0];
            IpSetFrm[11] = maskbytes[1];
            IpSetFrm[12] = maskbytes[2];
            IpSetFrm[13] = maskbytes[3];

            if (maskbytes[3] != 0)
            { 
                IpSetFrm[13] = 0;
                MessageBox.Show("子网掩码错误");
            } 

            IpstringToChar(gate, gatebytes);
            IpSetFrm[14] = gatebytes[0];
            IpSetFrm[15] = gatebytes[1];
            IpSetFrm[16] = gatebytes[2];
            IpSetFrm[17] = gatebytes[3];

            for (i = 6; i < 18; i++)
            {
                sum += IpSetFrm[i];
            }
            i = sum / 0xff;
            IpSetFrm[18] = (byte)i;
            IpSetFrm[19] = (byte)sum;
        }

        static bool ChecIpSetACK(byte[] IpSetFrm, int lenth)
        {
               int i = 0;
               if (0xaa != IpSetFrm[0])
                   return false;
               for (i = 0; i < 12; i++)
               {
                   IpSetFrm[i] = IpSetFrm[6+i];
               }
               return true;
         }

        private bool OpenCom()
        {
            //配置IP地址前要停止采集
            if (Collect == true)
            {
                MessageBox.Show("先停止采集");
                return false;
            }

            //是否配置了串口
            if (serialPort == null)
                return false;

            //是否串口已经打开了
            if (serialPort.IsOpen)
            {
                return true;
            }

            //根据所选串口号打开串口
            if (comboBox1.SelectedItem != null)
            {
                serialPort.PortName = (string)comboBox1.SelectedItem;
                serialPort.BaudRate = 115200;
                try
                {
                    serialPort.Open();
                    serialPort.ReadTimeout = 500;
                    return true;
                }
                catch
                {
                    MessageBox.Show("串口打开失败！");
                    return false;
                }
            }
            else
            {
                MessageBox.Show("请选择串口！");
                return false;
            }
            
        }

        private void CloseCom()
        {
            //是否配置了串口
            if (serialPort == null)
                return ;

            //是否串口已经打开了
            if (serialPort.IsOpen)
            {
                serialPort.Close() ;
            }
            return;
        }

        private void ComSetBtn_Click(object sender, EventArgs e)
        {
            byte[] IpSetFrm = new byte[30];
            byte[] ipbytes = new byte[20];
            byte[] maskbytes = new byte[20];
            string strIpAddress;
            string strMask;
            string strGate;

            //尝试打开串口
            if (false == OpenCom())
            {
                return;
            }

            strIpAddress = SetIPTBox.Text;
            if (isIp(strIpAddress.Trim()))
            {
                ;
            }
            else
            {
                MessageBox.Show("设置Ip地址错误");
                CloseCom();
                return;
            }
            strMask = SetMaskText.Text;

            //此处增加接收线程监测，如果正在接收数据则退出数据接收线程
            GenIpSetDat(SetIPTBox.Text, strMask, SetGateText.Text, IpSetFrm, 0x01);
            try
            {
                serialPort.Write(IpSetFrm, 0, 20);
            }
            catch
            {
                MessageBox.Show("配置写入失败");
                CloseCom();
                return;
            }
            Thread.Sleep(500);
            try
            {
                serialPort.Write(IpSetFrm, 0, 20);
            }
            catch
            {
                MessageBox.Show("配置写入失败");
                CloseCom();
                return;
            }

            ipbytes[3] = IpSetFrm[9];
            ipbytes[2] = IpSetFrm[8];
            ipbytes[1] = IpSetFrm[7];
            ipbytes[0] = IpSetFrm[6];

            //检查接收数据是否正确
            DeviceIPTBox.Text = ipbytes[0].ToString("D") + "." + ipbytes[1].ToString("D") + "." + ipbytes[2].ToString("D") + "." + ipbytes[3].ToString("D");
            MessageBox.Show(ipbytes[0].ToString("D") + "." + ipbytes[1].ToString("D") + "." + ipbytes[2].ToString("D") + "." + ipbytes[3].ToString("D"));
            Masktext.Text = IpSetFrm[10].ToString("D") + "." + IpSetFrm[11].ToString("D") + "." + IpSetFrm[12].ToString("D") + "." + IpSetFrm[13].ToString("D");
            GateText.Text = IpSetFrm[14].ToString("D") + "." + IpSetFrm[15].ToString("D") + "." + IpSetFrm[16].ToString("D") + "." + IpSetFrm[17].ToString("D");

            CloseCom();

            StreamWriter sr = new StreamWriter("config.ini");
            try
            {
                sr.WriteLine(DeviceIPTBox.Text);
                sr.WriteLine(FileSavePathTB.Text);
                sr.WriteLine(FileLengthTbox.Text);
                sr.WriteLine(Masktext.Text);
                sr.WriteLine(GateText.Text);
                sr.WriteLine(chtextBox1.Text);
                sr.WriteLine(chtextBox2.Text);
                sr.WriteLine(chtextBox3.Text);
                sr.WriteLine(chtextBox4.Text);
                sr.WriteLine(chtextBox5.Text);
                sr.WriteLine(chtextBox6.Text);
                sr.WriteLine(chtextBox7.Text);
                sr.WriteLine(chtextBox8.Text);
                sr.WriteLine(chtextBox9.Text);
                sr.WriteLine(chtextBox10.Text);
                sr.WriteLine(chtextBox11.Text);
                sr.WriteLine(chtextBox12.Text);
                sr.WriteLine(chtextBox13.Text);
                sr.WriteLine(chtextBox14.Text);
                sr.WriteLine(chtextBox15.Text);
                sr.WriteLine(chtextBox16.Text);
                sr.Close();
            }
            catch
            {
                MessageBox.Show("配置文件写入错误"); ;
            }
        }

        private void ComReadBtn_Click(object sender, EventArgs e)
        {
            byte[] IpSetFrm = new byte[20];
            byte[] ipbytes = new byte[20];

            //尝试打开串口
            if (false == OpenCom())
            {
                return;
            }

            //此处增加接收线程监测，如果正在接收数据则退出数据接收线程
            GenIpSetDat("0.0.0.0", "0.0.0.0","0.0.0.0",IpSetFrm, 0x00);
            serialPort.Write(IpSetFrm, 0, 20);
            System.Threading.Thread.Sleep(1000);
            try
            {
                serialPort.Read(ipbytes, 0, 20);
                if (!ChecIpSetACK(ipbytes, 21))
                {
                    MessageBox.Show("读取错误，请重新读取");
                    CloseCom();
                    return;
                }
            }
            catch
            {
                MessageBox.Show("读取超时，检查串口连线或设备");
                CloseCom();
                return;
            }

            //检查接收数据是否正确
            DeviceIPTBox.Text = ipbytes[0].ToString("D") + "." + ipbytes[1].ToString("D") + "." + ipbytes[2].ToString("D") + "." + ipbytes[3].ToString("D");
            Masktext.Text = ipbytes[4].ToString("D") + "." + ipbytes[5].ToString("D") + "." + ipbytes[6].ToString("D") + "." + ipbytes[7].ToString("D");
            GateText.Text = ipbytes[8].ToString("D") + "." + ipbytes[9].ToString("D") + "." + ipbytes[10].ToString("D") + "." + ipbytes[11].ToString("D");
            MessageBox.Show(DeviceIPTBox.Text);
            CloseCom();
            StreamWriter sr = new StreamWriter("config.ini");
            try
            {
                sr.WriteLine(DeviceIPTBox.Text);
                sr.WriteLine(FileSavePathTB.Text);
                sr.WriteLine(FileLengthTbox.Text);
                sr.WriteLine(Masktext.Text);
                sr.WriteLine(GateText.Text);
                sr.WriteLine(chtextBox1.Text);
                sr.WriteLine(chtextBox2.Text);
                sr.WriteLine(chtextBox3.Text);
                sr.WriteLine(chtextBox4.Text);
                sr.WriteLine(chtextBox5.Text);
                sr.WriteLine(chtextBox6.Text);
                sr.WriteLine(chtextBox7.Text);
                sr.WriteLine(chtextBox8.Text);
                sr.WriteLine(chtextBox9.Text);
                sr.WriteLine(chtextBox10.Text);
                sr.WriteLine(chtextBox11.Text);
                sr.WriteLine(chtextBox12.Text);
                sr.WriteLine(chtextBox13.Text);
                sr.WriteLine(chtextBox14.Text);
                sr.WriteLine(chtextBox15.Text);
                sr.WriteLine(chtextBox16.Text);

                sr.Close();
            }
            catch
            {
                MessageBox.Show("配置文件写入错误"); ;
            }
        }

        void SetSampleMode(int i)
        {
            NetSetSampleFlag = true;
            GenSampleModeDat(IpSetFrm, 0x01, i);
            TcpClient tcpClient = null;
            try
            {
                tcpClient = new TcpClient(ipstring, 8000);
            }
            catch
            {
                MessageBox.Show("不能建立TCP连接");
                NetSetSampleFlag = false;
                return;
            }
            NetworkStream ns = tcpClient.GetStream();
            try
            {
                ns.Write(IpSetFrm, 0, IpSetFrm.Length);
            }
            catch
            {
                NetSetSampleFlag = false;
                return;
            }
            ns.Close();
            tcpClient.Close();
            NetSetSampleFlag = false;
        }

        static void SetIP()
        {
            NetSetIPFlag = true;
            GenIpSetDat(ipstring2, setmaskstring, setgatestring, IpSetFrm, 0x01);
            TcpClient tcpClient;
            try
            {
                tcpClient = new TcpClient(ipstring, 8000);
            }
            catch
            {
                MessageBox.Show("不能建立TCP连接");
                NetSetIPFlag = false;
                NetStateFlag = false;
                return;
            }
            NetworkStream ns = tcpClient.GetStream();
            try
            {
                ns.Write(IpSetFrm, 0, IpSetFrm.Length);
            }
            catch
            {
                NetSetIPFlag = false;
                NetStateFlag = false;
                return;
            }
            
            ipstring = ipstring2;
            ns.Close();
            tcpClient.Close();
            NetStateFlag = true;
            NetSetIPFlag = false;
        }

        private void refreshset()
        {
            do
            {
                if (NetStateFlag == false)
                {
                    SetNetStateText("连接中");
                }
                else
                {
                    SetNetStateText("配置中");
                }
            } while(NetSetIPFlag == true);

            if (NetStateFlag == true)
            {
                SetDeviceIPText(ipstring2);
                SetMasktext(setmaskstring);
                setGateText(setgatestring);
                SetNetStateText("配置成功");
                MessageBox.Show(ipstring2);

                StreamWriter sr = new StreamWriter("config.ini");
                try
                {
                    sr.WriteLine(DeviceIPTBox.Text);
                    sr.WriteLine(FileSavePathTB.Text);
                    sr.WriteLine(FileLengthTbox.Text);
                    sr.WriteLine(Masktext.Text);
                    sr.WriteLine(GateText.Text);
                    sr.WriteLine(chtextBox1.Text);
                    sr.WriteLine(chtextBox2.Text);
                    sr.WriteLine(chtextBox3.Text);
                    sr.WriteLine(chtextBox4.Text);
                    sr.WriteLine(chtextBox5.Text);
                    sr.WriteLine(chtextBox6.Text);
                    sr.WriteLine(chtextBox7.Text);
                    sr.WriteLine(chtextBox8.Text);
                    sr.WriteLine(chtextBox9.Text);
                    sr.WriteLine(chtextBox10.Text);
                    sr.WriteLine(chtextBox11.Text);
                    sr.WriteLine(chtextBox12.Text);
                    sr.WriteLine(chtextBox13.Text);
                    sr.WriteLine(chtextBox14.Text);
                    sr.WriteLine(chtextBox15.Text);
                    sr.WriteLine(chtextBox16.Text);
                    sr.Close();
                }
                catch
                {
                    MessageBox.Show("配置文件写入错误");
                }
            }
            else
            {
                SetNetStateText("配置失败");
            }
        }
        
        private void NetSetBtn_Click(object sender, EventArgs e)
        {
            byte[] IpSetFrm = new byte[20];
            byte[] ipbytes = new byte[20];
            string strIpAddress;

            if (Collect == true)
            {
                MessageBox.Show("请先停止采集");
                return;
            }

            strIpAddress = DeviceIPTBox.Text;

            if (!isIp(strIpAddress.Trim()))
            {
                MessageBox.Show("本机Ip地址错误");
                return;
            }

            strIpAddress = SetIPTBox.Text;

            if (!isIp(strIpAddress.Trim()))
            {
                MessageBox.Show("设置Ip地址错误");
                return;
            }

            ipstring = DeviceIPTBox.Text;
            ipstring2 = SetIPTBox.Text;
            setmaskstring = SetMaskText.Text;
            setgatestring = SetGateText.Text;

            Thread thread = new Thread(new ThreadStart(SetIP));
            thread.Start();

            this.demoThread = new Thread(new ThreadStart(this.refreshset));
            this.demoThread.Start();
        }

        public static bool isIp(string ip)
        { 
            if(string.IsNullOrEmpty(ip))
            {
                return false;
            }
 
            //清除要验证字符传中的空格
            ip=ip.Trim();
 
            //模式字符串，正则表达式
            string patten = @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$";
 
            //验证
            return Regex.IsMatch(ip,patten );
        }

        private void FileSetBtn_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            string file = folderBrowserDialog1.SelectedPath;
            FileSavePathTB.Text = file;
        }

        delegate void SetTextCallback(string text);

        delegate void SetTextDaCallback(string text, int i);

        delegate void SetTextDaDaCallback(Bitmap text, Color color, int i);

        System.Windows.Forms.Label[] ChMod;

        private void SetChCntText(string text, int i)
        {
            if (this.ChCnt[i].InvokeRequired)
            {
                var d = new SetTextDaCallback(SetChCntText);
                this.Invoke(d, new object[] { text, i });
            }
            else
            {
                this.ChCnt[i].Text = text;
            }
        }

        private void SetChModText(string text, int i)
        {
            if (this.ChMod[i].InvokeRequired)
            {
                var d = new SetTextDaCallback(SetChModText);
                this.Invoke(d, new object[] { text, i });
            }
            else
            {
                this.ChMod[i].Text = text;
            }
        }

        private void SetLostText(string text)
        {
            if (this.LostCNT.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetLostText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.LostCNT.Text = text;
            }
        }

        private void SetTotalText(string text)
        {
            if (this.totalCNT.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetTotalText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.totalCNT.Text = text;
            }
        }

        private void SetRepText(string text)
        {
            if (this.RepCNT.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetRepText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.RepCNT.Text = text;
            }
        }

        private void SetNetStateText(string text)
        {
            if (this.NetState.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetNetStateText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.NetState.Text = text;
            }
        }

        private void SetDeviceIPText(string text)
        {
            if (this.DeviceIPTBox.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetDeviceIPText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.DeviceIPTBox.Text = text;
            }
        }

        private void SetMasktext(string text)
        {
            if (this.Masktext.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetMasktext);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.Masktext.Text = text;
            }
        }

        private void setGateText(string text)
        {
            if (this.GateText.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(setGateText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.GateText.Text = text;
            }
        }

        private void SetNetDevice1Text(string text)
        {
            if (this.NetDevice1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetNetDevice1Text);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (text == " Color.Lime")
                    this.NetDevice1.BackColor = Color.Lime;
                else
                    this.NetDevice1.BackColor = Color.Transparent;
            }
        }

        private void SetMACText(string text)
        {
            if (this.MACTB.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetMACText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.MACTB.Text = text;
            }
        }

        private void SetDatLightCHText(Bitmap text, Color color, int i)
        {
            if (this.DatLightCH[i].InvokeRequired)
            {
                var d = new SetTextDaDaCallback(SetDatLightCHText);
                this.Invoke(d, new object[] { text, color, i });
            }
            else
            {
                this.DatLightCH[i].BackColor = color;
                this.DatLightCH[i].BackgroundImage = text;
            }
        }

        System.Windows.Forms.Label[] ChCnt;
        int SampleMode;

        private void refreshUDPdat()
        {
　　        string temIPsring; 
　　        byte[] receiveData;
            int li_Index; //变量声明
            byte[] macAddrData = new byte[6], gatewayData = new byte[4], maskData = new byte[4];
            
　　        IPEndPoint remotePoint = new IPEndPoint(IPAddress.Any, 0);
            UdpClient client = new UdpClient(8010);
　　        while (true) 
　　        {
                receiveData = client.Receive(ref remotePoint);//接收数据 
                temIPsring = remotePoint.ToString();
                li_Index = temIPsring.LastIndexOf(':');
                temIPsring = temIPsring.Substring(0, li_Index);//获得目标字符串
                --NetDevIP1life;
                
                if (receiveData[0] == 0xAA && receiveData[1] == 0xBB && receiveData[2] == 0xCC)
                {
                    SetNetDevice1Text(" Color.Lime");
                    NetDevIP1life = 5;
                    Array.Copy(receiveData, 20, macAddrData, 0, 6);
                    Array.Copy(receiveData, 26, maskData, 0, 4);
                    Array.Copy(receiveData, 30, gatewayData, 0, 4);
                    SampleMode = (receiveData[34] << 8) + receiveData[35];

                    if (NetDevIP1life == 0)
                    {
                        SetNetDevice1Text(" Color.Transparent");
                    }

                    for (int i = 0; i < 8; i++)
                    {
                        SetDatLightCHText((0 == (SampleMode & (1 << i))) ? Properties.Resources.rising_edge : Properties.Resources.falling_edge, (receiveData[4 + i] != 0) ? Color.Lime : Color.Transparent, i);
                    }

                    SetDeviceIPText(temIPsring);
                    SetMACText(receiveData[20].ToString("X2") + ":" + receiveData[21].ToString("X2") + ":" + receiveData[22].ToString("X2") + ":" + receiveData[23].ToString("X2") + ":" + receiveData[24].ToString("X2") + ":" + receiveData[25].ToString("X2"));
                    SetMasktext(maskData[0].ToString("D") + "." + maskData[1].ToString("D") + "." + maskData[2].ToString("D") + "." + maskData[3].ToString("D"));
                    setGateText(gatewayData[0].ToString("D") + "." + gatewayData[1].ToString("D") + "." + gatewayData[2].ToString("D") + "." + gatewayData[3].ToString("D"));
                }
　　        }
            client.Close();//关闭连接
        }

        private void refresh()
        {
            do
            {
                for (int i = 0; i < 8; i++)
                {
                    if (ChanSel[i])
                    {
                        this.SetChModText(chamod[i] == 0? "TTL": "HDB3", i);
                        this.SetChCntText((chacnt[i] / 1000).ToString("d"), i);
                    }
                }
                //更新网络状态
                if (NetStateFlag == false)
                {
                    this.SetNetStateText("正在连接");
                }
                else
                {
                    this.SetNetStateText("设备连接");
                }
                Thread.Sleep(1000);

            } while (Collect);
        }
        private Thread demoThread = null;

        private void CreatDirAndFile()
        {
            if (!Directory.Exists(FilePaths))   //创建文件夹
            {
                Directory.CreateDirectory(FilePaths);
            }

            string NowTime = DateTime.Now.ToString("_yyyy_MM_dd_hhmmss");

            for (int i = 0; i < 16; i++)       //创建需要创建的文件
            {
                if (ChanSel[i] && fs1[i] == null)
                {
                    fs1[i] = new FileStream(FilePaths + "ch" + (i + 1) + "_" + Beizhu[i] + NowTime + ".dat", FileMode.Create, FileAccess.Write);
                }
            }
        }

        System.Windows.Forms.TextBox[] chtextBox;

        private void StartCollectBtn_Click(object sender, EventArgs e)
        {
            string strIpAddress;
            FilePaths = FileSavePathTB.Text + "\\";
           
            try
            {
                FileLenth = Convert.ToInt64(FileLengthTbox.Text);
                if (FileLenth > 3000)
                {
                    FileLenth = 3000;
                    FileLengthTbox.Text = "3000";
                }
            }
            catch
            {
                MessageBox.Show("设定文件长度");
                return;
            }
            FileLenth *= 1000000;
            ipstring = DeviceIPTBox.Text;
            strIpAddress = DeviceIPTBox.Text;

            /*获取哪些通道需要采集和存储  */
            if (Collect == false)
            {
                if (!isIp(strIpAddress.Trim()))
                {
                    MessageBox.Show("本机Ip地址错误");
                    return;
                }

                for(int i=0;i<8;i++)
                {
                    ChanSel[i] = ChSel[i].Checked;
                    ChSel[i].Enabled = false;
                    Beizhu[i] = chtextBox[i].Text;
                }

                CreatDirAndFile();

                Collect = true;

                StreamWriter sr = new StreamWriter("config.ini");
                try
                {
                    sr.WriteLine(DeviceIPTBox.Text);
                    sr.WriteLine(FileSavePathTB.Text);
                    sr.WriteLine(FileLengthTbox.Text);
                    sr.WriteLine(Masktext.Text);
                    sr.WriteLine(GateText.Text);
                    sr.WriteLine(chtextBox1.Text);
                    sr.WriteLine(chtextBox2.Text);
                    sr.WriteLine(chtextBox3.Text);
                    sr.WriteLine(chtextBox4.Text);
                    sr.WriteLine(chtextBox5.Text);
                    sr.WriteLine(chtextBox6.Text);
                    sr.WriteLine(chtextBox7.Text);
                    sr.WriteLine(chtextBox8.Text);
                    sr.WriteLine(chtextBox9.Text);
                    sr.WriteLine(chtextBox10.Text);
                    sr.WriteLine(chtextBox11.Text);
                    sr.WriteLine(chtextBox12.Text);
                    sr.WriteLine(chtextBox13.Text);
                    sr.WriteLine(chtextBox14.Text);
                    sr.WriteLine(chtextBox15.Text);
                    sr.WriteLine(chtextBox16.Text);
                    sr.Close();
                }
                catch
                {
                    MessageBox.Show("配置文件写入错误");
                }

                for (int i = 0; i < 16; i++)
                {
                    chamod[i] = 0;
                    chacnt[i] = 0;
                }
                RePocketSn = 0;
                OldPocketSn = 0;
                LostPocketSn = 0;
                
                /*启动文件线程*/
                this.demoThread = new Thread(new ThreadStart(this.refresh));
                this.demoThread.Start();

                /*启动接收线程*/
                Thread thread2 = new Thread(new ThreadStart(TcpClientRecDat));
                thread2.Start();

                StartCollectBtn.Text = "停止采集";
                Ballsel.Enabled = false;
            }
            else if (Collect == true)
            {
                Collect = false;
                for (int i = 0; i < 8; i++)
                {
                    ChanSel[i] = ChSel[i].Checked;
                    ChSel[i].Enabled = true;
                }
                StartCollectBtn.Text = "开始采集";
                Ballsel.Enabled = true;
            }
        }

        private void StopColleBtn_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 16; i++)
            {
                chamod[i] = 0;
                chacnt[i] = 0;
            }
            RePocketSn = 0;
            OldPocketSn = 0;
            LostPocketSn = 0;
            TotalPacketSn = 0;
            if (Collect == false)
            {
                refresh();
            }
        }

        private void NetDevice1_Click(object sender, EventArgs e)
        {
            StreamWriter sr = new StreamWriter("config.ini");
            try
            {
                sr.WriteLine(DeviceIPTBox.Text);
                sr.WriteLine(FileSavePathTB.Text);
                sr.WriteLine(FileLengthTbox.Text);
                sr.WriteLine(Masktext.Text);
                sr.WriteLine(GateText.Text);
                sr.WriteLine(chtextBox1.Text);
                sr.WriteLine(chtextBox2.Text);
                sr.WriteLine(chtextBox3.Text);
                sr.WriteLine(chtextBox4.Text);
                sr.WriteLine(chtextBox5.Text);
                sr.WriteLine(chtextBox6.Text);
                sr.WriteLine(chtextBox7.Text);
                sr.WriteLine(chtextBox8.Text);
                sr.WriteLine(chtextBox9.Text);
                sr.WriteLine(chtextBox10.Text);
                sr.WriteLine(chtextBox11.Text);
                sr.WriteLine(chtextBox12.Text);
                sr.WriteLine(chtextBox13.Text);
                sr.WriteLine(chtextBox14.Text);
                sr.WriteLine(chtextBox15.Text);
                sr.WriteLine(chtextBox16.Text);
                sr.Close();
            }
            catch
            {
                MessageBox.Show("配置文件写入错误"); ;
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (ChSel1.Checked == false)
            {
                ChSel1.Checked = true;
                ChSel2.Checked = true;
                ChSel3.Checked = true;
                ChSel4.Checked = true;
                ChSel5.Checked = true;
                ChSel6.Checked = true;
                ChSel7.Checked = true;
                ChSel8.Checked = true;
                Ballsel.Text = "全不选";
            }
            else
            {
                ChSel1.Checked = false;
                ChSel2.Checked = false;
                ChSel3.Checked = false;
                ChSel4.Checked = false;
                ChSel5.Checked = false;
                ChSel6.Checked = false;
                ChSel7.Checked = false;
                ChSel8.Checked = false;
                Ballsel.Text = "全选";
            }
        }

        System.Windows.Forms.CheckBox[] ChSel;

        private void ChSel_CheckedChanged(object sender, EventArgs e)
        {
            System.Windows.Forms.CheckBox chBox = ((System.Windows.Forms.CheckBox)sender);
            int i;
            for (i = 0; i < 8; i++)
                if (ChSel[i] == chBox)
                    break;
            if (Collect == false)
                return;
            string NowTime = DateTime.Now.ToString("_yyyy_MM_dd_hhmmss");
            if (ChSel[i].Checked == true)
            {
                ChanSel[i] = ChSel[i].Checked;
                fs1[i] = new FileStream(FilePaths + "ch" + (i+1) + "_" + Beizhu[i] + NowTime + ".dat", FileMode.Create, FileAccess.Write);
            }
            else
            {
                ChanSel[i] = ChSel[i].Checked;
                fs1[i].Flush();
                fs1[i].Close();
                fs1[i] = null;
            }
        }

        System.Windows.Forms.Button[] DatLightCH;

        private void DatLightCH_Click(object sender, EventArgs e)
        {
            byte[] IpSetFrm = new byte[20];

            System.Windows.Forms.Button btn = ((System.Windows.Forms.Button)sender);
            int i;
            for (i = 0; i < 8; i++)
                if (DatLightCH[i] == btn)
                    break;

            if (Collect == true)
            {
                MessageBox.Show("请先停止采集");
                return;
            }

            ipstring = DeviceIPTBox.Text;

            Thread thread = new Thread(new ThreadStart(()=> {
                SetSampleMode(i);
            }));
            thread.Start();

            //btn.Text = (0 == (SampleMode & (1 << i))) ? "RisingEdge" : "FallingEdge";
            //this.demoThread = new Thread(new ThreadStart(this.refreshset));
            //this.demoThread.Start();
        }
    }
}
