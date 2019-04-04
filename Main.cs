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

namespace VISA_CLI
{
    class myVISA_CLI
    {  
        public static void ParseArgs(string[] args) //https://mail.gnome.org/archives/commits-list/2012-December/msg00139.html
        {
            bool showHelp = true;
            var p = new OptionSet() {
                { "g|GPIB", "use GPIB", v => GlobalVars.VISA_CLI_Option_UseGPIB = true },
                { "b|gpib|BoardIndex=", "    board index(GPIB board index)", v =>  short.TryParse(v,out GlobalVars.VISA_CLI_Option_GPIB_BoardIndex) },  //https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/types/how-to-convert-a-string-to-a-number
                { "p|pad|PrimaryAddress=", "    primary address", v =>  short.TryParse(v,out GlobalVars.VISA_CLI_Option_GPIB_PrimaryAddress) },
                { "s|sad|SecondaryAddress=", "    secondary address", v =>  short.TryParse(v,out GlobalVars.VISA_CLI_Option_GPIB_SecondaryAddress ) },


                //Common
                { "c|cmdstr|CommandString=", "command(s) to send to the device", v =>  GlobalVars.VISA_CLI_Option_CommandString = v },
                { "q|query|QueryCommand", "the command is a query command", v =>  GlobalVars.VISA_CLI_Option_isQueryCommand = v != null },
                { "d|debug|PrintDebugMessage", "prints debug messages", v =>  GlobalVars.VISA_CLI_Option_PrintDebugMessage  = v != null },
                { "f|save2file|FileName=", "save the response binary data to specify file", v =>  GlobalVars.VISA_CLI_Option_FileName = v },
                { "o|overwrite|OverwriteFile", "prints debug messages", v =>  GlobalVars.VISA_CLI_Option_OverwriteFile = v != null },
                { "r|rBytes|ReadBackNbytes=", "how many bytes should be read back", v =>  int.TryParse(v,out GlobalVars.VISA_CLI_Option_ReadBackNbytes ) },
                { "e|skip|SkipFirstNbytes=", " skip first n bytes of received data", v =>  int.TryParse(v,out GlobalVars.VISA_CLI_Option_SkipFirstNbytes ) },
                { "h|help",  "show this message and exit.", v => showHelp = v != null },
            };

            try
            {
                p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("tasque: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `tasque --help' for more information.");
            }

            if (showHelp)
            {
                Console.WriteLine("Usage: tasque [[-q|--quiet] [[-b|--backend] BACKEND]]");
                Console.WriteLine();
                Console.WriteLine("Options:");
                p.WriteOptionDescriptions(Console.Out);
            }
        }
        public static bool GenerateVISAResourceName()
        {
            if(GlobalVars.VISA_CLI_Option_UseGPIB)//GPIB  GPIB0::2::INSTR
            {
                GlobalVars.VISAResourceName = String.Empty; // 清空原字符串
                GlobalVars.VISAResourceName = "GPIB" + GlobalVars.VISA_CLI_Option_GPIB_BoardIndex +"::"+GlobalVars.VISA_CLI_Option_GPIB_PrimaryAddress+"::INSTR";
            }
            return true;
        }
        static Int32 Main(string[] args)
        {
            //Console.WriteLine("Hello World!");
            //myHelp.Show_gpib_help();
            //Console.ReadLine();
            //尝试解析各参数
            myVISA_CLI.ParseArgs(args);
            //根据各参数开始执行命令
            //首先根据用户需求生成相应的资源名称(ASRL2::INSTR、GPIB0::2::INSTR)
            GenerateVISAResourceName();
            //尝试打开资源
             try
            {
                // GlobalVars.mbSession = (MessageBasedSession)ResourceManager.GetLocalManager().Open(GlobalVars.VISAResourceName);
                GlobalVars.mbSession = (MessageBasedSession)ResourceManager.GetLocalManager().Open(GlobalVars.VISAResourceName);
                Console.WriteLine("will open {0}",GlobalVars.VISAResourceName);
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
                GlobalVars.mbSession.Timeout = GlobalVars.VISASessionTimeout; //设置超时
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
            }
            //Console.ReadLine();
            return 0;
        }
    }
    public static class GlobalVars
    {
        //使用GPIB
        public static bool VISA_CLI_Option_UseGPIB = false;
        public static short VISA_CLI_Option_GPIB_BoardIndex = -1;            // Board index
        public static short VISA_CLI_Option_GPIB_PrimaryAddress = -1;        // Primary address
        public static short VISA_CLI_Option_GPIB_SecondaryAddress = -1;      // Secondary address


        public static String VISA_CLI_Option_CommandString = null;         //command to send
        public static bool VISA_CLI_Option_isQueryCommand = false;           // is a query command
        public static bool VISA_CLI_Option_PrintDebugMessage = false; //debug switch
        public static String VISA_CLI_Option_FileName = null;           //when file name specified, save binary response to file
        public static bool VISA_CLI_Option_OverwriteFile = false;     //if file exist ,overwrite it
        public static int VISA_CLI_Option_ReadBackNbytes = 1024;             //read specified length of response,default is 1024 bytes
        public static int VISA_CLI_Option_SkipFirstNbytes = 0;     //for some system(DCA86100,AQ6370,etc.), transfered data via GPIB contain extra bytes,user can skip them

        public static   MessageBasedSession mbSession;
        public static String VISAResourceName = null;
        public static int    VISASessionTimeout= 10000; //10000ms
    }
}
