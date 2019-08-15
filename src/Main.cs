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
using System.Diagnostics;
using System.Globalization;  //Byte.TryParse()
using Cintio;
using System.Threading;

#if NET40
using DotNet4_ArraySegment_ToArray_Implement;
#endif


namespace VISA_CLI
{
    
    class VISA_CLI
    {
        public enum Mode : short { GPIB = 1, SERIAL,USBRAW,USBTMC,TCPIP} //https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/enum
        public static void ParseArgs(string[] args) //https://mail.gnome.org/archives/commits-list/2012-December/msg00139.html
        {
            bool showHelp = false;
            var p = new OptionSet() {
                //

                //GPIB related
                { "G|useGPIB", "GPIB mode", v => GlobalVars.VISA_CLI_Option_CurrentMode = (short)Mode.GPIB },
                { "gpib|gpibBoardIndex=", "GPIB board index(Default 0)", v =>  short.TryParse(v,out GlobalVars.VISA_CLI_Option_GPIB_BoardIndex) },  //https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/types/how-to-convert-a-string-to-a-number
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
                { "stmR|SerialTerminationMethodWhenRead=",  "Serial Termination Method When Read  : None 0   LastBit 1   TerminationCharacter 2   Break 3 (Default TerminationCharacter)", v =>   SerialTerminationMethod.TryParse(v,out GlobalVars.VISA_CLI_Option_SerialTerminationMethodWhenRead) },
                { "stmW|SerialTerminationMethodWhenWrite=", "Serial Termination Method When Write : None 0   LastBit 1   TerminationCharacter 2   Break 3 (Default NONE)",                 v =>   SerialTerminationMethod.TryParse(v,out GlobalVars.VISA_CLI_Option_SerialTerminationMethodWhenWrite) },
                   //{ "terminateW|TerminationCharactersOfWrite=", "Termination Characters of Serial  When Write (Default 0x0A(\"\\n\"))", v =>  GlobalVars.theWriteTerminationCharactersOfRS232 = Convert.ToByte(v,16) }, // --terminateW "" 时会产生异常,用 Byte.TryParse()
                   //{ "terminateR|TerminationCharactersOfRead=",  "Termination Characters of Serial  When Read  (Default 0x0A(\"\\n\"))", v =>   GlobalVars.theReadTerminationCharactersOfRS232 = Convert.ToByte(v,16) },
                { "terminateW|TerminationCharactersOfWrite=", "Termination Characters of Serial  When Write (Default 0x0A(\"\\n\"))", v =>  Byte.TryParse(Regex.Replace(v,@"0[x,X]",""),NumberStyles.HexNumber,/*CultureInfo.CurrentCulture*/null,out GlobalVars.theWriteTerminationCharactersOfRS232) }, //https://stackoverflow.com/questions/2801509/uint32-tryparse-hex-number-not-working/3570612#3570612     https://stackoverflow.com/questions/16117043/regular-expression-replace-in-c-sharp/16117150#16117150
                { "terminateR|TerminationCharactersOfRead=",  "Termination Characters of Serial  When Read  (Default 0x0A(\"\\n\"))", v =>  Byte.TryParse(Regex.Replace(v,@"0[x,X]",""),NumberStyles.HexNumber,/*CultureInfo.CurrentCulture*/null,out GlobalVars.theReadTerminationCharactersOfRS232) },
                
                
                //USBTMC  USB0::0x0699::0x0415::C022855::INSTR  少了等号=导致出现 解析为 USB0::0xvid::0xpid::sn::INSTR
                { "U|useUSBTMC", "USBTMC mode", v => GlobalVars.VISA_CLI_Option_CurrentMode = (short)Mode.USBTMC},
                { "usb|usbBoardIndex=", "USB board index(Default 0)", v => short.TryParse(v,out GlobalVars.VISA_CLI_Option_USB_BoardIndex) },
                { "vid|usbVID=", "USB Vendor ID", v =>  GlobalVars.VISA_CLI_Option_USB_VID = v},
                { "pid|usbPID=", "USB Model ID", v => GlobalVars.VISA_CLI_Option_USB_PID = v},
                { "sn|usbSerialNumber=", "USB Serial Number", v => GlobalVars.VISA_CLI_Option_USB_SerialNumber = v},
                
                //TCPIP  TCPIP0::192.168.1.2::inst0::INSTR
                { "T|useTCPIP", "TCPIP mode", v => GlobalVars.VISA_CLI_Option_CurrentMode = (short)Mode.TCPIP},
                { "tcpip|tcpipAdapterBoardIndex=", "TCPIP Adapter board index(Default 0)", v => short.TryParse(v,out GlobalVars.VISA_CLI_Option_TCPIP_BoardIndex) },
                { "ip|ipAddress=", "IP Address  or hostname of the device", v => GlobalVars.VISA_CLI_Option_TCPIP_IPAddressOrHostName = v},
                { "inst|instNumber=", "LAN Device Name :inst number ,(Default 0)",  v => short.TryParse(v,out GlobalVars.VISA_CLI_Option_TCPIP_instNumber)},
                
                
                //Common
                { "C|cmdstr|CommandString=", "command(s) to send to the device", v =>  GlobalVars.VISA_CLI_Option_CommandString = v },
                { "W|write|JustWriteCommand", "just write (default)", v =>  GlobalVars.VISA_CLI_Option_JustWriteCommand = v != null },
                { "R|read|JustReadBack", "just read back", v =>  GlobalVars.VISA_CLI_Option_JustReadBack = v != null },
                { "Q|query|QueryCommand", "the command is a query command", v =>  GlobalVars.VISA_CLI_Option_isQueryCommand = v != null },
                { "D|debug|PrintDebugMessage", "prints debug messages", v =>  GlobalVars.VISA_CLI_Option_PrintDebugMessage  = v != null },
                { "F|save2file|FileName=", "save the response binary data to specify file", v =>  GlobalVars.VISA_CLI_Option_FileName = v },
                { "O|overwrite|OverwriteFile", "if file exist ,overwrite it", v =>  GlobalVars.VISA_CLI_Option_OverwriteFile = v != null },
                { "N|rBytes|ReadBackNbytes=", "how many bytes should be read back", v =>  Decimal.TryParse(v,NumberStyles.Any,/*CultureInfo.CurrentCulture*/null,out GlobalVars.VISA_CLI_Option_ReadBackNbytes)},
                { "E|skip|SkipFirstNbytes=", "skip first n bytes of received data", v =>  Decimal.TryParse(v,NumberStyles.Any,/*CultureInfo.CurrentCulture*/null,out GlobalVars.VISA_CLI_Option_SkipFirstNbytes)},
                { "L|ls|ListAllInstruments", "List All Instruments on interface", v =>  GlobalVars.VISA_CLI_Option_ListInstruments = v != null },
                { "X|dcl|DeviceClear", "Send Device Clear before commands send ", v =>  GlobalVars.VISA_CLI_Option_isDeviceClearSend = v != null },
                { "I|InteractiveMode", "Interactive Mode ", v =>  GlobalVars.VISA_CLI_Option_isInteractiveMode = v != null },
                { "t|timeout=", "Timeout milliseconds (Default 10000ms) ", v =>   Decimal.TryParse(v,NumberStyles.Any,/*CultureInfo.CurrentCulture*/null,out GlobalVars.VISASessionTimeout) },
                { "v|visa|VisaResourceName=", "VISA Resource Name, if this filed specified, Mode and model related parameters should be omitted", v =>  GlobalVars.VISAResourceName = v },
                { "m|mix|MixMode", "Support Mix string input, For example  string  '0x39\\37\\x398' will be prase as string '9798' , the priority of this switch is the highest, if both --MixMode  and --HexInputMode specified, string  '0x39\\37\\x398' will be prase as string '9798' at first ,then it will be treat as hex string and prase as string 'ab' finally", v =>  GlobalVars.VISA_CLI_Option_isMixMode  = v != null },
                { "i|hi|Hi|HexInputMode", "Treat argument of --CommandString as hexadecimal, please  see option --MixMode for detail", v =>  GlobalVars.VISA_CLI_Option_isInputModeHex  = v != null },
                { "o|ho|Ho|HexOutputMode", "Format output as hexadecimal string,this function ONLY applied on the standard output, when save to file,data will always be saved as raw binary", v =>  GlobalVars.VISA_CLI_Option_isOutputModeHex = v != null },
                { "c|clear|ClearConsole", "clear the console before each operation", v =>  GlobalVars.VISA_CLI_Option_isClearConsole = v != null },
                { "l|cycle|LoopCycle=", "The cycle of  loop mode （Default : 1 cycle ,operate once）", v =>  Decimal.TryParse(v,NumberStyles.Any,/*CultureInfo.CurrentCulture*/null,out GlobalVars.VISA_CLI_Option_CycleOfLoopMode)},
                { "delay|DelayTime=", "The delay time(milliseconds) in loop mode(Default 0 ms)", v =>  Decimal.TryParse(v,NumberStyles.Any,/*CultureInfo.CurrentCulture*/null,out GlobalVars.VISA_CLI_Option_DelayTimeOfLoopMode_ms)},
                { "h|?|help",  "show this message and exit.", v => showHelp = v != null },
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
                Console.WriteLine("Usage: {0} -G|-S  [MODE related options] -C \"command string\"", System.AppDomain.CurrentDomain.FriendlyName);// System.Diagnostics.Process.GetCurrentProcess().ProcessName); //https://stackoverflow.com/questions/37459509/how-to-get-the-exe-name-while-the-program-is-running/37459553#37459553
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
                case ((short)Mode.USBTMC):     //USBTMC  USB0::0x0699::0x0415::C022855::INSTR  与 USB0::0x699::0x415::C022855::INSTR 是一样的
                    {
                        GlobalVars.VISAResourceName = String.Empty; // 清空原字符串
                        GlobalVars.VISAResourceName = "USB"+GlobalVars.VISA_CLI_Option_USB_BoardIndex.ToString()
                                                       + "::0x" + Regex.Replace(GlobalVars.VISA_CLI_Option_USB_VID, @"0[x,X]", "")
                                                       + "::0x" + Regex.Replace(GlobalVars.VISA_CLI_Option_USB_PID, @"0[x,X]", "")
                                                       + "::" + GlobalVars.VISA_CLI_Option_USB_SerialNumber
                                                       + "::INSTR";
                        return true;
                    }
                case ((short)Mode.TCPIP):     //TCPIP0::192.168.1.2::inst0::INSTR  TCPIP0::HostName::inst0::INSTR   IP地址或者主机名都可以
                    {
                        GlobalVars.VISAResourceName = String.Empty; // 清空原字符串
                        GlobalVars.VISAResourceName = "TCPIP" + GlobalVars.VISA_CLI_Option_TCPIP_BoardIndex.ToString()
                                                     + "::" + GlobalVars.VISA_CLI_Option_TCPIP_IPAddressOrHostName
                                                     + "::inst" + GlobalVars.VISA_CLI_Option_TCPIP_instNumber.ToString()
                                                     + "::INSTR";
                        return true;
                    }
                default : return false;
            }
        }
        public static void SetSerialAttribute(ref MessageBasedSession mbs)
        {
            SerialSession ser = (SerialSession)mbs;
                        ser.BaudRate = GlobalVars.VISA_CLI_Option_SerialBaudRate; //设置速率
                        ser.DataBits = GlobalVars.VISA_CLI_Option_SerialDataBits; //数据位
                        ser.StopBits = GlobalVars.VISA_CLI_Option_SerialStopBits; //停止位 
                          ser.Parity = GlobalVars.VISA_CLI_Option_SerialParity;   //校验     NONE 0  Odd 1  Even 2 Mark 3 Space 4
                     ser.FlowControl = GlobalVars.VISA_CLI_Option_SerialFlowControl;   //Flow Control  NONE 0  XON/XOFF 1   使用 NI I/O Trace 监视   VISA Test Panel 设置时得到
            ser.TerminationCharacter = GlobalVars.theReadTerminationCharactersOfRS232; //结束符，针对每个串口只能设置唯一的结束符,此处留给 Read操作,Write操作直接手动在命令末尾加指定的结束符
                 ser.ReadTermination = GlobalVars.VISA_CLI_Option_SerialTerminationMethodWhenRead;  // 读回结束符选择
                                                                                                    //VI_ATTR_ASRL_END_IN indicates the method used to terminate read operations
                ser.WriteTermination = GlobalVars.VISA_CLI_Option_SerialTerminationMethodWhenWrite; // 写入结束符选择(ser.TerminationCharacter 定义的终止符优先给读回终止符, 写入终止符由 --stmW=2 和 --terminateW="0x0A" 共同指定,若--terminateW不指定,默认使用0x0aA）
                                                                                                    // VI_ATTR_ASRL_END_OUT indicates the method used to terminate write operations
            if (GlobalVars.VISA_CLI_Option_PrintDebugMessage)
            {
                Console.WriteLine("当前正使用COM通信,当前设置为:\n速率 : " + GlobalVars.VISA_CLI_Option_SerialBaudRate.ToString()+ " bps"
                    + "\n数据位 : " + GlobalVars.VISA_CLI_Option_SerialDataBits.ToString() 
                    + "\n停止位 : " + (((int)GlobalVars.VISA_CLI_Option_SerialStopBits) / 10).ToString() 
                    + "\n校验方式(Parity) : " + GlobalVars.VISA_CLI_Option_SerialParityEnumList[((int)GlobalVars.VISA_CLI_Option_SerialParity)]
                    + "\nFlowControl : " + GlobalVars.VISA_CLI_Option_SerialFlowControlEnumList[((int)GlobalVars.VISA_CLI_Option_SerialFlowControl)]
                    + "\nTerminationCharacter : 0x" + GlobalVars.theReadTerminationCharactersOfRS232.ToString("X2") //https://stackoverflow.com/questions/5426582/turn-byte-into-two-digit-hexadecimal-number-just-using-tostring/5426587#5426587
                    + "\nTerminationCharactersOfRead : 0x" + GlobalVars.theReadTerminationCharactersOfRS232.ToString("X2")
                    + "\nTerminationCharactersOfWrite : 0x" + GlobalVars.theWriteTerminationCharactersOfRS232.ToString("X2")
                    + "\nSerialTerminationMethodOfRead : " + GlobalVars.VISA_CLI_Option_SerialTerminationMethodEnumList[((int)GlobalVars.VISA_CLI_Option_SerialTerminationMethodWhenRead)]
                    + "\nSerialTerminationMethodOfWrite : " + GlobalVars.VISA_CLI_Option_SerialTerminationMethodEnumList[((int)GlobalVars.VISA_CLI_Option_SerialTerminationMethodWhenWrite)]
                    + "\n\n请确保仪器设置与本设置相符"
                                 );
            }
        }
        public static void ListAll_TMC_Devices()
        { 
             //对满足 TMC的设备都支持-ls列出                                                                                            
             // (GPIB[0-9]{1,}::[0-9]{1,}::INSTR|TCPIP[0-9]{1,}::.*::INSTR|USB[0-9]{1,}.*::INSTR)     USB0::0x0699::0x0415::C022855::INSTR TCPIP0::192.168.1.2::inst0::INSTR  GPIB0::2::INSTR            https://www.regextester.com/93690
            String[] resources = ResourceManager.GetLocalManager().FindResources("?*");
            Regex regex = new Regex(@"(GPIB[0-9]{1,}::[0-9]{1,}::INSTR|TCPIP[0-9]{1,}::.*::INSTR|USB[0-9]{1,}.*::INSTR)", RegexOptions.IgnoreCase); //由于该正则表达式在 FindResources("")中不被识别,直接被解析为了 viFindRsrc (0x00001001, "(GPIB[0-9]{1,}::[0-9]{1,}::INSTR|TCPIP[0-9]{1,}::.*::INSTR|USB[0-9]{1,}.*::INSTR)", 0x00000000, 0 (0x0), "") ,因此另用正则表达式匹配 https://www.dotnetperls.com/regex
            foreach (String res in resources)
            {
                if (regex.Match(res).Success)
                {
                    MessageBasedSession mbs = (MessageBasedSession)ResourceManager.GetLocalManager().Open(res);
                    mbs.Clear(); //it's better send a Device Clear before operation
                    String IDN = mbs.Query("*IDN?");
                    Console.Write(res.PadRight(20) + "   " + IDN);
                }
            }
            //此处不能用return,return后程序继续执行,导致出现 ： 指定的资源引用非法。解析出错。  VISA error code -1073807342 (0xBFFF0012), ErrorInvalidResourceName  viParseRsrcEx (0x00001001, NULL, 0 (0x0), 0 (0x0), "", "", "")
            Environment.Exit(0);
        }
        public static void  SendDeviceClear()
        {    
            if ((GlobalVars.VISA_CLI_Option_CurrentMode == (short)Mode.GPIB) || GlobalVars.VISA_CLI_Option_isDeviceClearSend)
            {
                GlobalVars.mbSession.Clear(); //it's better send a Device Clear before operation
            }
        }
        public static void  Write()
        {
            SendDeviceClear();
            if (GlobalVars.VISA_CLI_Option_isMixMode)  // High Priority of mix mode ,then hex mode
            {
                ProcessMixedHex();
            }
            Byte[] ba = GlobalVars.VISA_CLI_Option_isInputModeHex ? (DRDigit.Fast.FromHexString(GlobalVars.VISA_CLI_Option_CommandString)): (Encoding.Default.GetBytes(GlobalVars.VISA_CLI_Option_CommandString));
            GlobalVars.mbSession.Write(ba); // use Write(Byte[]) instead of Write(String)
            //GlobalVars.mbSession.Write(GlobalVars.VISA_CLI_Option_CommandString); // use Write(Byte[]) instead of Write(String)
        }
        public static void Read()
        {
            GlobalVars.VISA_CLI_ReadBackBuffer = null;
            // GlobalVars.VISA_CLI_ReadBackBuffer = GlobalVars.mbSession.ReadString(Convert.ToInt32(GlobalVars.VISA_CLI_Option_ReadBackNbytes));
            GlobalVars.VISA_CLI_ReadBackBuffer = GlobalVars.mbSession.ReadByteArray(Convert.ToInt32(GlobalVars.VISA_CLI_Option_ReadBackNbytes));
            //https://stackoverflow.com/questions/2530951/remove-first-16-bytes/2530994#2530994
            //https://stackoverflow.com/questions/5062233/is-there-a-away-to-convert-ilistarraysegmentbyte-to-byte-without-enumerati
            if (GlobalVars.VISA_CLI_Option_SkipFirstNbytes > 0)
            {
                ArraySegment<byte> segment = new ArraySegment<byte>(GlobalVars.VISA_CLI_ReadBackBuffer, Convert.ToInt32(GlobalVars.VISA_CLI_Option_SkipFirstNbytes), GlobalVars.VISA_CLI_ReadBackBuffer.Length - Convert.ToInt32(GlobalVars.VISA_CLI_Option_SkipFirstNbytes));
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
            //GlobalVars.VISA_CLI_ReadBackBuffer =GlobalVars.mbSession.Query(GlobalVars.VISA_CLI_Option_CommandString,Convert.ToInt32(GlobalVars.VISA_CLI_Option_ReadBackNbytes));
            if (GlobalVars.VISA_CLI_Option_PrintDebugMessage)
            {
                Console.WriteLine("{0} byte request And GlobalVars.VISA_CLI_ReadBackBuffer.Length={1} byte actually transfered", Convert.ToInt32(GlobalVars.VISA_CLI_Option_ReadBackNbytes), GlobalVars.VISA_CLI_ReadBackBuffer.Length);
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
            GlobalVars.VISA_CLI_Option_FileName = Path.GetFileNameWithoutExtension(GlobalVars.VISA_CLI_Option_FileName)+"_" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + Path.GetExtension(GlobalVars.VISA_CLI_Option_FileName);
            if (GlobalVars.VISA_CLI_Option_PrintDebugMessage)
            {
                Console.WriteLine("new file name is :{0}", GlobalVars.VISA_CLI_Option_FileName);
            }
        }

        public static bool SaveResponseToFile()
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
            //bw.Write(String.Join(String.Empty, GlobalVars.VISA_CLI_ReadBackBuffer.Skip(Convert.ToInt32(GlobalVars.VISA_CLI_Option_SkipFirstNbytes))),0,GlobalVars.VISA_CLI_ReadBackBuffer.Length); 
            // bw.Write(String.Join(String.Empty, GlobalVars.VISA_CLI_ReadBackBuffer.Skip(Convert.ToInt32(GlobalVars.VISA_CLI_Option_SkipFirstNbytes))));


            bw.Write(GlobalVars.VISA_CLI_ReadBackBuffer);
            bw.Close(); //Close
            //fs.Close(); //Close      警告 CA2202  可以在方法 'VISA_CLI.SaveResponseToFile()' 中多次释放对象 'fs'。若要避免生成 System.ObjectDisposedException，不应对一个对象多次调用 Dispose。: Lines: 282   VISA_CLI D:\Data\Github\VISA_CLI1\src\Main.cs    282 活动的

            if (GlobalVars.VISA_CLI_Option_PrintDebugMessage)
            {
                FileInfo fi = new FileInfo(GlobalVars.VISA_CLI_Option_FileName);
                Console.WriteLine("write to  file  :{0}  completely, {1} bytes total", GlobalVars.VISA_CLI_Option_FileName, fi.Length);
            }
            return true;
        }
		//https://stackoverflow.com/questions/3448116/convert-ascii-hex-codes-to-character-in-mixed-string/3448349#3448349
        public static bool ProcessMixedHex()
        {
            Regex regex = new Regex(@"\\(x)?[0-9,a-f,A-F]{2}");
            var matches = regex.Matches(GlobalVars.VISA_CLI_Option_CommandString);
            foreach (Match match in matches)
            {
                GlobalVars.VISA_CLI_Option_CommandString = GlobalVars.VISA_CLI_Option_CommandString.Replace(match.Value, ((char)Convert.ToByte(Regex.Replace(match.Value, @"\\(x)?", ""), 16)).ToString()); //https://docs.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regex.replace?view=netframework-4.8
            }         

            regex = new Regex(@"0x[0-9,a-f,A-F]{2}");
            matches = regex.Matches(GlobalVars.VISA_CLI_Option_CommandString);
            foreach (Match match in matches)
            {
                GlobalVars.VISA_CLI_Option_CommandString = GlobalVars.VISA_CLI_Option_CommandString.Replace(match.Value, ((char)Convert.ToByte(match.Value.Replace(@"0x", ""), 16)).ToString());
            }

            return true;
        }
        public static bool OperateOnce()
        {
            if (GlobalVars.VISA_CLI_Option_isQueryCommand && !String.IsNullOrEmpty(GlobalVars.VISA_CLI_Option_CommandString))  // query
            {
                Query();
                GlobalVars.VISA_CLI_Option_JustWriteCommand = false;
                GlobalVars.VISA_CLI_Option_JustReadBack = false;
            }
            else if (GlobalVars.VISA_CLI_Option_JustReadBack) //Just Read Back
            {
                Read();
                GlobalVars.VISA_CLI_Option_JustWriteCommand = false;
            }
            else if (GlobalVars.VISA_CLI_Option_JustWriteCommand && !String.IsNullOrEmpty(GlobalVars.VISA_CLI_Option_CommandString)) //Just write  
            {
                Write();
            }

            // 读回内容不为空
            if (!(GlobalVars.VISA_CLI_ReadBackBuffer == null || GlobalVars.VISA_CLI_ReadBackBuffer.Length == 0)) //save respond to file
            {
                if (!String.IsNullOrEmpty(GlobalVars.VISA_CLI_Option_FileName)) // 如果指定了文件名则保存内容到文件
                {
                    SaveResponseToFile();
                }
                else
                {
                    //https://www.cnblogs.com/michaelxu/archive/2007/05/14/745881.html
                    //Console.WriteLine(System.Text.Encoding.ASCII.GetString(GlobalVars.VISA_CLI_ReadBackBuffer));//
                    Console.Write((GlobalVars.VISA_CLI_Option_isOutputModeHex) ? (DRDigit.Fast.ToHexString(GlobalVars.VISA_CLI_ReadBackBuffer)+"\n") : (System.Text.Encoding.Default.GetString(GlobalVars.VISA_CLI_ReadBackBuffer).TrimEnd('\0')));//                  
                }
            }
            return true;
        }
		
        public static bool Interactive()
        {
            GlobalVars.CounterOfEnterInteractiveMode += 1; //每次进入交互模式计数器加一
            var prompt = GlobalVars.InteractivePromptString + ">";
            var startupMsg = "Now will enter interactive mode......\n       Tips: Type commands like linux shell (Tab to auto complete | Ctrl + C to quit) then press enter to write to device \n or Press Enter to read response";
            if(GlobalVars.CounterOfEnterInteractiveMode > 1) //如果交互模式计数器大于1,不要打印提示消息
            {
                startupMsg = String.Empty;
            }
            List<string> completionList = new List<string> { "test", "contractearnings", "cancels", "cancellationInfo", "cantankerous" };
            InteractivePrompt.Run(
                ((strCmd, promptt, listCmd) =>
                {
                    if (GlobalVars.VISA_CLI_Option_isClearConsole) { Console.Clear(); }
                    //return strCmd.Length.ToString() + Environment.NewLine;
                    //var handleInput = "(((--> " + strCmd + " <--)))";
                    //return handleInput + Environment.NewLine;
                    if (!String.IsNullOrWhiteSpace(strCmd))
                    {
                        GlobalVars.VISA_CLI_Option_CommandString = strCmd;
                        Write();
                        return "cmdStr is " + GlobalVars.VISA_CLI_Option_CommandString + " Write" + Environment.NewLine;
                    }
                    else
                    {
                        GlobalVars.VISA_CLI_Option_CommandString = String.Empty; //如果输入为空，则将GlobalVars.VISA_CLI_Option_CommandString置为空,防止在异常发生后尝试再次进入交互模式时判断出现错误  while (GlobalVars.VISA_CLI_Option_isInteractiveMode || String.IsNullOrEmpty(GlobalVars.VISA_CLI_Option_CommandString))
                        Read();
                        return "cmdStr is empty,Read(）" + Environment.NewLine + "ReadBack buffer is:" + Environment.NewLine + ((GlobalVars.VISA_CLI_Option_isOutputModeHex) ? (DRDigit.Fast.ToHexString(GlobalVars.VISA_CLI_ReadBackBuffer) + "\n") : (System.Text.Encoding.Default.GetString(GlobalVars.VISA_CLI_ReadBackBuffer).TrimEnd('\0')));
                    }
                }), prompt, startupMsg, completionList);
            return true;
        }
        static Int32 Main(string[] args)
        {
            
            Stopwatch sw = Stopwatch.StartNew();
            //尝试解析各参数
            VISA_CLI.ParseArgs(args);

            //根据各参数开始执行命令
            //首先根据用户需求生成相应的资源名称,如果未指定 -ls参数 
            if(!(GlobalVars.VISA_CLI_Option_ListInstruments))
            { 
                //且未指定 GPIB / Serial / USB / TCPIP 中的任何一种模式且未指定VISA资源名称,显示错误信息
                if ((String.IsNullOrEmpty(GlobalVars.VISAResourceName) && !(GenerateVISAResourceName(GlobalVars.VISA_CLI_Option_CurrentMode)))) //https://docs.microsoft.com/en-us/dotnet/api/system.console.error?redirectedfrom=MSDN&view=netframework-4.7.2#System_Console_Error
                {
                 
                    var standardError = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
                    Console.SetError(standardError);
                    Console.Error.WriteLine("mode or visa resource name must be specified!");
                    Console.Error.WriteLine("       {0} -h for more information", System.AppDomain.CurrentDomain.FriendlyName);
                    return -1;
                }
                //GPIB / Serial / USB / TCPIP / visa 必须指定一个,如果多个模式被指定,则最后指定的模式生效
                //                                                                                GPIB                                                    Serial                                                                                                            USB                                                                                                                                                  TCPIP
                else if (String.IsNullOrEmpty(GlobalVars.VISAResourceName) && ((GlobalVars.VISA_CLI_Option_GPIB_PrimaryAddress < 0) && (GlobalVars.VISA_CLI_Option_Serial_PortNumber < 0) && (String.IsNullOrEmpty(GlobalVars.VISA_CLI_Option_USB_VID) || String.IsNullOrEmpty(GlobalVars.VISA_CLI_Option_USB_PID) || String.IsNullOrEmpty(GlobalVars.VISA_CLI_Option_USB_SerialNumber)) && (String.IsNullOrEmpty(GlobalVars.VISA_CLI_Option_TCPIP_IPAddressOrHostName)))) //https://docs.microsoft.com/en-us/dotnet/api/system.console.error?redirectedfrom=MSDN&view=netframework-4.7.2#System_Console_Error
                {
                    var standardError = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
                    Console.SetError(standardError);
                    Console.Error.WriteLine("GPIB Primary Address or Serial Port Number or USB PID/VID/SN  or  IP address/host name  or VISA resource name must be specified!");
                    return -1;
                }

            }
            //尝试进行操作
            try
            {
                if (GlobalVars.VISA_CLI_Option_isClearConsole) { Console.Clear(); }
                if (GlobalVars.VISA_CLI_Option_ListInstruments) { ListAll_TMC_Devices(); } //list all device then exit

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
                    if (SerialTerminationMethod.TerminationCharacter == GlobalVars.VISA_CLI_Option_SerialTerminationMethodWhenWrite)  //如果用户指定了要使用写入终止符
                    {
                        GlobalVars.VISA_CLI_Option_CommandString += GlobalVars.VISA_CLI_Option_isInputModeHex ? DRDigit.Fast.ToHexString(new[] { GlobalVars.theWriteTerminationCharactersOfRS232 }) : (System.Text.Encoding.ASCII.GetString(new[] { GlobalVars.theWriteTerminationCharactersOfRS232 }));//GlobalVars.theWriteTerminationCharactersOfRS232.ToString(); // 更新要发送的命令，末尾加指定的结束符,此处手动添加,系统中终止符只能由  ser.TerminationCharacter 统一设定,为了灵活性,将这一设置让给Read操作，Write操作直接手动在此处在命令末尾加指定的结束符 
                                                                                                                                                                    //https://stackoverflow.com/questions/22135275/how-to-convert-a-single-byte-to-a-string/22135328#22135328
                    }
                }

                GlobalVars.mbSession.Timeout = Convert.ToInt32(GlobalVars.VISASessionTimeout); //设置超时

                //执行操作
                /* 和C版本一样，采用两个函数 OperateOnce 与 Interactive, 在其中细分 
                     ① 如果配置正确 mode/ index.... 或 VISA资源名称且   cmdstr 且 interactive 被指定, 则先执行相应操作再进入交互模式
                     ② 如果配置正确 mode/ index.... 或 VISA资源名称且 无cmdstr 且 interactive 被指定 进入交互模式
                     ③ 如果配置正确 mode/ index.... 或 VISA资源名称且 无cmdstr 且 interactive 未指定 直接进入交互模式
                */
                while (true)  //循环模式下即使异常发生仍旧继续
                 {
                        try
                        {
                         do{
                              GlobalVars.VISA_CLI_Option_CycleOfLoopModeCounter++;   //必须在 OperateOnce();前增加计数器,如果放到之后,一旦每次都有异常发生,计数器永远不会增加,循环次数显示便无效
                              OperateOnce();
                               if (GlobalVars.VISA_CLI_Option_CycleOfLoopMode > 1 && GlobalVars.VISA_CLI_Option_CycleOfLoopMode > GlobalVars.VISA_CLI_Option_CycleOfLoopModeCounter) //单次模式 或者 循环的最后一次不用延时
                               {
                                Thread.Sleep(Convert.ToInt32(GlobalVars.VISA_CLI_Option_DelayTimeOfLoopMode_ms));
                                }
                            }while (GlobalVars.VISA_CLI_Option_CycleOfLoopMode > GlobalVars.VISA_CLI_Option_CycleOfLoopModeCounter);
                        }
                        catch (Exception exp)
                        {
                            Console.WriteLine(exp.Message);
                            continue;
                        }
                        break;
                 }
                //进入Interactive模式,可能会有异常发生,例如读取超时异常,一般情况下程序将会退出;本程序设置为即使有异常发生也继续执行 https://forums.asp.net/t/1626951.aspx?How+to+continue+after+exception+occurred+in+C+
                while (GlobalVars.VISA_CLI_Option_isInteractiveMode || String.IsNullOrEmpty(GlobalVars.VISA_CLI_Option_CommandString))
                    {
                        try
                        {
                            Interactive();
                        }
                        catch (Exception exp)
                        {
                            Console.WriteLine(exp.Message);
                            continue;
                        }
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

            }
            
            sw.Stop();
            if (GlobalVars.VISA_CLI_Option_PrintDebugMessage)
            {
                Console.WriteLine("Time taken: {0}ms", sw.Elapsed.TotalMilliseconds);
            }
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
       //对RS232接口,发送命令时可能需要加特定字符  作为结束 例如 LF(\n <==> 0x0A)  CR（\r <==> 0x0D)等        
        public static Byte theWriteTerminationCharactersOfRS232 = 0x0A;
             //读取时的结束字符 1 字节
        public static Byte theReadTerminationCharactersOfRS232  = 0x0A;
        //RS232设置用
        public static Int32 VISA_CLI_Option_SerialBaudRate = 19200; //Serial速率
        public static short VISA_CLI_Option_SerialDataBits = 8; //数据位
        public static StopBitType VISA_CLI_Option_SerialStopBits = (StopBitType)10; //停止位
        public static List<String> VISA_CLI_Option_SerialParityEnumList = new List<String> { "NONE", "Odd", "Even", "Mark ", "Space" }; //校验方式列表
        public static Parity VISA_CLI_Option_SerialParity = (Parity)0; //校验     NONE 0  Odd 1  Even 2 Mark 3 Space 4
        public static List<String> VISA_CLI_Option_SerialFlowControlEnumList = new List<String>(new String[] { "NONE", "XON/XOFF" }); //FlowControl方式列表
        public static FlowControlTypes VISA_CLI_Option_SerialFlowControl = (FlowControlTypes)0; ////Flow Control  NONE 0  XON/XOFF 1   使用 NI I/O Trace 监视   VISA Test Panel 设置时得到
        public static SerialTerminationMethod VISA_CLI_Option_SerialTerminationMethodWhenRead  = SerialTerminationMethod.TerminationCharacter;//读回结束符选择 None 0   LastBit 1   TerminationCharacter 2   Break 3
        public static SerialTerminationMethod VISA_CLI_Option_SerialTerminationMethodWhenWrite = SerialTerminationMethod.None;              //写入结束符选择 None 0   LastBit 1   TerminationCharacter 2   Break 3
        public static List<String> VISA_CLI_Option_SerialTerminationMethodEnumList = new List<String>(new String[] { "None", "LastBit", "TerminationCharacter", "Break" });

        //USB
        public static short VISA_CLI_Option_USB_BoardIndex = 0;
        public static String VISA_CLI_Option_USB_VID = String.Empty;
        public static String VISA_CLI_Option_USB_PID = String.Empty;
        public static String VISA_CLI_Option_USB_SerialNumber = String.Empty;


        //TCPIP
        public static short VISA_CLI_Option_TCPIP_BoardIndex           = 0;
        public static String VISA_CLI_Option_TCPIP_IPAddressOrHostName = String.Empty;
        public static short VISA_CLI_Option_TCPIP_instNumber           = 0;


        //Common
        public static String VISA_CLI_Option_CommandString = null;         //command to send
        public static bool VISA_CLI_Option_JustWriteCommand = true;        //just send command (default)
        public static bool VISA_CLI_Option_JustReadBack = false;           //just read back
        public static bool VISA_CLI_Option_isQueryCommand = false;         // is a query command
        public static bool VISA_CLI_Option_PrintDebugMessage = false; //debug switch
        public static String VISA_CLI_Option_FileName = null;           //when file name specified, save binary response to file
        public static bool VISA_CLI_Option_OverwriteFile = false;     //if file exist ,overwrite it
        public static Decimal VISA_CLI_Option_ReadBackNbytes = 10240;      //read specified length of response,default is 1024 bytes
        public static Decimal VISA_CLI_Option_SkipFirstNbytes = 0;     //for some system(DCA86100,AQ6370,etc.), transfered data via GPIB contain extra bytes,user can skip them

        public static bool VISA_CLI_Option_isDeviceClearSend = false; //是否发送DeviceClear命令,对GPIB接口默认情况为发送
        public static String InteractivePromptString = Regex.Replace(System.AppDomain.CurrentDomain.FriendlyName, @".exe", "");
        public static bool VISA_CLI_Option_isInteractiveMode = false;  //是否进入交互模式
        public static UInt16 CounterOfEnterInteractiveMode = 0; //可能存在异常发生后多次进入交互模式的情况,设置该计数器以便后续判断是否打印提示信息
        public static   MessageBasedSession mbSession;
        public static   UsbRaw      USBRAW_Session;      //USB
        public static String VISAResourceName = null;
        public static Decimal  VISASessionTimeout= 10000; //10000ms   //https://stackoverflow.com/questions/32184971/tryparse-not-working-when-trying-to-parse-a-decimal-number-to-an-int/32185117#32185117
        public static bool VISA_CLI_Option_isInputModeHex = false;   //是否将输入字符串视为十六进制字符串
        public static bool VISA_CLI_Option_isOutputModeHex = false;  //将输出格式化为十六进制字符串
        public static bool VISA_CLI_Option_isClearConsole = false;   //每次读写操作前清理控制台

        public static Decimal VISA_CLI_Option_DelayTimeOfLoopMode_ms = 0; //进入循环操作模式后每次循环的时间间隔
        public static Decimal VISA_CLI_Option_CycleOfLoopMode = 1; //总循环次数
        public static Int32   VISA_CLI_Option_CycleOfLoopModeCounter = 0; //循环次数计数器

        public static bool VISA_CLI_Option_isMixMode = false;   //混合格式输入0x39\37\x398 等效于 字符串 9798
    }
}
