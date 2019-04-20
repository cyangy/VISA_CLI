using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NationalInstruments.VisaNS;
using Mono.Options;

#if NET40
using DotNet4_ArraySegment_ToArray_Implement;
#endif


namespace VISA_CLI
{
    
    class myVISA_CLI
    {
        public enum Mode : short { GPIB = 1, SERIAL,USBRAW,USBTMC } //https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/enum
        public static void ParseArgs(string[] args) //https://mail.gnome.org/archives/commits-list/2012-December/msg00139.html
        {
            bool showHelp = false;
            var p = new OptionSet() {
                //

                //GPIB related
                { "G|useGPIB", "GPIB mode", v => GlobalVars.VISA_CLI_Option_CurrentMode = (short)Mode.GPIB },
                { "gpib|BoardIndex=", "GPIB board index(Default 0)", v =>  short.TryParse(v,out GlobalVars.VISA_CLI_Option_GPIB_BoardIndex) },  //https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/types/how-to-convert-a-string-to-a-number
                { "pad|PrimaryAddress=", "primary address", v =>  short.TryParse(v,out GlobalVars.VISA_CLI_Option_GPIB_PrimaryAddress) },
                { "sad|SecondaryAddress=", "secondary address", v =>  short.TryParse(v,out GlobalVars.VISA_CLI_Option_GPIB_SecondaryAddress ) },
           
                //COM related
                { "S|useSERIAL", "SERIAL mode", v => GlobalVars.VISA_CLI_Option_CurrentMode = (short)Mode.SERIAL },
                { "port|SerialPortNumber=", "Serial Port Number", v =>  short.TryParse(v,out GlobalVars.VISA_CLI_Option_Serial_PortNumber) },
                { "baud|BaudRate=", "Baud of Serial Port(Default 19200)", v =>  int.TryParse(v,out GlobalVars.VISA_CLI_Option_SerialBaudRate) },
                { "data|DataBits=", "Data bits (Default 8)", v =>  short.TryParse(v,out GlobalVars.VISA_CLI_Option_SerialDataBits) },
                { "stop|StopBits=", "Stop bits (Default 10)", v =>  StopBitType.TryParse(v,out GlobalVars.VISA_CLI_Option_SerialStopBits) },
                { "parity|SerialParity=", "Serial Parity: NONE 0  Odd 1  Even 2 Mark 3 Space 4 (Default NONE)", v =>  Parity.TryParse(v,out GlobalVars.VISA_CLI_Option_SerialParity) },
                { "flow|FlowControlTypes=", "Flow Control Types: NONE 0  XON/XOFF 1 (Default NONE)", v =>  FlowControlTypes.TryParse(v,out GlobalVars.VISA_CLI_Option_SerialFlowControl) },
                { "terminate|TerminationCharacters=", "Termination Characters of Serial (Default \"\\n\")", v =>  GlobalVars.theTerminationCharactersOfRS232 = v },


                //Common
                { "C|cmdstr|CommandString=", "command(s) to send to the device", v =>  GlobalVars.VISA_CLI_Option_CommandString = v },
                { "W|write|JustWriteCommand", "just write (default)", v =>  GlobalVars.VISA_CLI_Option_JustWriteCommand = v != null },
                { "R|read|JustReadBack", "the command is a query command", v =>  GlobalVars.VISA_CLI_Option_JustReadBack = v != null },
                { "Q|query|QueryCommand", "the command is a query command", v =>  GlobalVars.VISA_CLI_Option_isQueryCommand = v != null },
                { "D|debug|PrintDebugMessage", "prints debug messages", v =>  GlobalVars.VISA_CLI_Option_PrintDebugMessage  = v != null },
                { "F|save2file|FileName=", "save the response binary data to specify file", v =>  GlobalVars.VISA_CLI_Option_FileName = v },
                { "O|overwrite|OverwriteFile", "if file exist ,overwrite it", v =>  GlobalVars.VISA_CLI_Option_OverwriteFile = v != null },
                { "N|rBytes|ReadBackNbytes=", "how many bytes should be read back", v =>  int.TryParse(v,out GlobalVars.VISA_CLI_Option_ReadBackNbytes ) },
                { "E|skip|SkipFirstNbytes=", "skip first n bytes of received data", v =>  int.TryParse(v,out GlobalVars.VISA_CLI_Option_SkipFirstNbytes ) },
                { "h|help",  "show this message and exit.", v => showHelp = v != null },
            };

            try
            {
                p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ",System.Diagnostics.Process.GetCurrentProcess().ProcessName);
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", System.AppDomain.CurrentDomain.FriendlyName);
            }

            if (showHelp)
            {
                Console.WriteLine("Usage: {0} -G|-C  [MODE related options] -C \"command string\"", System.AppDomain.CurrentDomain.FriendlyName);// System.Diagnostics.Process.GetCurrentProcess().ProcessName); //https://stackoverflow.com/questions/37459509/how-to-get-the-exe-name-while-the-program-is-running/37459553#37459553
                Console.WriteLine();
                Console.WriteLine("Options:");
                p.WriteOptionDescriptions(Console.Out);
                System.Environment.Exit(-1);            //https://stackoverflow.com/questions/12977924/how-to-properly-exit-a-c-sharp-application
            }
        }
        public static bool GenerateVISAResourceName(short mode)
        {
            switch (mode)
            {
                case ((short)Mode.GPIB):     //GPIB  GPIB0::2::INSTR
                      {
                        GlobalVars.VISAResourceName = String.Empty; // 清空原字符串
                        GlobalVars.VISAResourceName = "GPIB" + GlobalVars.VISA_CLI_Option_GPIB_BoardIndex + "::" + GlobalVars.VISA_CLI_Option_GPIB_PrimaryAddress + "::INSTR";
                        return true;
                    }
                case ((short)Mode.SERIAL):     //SERIAL  ASRL2::INSTR
                    {
                        GlobalVars.VISAResourceName = String.Empty; // 清空原字符串
                        GlobalVars.VISAResourceName = "ASRL" + GlobalVars.VISA_CLI_Option_Serial_PortNumber+ "::INSTR";
                        return true;
                    }
                default : return false;
            }
        }
        public static void SetSerialAttribute(ref MessageBasedSession mbs)
        {
            SerialSession ser = (SerialSession)mbs;
            ser.BaudRate = GlobalVars.VISA_CLI_Option_SerialBaudRate; //设置速率
            ser.DataBits = GlobalVars.VISA_CLI_Option_SerialDataBits;      //数据位
            ser.StopBits = GlobalVars.VISA_CLI_Option_SerialStopBits; //停止位 
            ser.Parity = GlobalVars.VISA_CLI_Option_SerialParity; //校验     NONE 0  Odd 1  Even 2 Mark 3 Space 4
            ser.FlowControl = GlobalVars.VISA_CLI_Option_SerialFlowControl; //Flow Control  NONE 0  XON/XOFF 1   使用 NI I/O Trace 监视   VISA Test Panel 设置时得到
            if (GlobalVars.VISA_CLI_Option_PrintDebugMessage)
            {
                Console.WriteLine("当前正使用COM通信,当前设置为:\n速率" + GlobalVars.VISA_CLI_Option_SerialBaudRate.ToString() + "\n数据位:" + GlobalVars.VISA_CLI_Option_SerialDataBits.ToString() + "\n停止位:" + (((int)GlobalVars.VISA_CLI_Option_SerialStopBits) / 10).ToString() + "\n校验方式(Parity):" + GlobalVars.VISA_CLI_Option_SerialParityEnumList[((int)GlobalVars.VISA_CLI_Option_SerialParity)] + "\nFlowControl:" + GlobalVars.VISA_CLI_Option_SerialFlowControlEnumList[((int)GlobalVars.VISA_CLI_Option_SerialFlowControl)] + "\n\n请确保仪器设置与本设置相符",
                  "当前通讯设置");
            }
        }
        public static void  Write()
        {
            GlobalVars.mbSession.Write(GlobalVars.VISA_CLI_Option_CommandString);
        }
        public static void Read()
        {
            GlobalVars.VISA_CLI_ReadBackBuffer = null;
            // GlobalVars.VISA_CLI_ReadBackBuffer = GlobalVars.mbSession.ReadString(GlobalVars.VISA_CLI_Option_ReadBackNbytes);
            GlobalVars.VISA_CLI_ReadBackBuffer = GlobalVars.mbSession.ReadByteArray(GlobalVars.VISA_CLI_Option_ReadBackNbytes);
            //https://stackoverflow.com/questions/2530951/remove-first-16-bytes/2530994#2530994
            //https://stackoverflow.com/questions/5062233/is-there-a-away-to-convert-ilistarraysegmentbyte-to-byte-without-enumerati
            if (GlobalVars.VISA_CLI_Option_SkipFirstNbytes > 0)
            {
                ArraySegment<byte> segment = new ArraySegment<byte>(GlobalVars.VISA_CLI_ReadBackBuffer, GlobalVars.VISA_CLI_Option_SkipFirstNbytes, GlobalVars.VISA_CLI_ReadBackBuffer.Length - GlobalVars.VISA_CLI_Option_SkipFirstNbytes);
#if NET40
                if (GlobalVars.VISA_CLI_Option_PrintDebugMessage)
                 {
                   Console.WriteLine(" .NET Framwork Version 4.0 : segment.AsList().ToArray()) ");
                 }
                GlobalVars.VISA_CLI_ReadBackBuffer = segment.AsList().ToArray();
#else
                if (GlobalVars.VISA_CLI_Option_PrintDebugMessage)
                {
                    Console.WriteLine(" .NET Framwork Version > 4.0 : segment.ToArray()) "); // For   Newer than  .NET4.0 Framework Use  segment.ToArray()  https://docs.microsoft.com/en-us/dotnet/api/system.arraysegment-1.array?view=netframework-4.0
                 }
                GlobalVars.VISA_CLI_ReadBackBuffer = segment.ToArray();
#endif
            }
        }
        public static void Query()
        {
            Write();
            Read();
            //GlobalVars.VISA_CLI_ReadBackBuffer =GlobalVars.mbSession.Query(GlobalVars.VISA_CLI_Option_CommandString,GlobalVars.VISA_CLI_Option_ReadBackNbytes);
            if (GlobalVars.VISA_CLI_Option_PrintDebugMessage)
            {
                Console.WriteLine("{0} byte request And GlobalVars.VISA_CLI_ReadBackBuffer.Length={1} byte actually transfered", GlobalVars.VISA_CLI_Option_ReadBackNbytes, GlobalVars.VISA_CLI_ReadBackBuffer.Length);
            }
        }
        public static void GenerateNewFileName()
        {
            if (GlobalVars.VISA_CLI_Option_PrintDebugMessage)
            {
                Console.WriteLine("File {0} Exist", GlobalVars.VISA_CLI_Option_FileName);
            }
            //https://docs.microsoft.com/en-us/dotnet/api/system.io.path.getextension?view=netframework-4.7.2
            //https://docs.microsoft.com/en-us/dotnet/api/system.io.path.getfilenamewithoutextension?view=netframework-4.7.2
            GlobalVars.VISA_CLI_Option_FileName = Path.GetFileNameWithoutExtension(GlobalVars.VISA_CLI_Option_FileName) + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + Path.GetExtension(GlobalVars.VISA_CLI_Option_FileName);
            if (GlobalVars.VISA_CLI_Option_PrintDebugMessage)
            {
                Console.WriteLine("new file name is :{0}", GlobalVars.VISA_CLI_Option_FileName);
            }
        }
        static Int32 Main(string[] args)
        {
            //Console.WriteLine("Hello World!");
            //myHelp.Show_gpib_help();
            //Console.ReadLine();
            //尝试解析各参数
            myVISA_CLI.ParseArgs(args);

            //根据各参数开始执行命令
            //首先根据用户需求生成相应的资源名称(GPIB0::2::INSTR、ASRL2::INSTR)
            if(!GenerateVISAResourceName(GlobalVars.VISA_CLI_Option_CurrentMode)) //https://docs.microsoft.com/en-us/dotnet/api/system.console.error?redirectedfrom=MSDN&view=netframework-4.7.2#System_Console_Error
            {
                var standardError = new StreamWriter(Console.OpenStandardError());
                standardError.AutoFlush = true;
                Console.SetError(standardError);
                Console.Error.WriteLine("mode must be specified!");
                Console.Error.WriteLine("       {0} -h for more information", System.AppDomain.CurrentDomain.FriendlyName);
                return -1;
            }
            if (GlobalVars.VISA_CLI_Option_GPIB_PrimaryAddress < 0 && GlobalVars.VISA_CLI_Option_Serial_PortNumber < 0) //https://docs.microsoft.com/en-us/dotnet/api/system.console.error?redirectedfrom=MSDN&view=netframework-4.7.2#System_Console_Error
            {
                var standardError = new StreamWriter(Console.OpenStandardError());
                standardError.AutoFlush = true;
                Console.SetError(standardError);
                Console.Error.WriteLine("GPIB Primary Address or Serial Port Number  must be specified!");
                return -1;
            }
            //尝试进行操作
            try
            {
                // GlobalVars.mbSession = (MessageBasedSession)ResourceManager.GetLocalManager().Open(GlobalVars.VISAResourceName);
                GlobalVars.mbSession = (MessageBasedSession)ResourceManager.GetLocalManager().Open(GlobalVars.VISAResourceName);
                GlobalVars.currentInterfaceType = GlobalVars.mbSession.HardwareInterfaceType.ToString().ToUpper();//GPIB SERIAL
                if (GlobalVars.VISA_CLI_Option_PrintDebugMessage)
                {
                    Console.WriteLine("当前使用硬件接口类型:{0}", GlobalVars.currentInterfaceType);
                    Console.WriteLine("当前使用硬件接口名:{0}", GlobalVars.mbSession.HardwareInterfaceName);
                    Console.WriteLine("will open {0}", GlobalVars.VISAResourceName);
                }
                //Serial
                // 如何设置 Serial https://forums.ni.com/t5/Instrument-Control-GPIB-Serial/How-do-you-set-the-number-of-start-bits-for-a-VISA-serial/td-p/325520
                if (GlobalVars.currentInterfaceType == "SERIAL")//SERIAL 接口 
                {
                    SetSerialAttribute(ref GlobalVars.mbSession);  //  设置SERIAL
                    GlobalVars.VISA_CLI_Option_CommandString += GlobalVars.theTerminationCharactersOfRS232; // 更新要发送的命令，末尾加换行符
                }
                GlobalVars.mbSession.Timeout = GlobalVars.VISASessionTimeout; //设置超时
                //priority  ?
                if (GlobalVars.VISA_CLI_Option_isQueryCommand && !String.IsNullOrEmpty(GlobalVars.VISA_CLI_Option_CommandString))  // query
                {
                    Query();
                    GlobalVars.VISA_CLI_Option_JustWriteCommand = false;
                    GlobalVars.VISA_CLI_Option_JustReadBack = false;
                }
                else if(GlobalVars.VISA_CLI_Option_JustReadBack) //Just Read Back
                {
                    Read();
                    GlobalVars.VISA_CLI_Option_JustWriteCommand = false;
                }
                else if(GlobalVars.VISA_CLI_Option_JustWriteCommand && !String.IsNullOrEmpty(GlobalVars.VISA_CLI_Option_CommandString)) //Just write  
                {
                    Write(); 
                }

                // save to file
                if(!String.IsNullOrEmpty(GlobalVars.VISA_CLI_Option_FileName) && !(GlobalVars.VISA_CLI_ReadBackBuffer == null || GlobalVars.VISA_CLI_ReadBackBuffer.Length == 0)  ) //save respond to file
                {
                    //https://docs.microsoft.com/en-us/dotnet/api/system.io.file.exists?view=netframework-4.7.2
                    if (File.Exists(GlobalVars.VISA_CLI_Option_FileName) && !(GlobalVars.VISA_CLI_Option_OverwriteFile)) // file exist but not overwrite
                    {
                        GenerateNewFileName(); //new file name
                    }
                    // file exist but overwrite
                    // https://www.cnblogs.com/ybwang/archive/2010/06/12/1757409.html
                    // https://stackoverflow.com/questions/17967509/binarywriter-to-overwrite-an-existing-file-c-sharp/17967581#17967581
                    FileStream fs = File.Open(GlobalVars.VISA_CLI_Option_FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    BinaryWriter bw = new BinaryWriter(fs);
                    //bw.Write(String.Join(String.Empty, GlobalVars.VISA_CLI_ReadBackBuffer.Skip(GlobalVars.VISA_CLI_Option_SkipFirstNbytes)),0,GlobalVars.VISA_CLI_ReadBackBuffer.Length); 
                    // bw.Write(String.Join(String.Empty, GlobalVars.VISA_CLI_ReadBackBuffer.Skip(GlobalVars.VISA_CLI_Option_SkipFirstNbytes)));


                    bw.Write(GlobalVars.VISA_CLI_ReadBackBuffer);
                    //bw.Write(
                    bw.Close(); //Close
                    fs.Close(); //Close
                    if (GlobalVars.VISA_CLI_Option_PrintDebugMessage)
                    {
                        FileInfo fi = new FileInfo(GlobalVars.VISA_CLI_Option_FileName);
                        Console.WriteLine("write to  file  :{0}  completely, {1} bytes total", GlobalVars.VISA_CLI_Option_FileName,fi.Length);
                    }

                }
                else if(!(GlobalVars.VISA_CLI_ReadBackBuffer == null || GlobalVars.VISA_CLI_ReadBackBuffer.Length == 0))
                {
                    //https://www.cnblogs.com/michaelxu/archive/2007/05/14/745881.html
                    //Console.WriteLine(System.Text.Encoding.ASCII.GetString(GlobalVars.VISA_CLI_ReadBackBuffer));//
                    Console.Write(System.Text.Encoding.Default.GetString(GlobalVars.VISA_CLI_ReadBackBuffer).TrimEnd('\0'));//

                }




            }
            catch (InvalidCastException) //打开了不支持的设备
            {
                Console.WriteLine("会话必须是基于消息的会话, 请选择正确的设备\n Resource selected must be a message - based session!");
                return -1;
            }
            catch (Exception exp)//其他异常
            {
                Console.WriteLine(exp.Message);
                return -1;
            }
            finally //不论是否有异常以下代码都会被执行
            {
                /*
                Console.WriteLine("Console.WriteLine(GlobalVars.mbSession.Write(\"{0}\"));", GlobalVars.VISA_CLI_Option_CommandString);
                GlobalVars.mbSession.Write(GlobalVars.VISA_CLI_Option_CommandString);
                Console.WriteLine("GlobalVars.mbSession.ReadString({0});",GlobalVars.VISA_CLI_Option_ReadBackNbytes);
                String s = GlobalVars.mbSession.ReadString(GlobalVars.VISA_CLI_Option_ReadBackNbytes);
                Console.WriteLine("{0} byte request And s.Length={1} byte actually transfered",GlobalVars.VISA_CLI_Option_ReadBackNbytes , s.Length);
                Console.WriteLine("retrun string is :{0}",s);
                Console.WriteLine("retrun string skip first {0} bytes  is :{1}",GlobalVars.VISA_CLI_Option_SkipFirstNbytes, String.Join(String.Empty, s.Skip(GlobalVars.VISA_CLI_Option_SkipFirstNbytes))); //https://stackoverflow.com/questions/7186648/how-to-remove-first-10-characters-from-a-string/7186753#7186753
                Console.WriteLine("retrun string skip last {0} bytes  is :{1}", GlobalVars.VISA_CLI_Option_SkipFirstNbytes, s.Remove(s.Length-GlobalVars.VISA_CLI_Option_SkipFirstNbytes));//https://stackoverflow.com/questions/15564944/remove-the-last-three-characters-from-a-string/15564958#15564958
                // Console.WriteLine("Console.WriteLine(GlobalVars.mbSession.Query(\"{0}\"));",GlobalVars.VISA_CLI_Option_CommandString);
                // Console.WriteLine("press ENTER to quit");
                // Console.ReadLine();
                // String s = GlobalVars.mbSession.Query(":DISPlay:DATA? JPG", 500000);
                // Console.WriteLine("50000 byte request And s.Length={0} byte actually transfered", s.Length);
                // Console.WriteLine(s);
                // Console.WriteLine("Console.WriteLine(GlobalVars.mbSession.Query(\":DISPlay: DATA ? JPG\", 500000));");
                // Console.WriteLine("press ENTER to continue");
                // Console.ReadLine();
                // GlobalVars.mbSession.Write(":DISPlay: DATA ? JPG");
                // GlobalVars.mbSession.ReadToFile("1.jpg");
                // Console.WriteLine("GlobalVars.mbSession.ReadToFile(\"1.jpg\");");
                // GlobalVars.mbSession.Dispose();
                */
            }
            //Console.ReadLine();
            return 0;
        }
    }
    
    public static class GlobalVars
    {
        //list instruments
        public static bool VISA_CLI_Option_ListInstruments = false;

        public static String currentInterfaceType = null;  //接口类型
        public static short VISA_CLI_Option_CurrentMode= -1;

        // read back buffer
        public static Byte [] VISA_CLI_ReadBackBuffer = null; 


        //使用GPIB Mode.GPIB
        public static short VISA_CLI_Option_GPIB_BoardIndex = 0;            // Board index
        public static short VISA_CLI_Option_GPIB_PrimaryAddress = -1;        // Primary address
        public static short VISA_CLI_Option_GPIB_SecondaryAddress = -1;      // Secondary address

        //使用串口 Mode.SERIAL
        public static short VISA_CLI_Option_Serial_PortNumber= -1; // serial port number
             //对RS232接口,命令后必须加 LF  作为结束  0x0A
        public static String theTerminationCharactersOfRS232 = "\n";
            //RS232设置用
        public static Int32 VISA_CLI_Option_SerialBaudRate = 19200; //Serial速率
        public static short VISA_CLI_Option_SerialDataBits = 8; //数据位
        public static StopBitType VISA_CLI_Option_SerialStopBits = (StopBitType)10; //停止位
        public static List<String> VISA_CLI_Option_SerialParityEnumList = new List<String> { "NONE", "Odd", "Even", "Mark ", "Space" }; //校验方式列表
        public static Parity VISA_CLI_Option_SerialParity = (Parity)0; //校验     NONE 0  Odd 1  Even 2 Mark 3 Space 4
        public static List<String> VISA_CLI_Option_SerialFlowControlEnumList = new List<String>(new String[] { "NONE", "XON/XOFF" }); //FlowControl方式列表
        public static FlowControlTypes VISA_CLI_Option_SerialFlowControl = (FlowControlTypes)0; ////Flow Control  NONE 0  XON/XOFF 1   使用 NI I/O Trace 监视   VISA Test Panel 设置时得到

        //Common
        public static String VISA_CLI_Option_CommandString = null;         //command to send
        public static bool VISA_CLI_Option_JustWriteCommand = true;        //just send command (default)
        public static bool VISA_CLI_Option_JustReadBack = false;           //just read back
        public static bool VISA_CLI_Option_isQueryCommand = false;         // is a query command
        public static bool VISA_CLI_Option_PrintDebugMessage = false; //debug switch
        public static String VISA_CLI_Option_FileName = null;           //when file name specified, save binary response to file
        public static bool VISA_CLI_Option_OverwriteFile = false;     //if file exist ,overwrite it
        public static int VISA_CLI_Option_ReadBackNbytes = 1024;      //read specified length of response,default is 1024 bytes
        public static int VISA_CLI_Option_SkipFirstNbytes = 0;     //for some system(DCA86100,AQ6370,etc.), transfered data via GPIB contain extra bytes,user can skip them

        public static   MessageBasedSession mbSession;
        public static   UsbRaw      USBRAW_Session;      //USB
        public static String VISAResourceName = null;
        public static int    VISASessionTimeout= 10000; //10000ms
    }
}
