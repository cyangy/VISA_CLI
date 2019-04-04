using System;
using System.IO;

//https://stackoverflow.com/questions/616584/how-do-i-get-the-name-of-the-current-executable-in-c
//https://stackoverflow.com/questions/11512821/how-to-stop-c-sharp-console-applications-from-closing-automatically

namespace VISA_CLI
{
    class myHelp
    {
        public static Int32 Show_gpib_help()
        {
            Console.WriteLine("GPIB client command options: ");
            Console.WriteLine("    -gpib   <N>         board index(GPIB board index)");
            Console.WriteLine("    -pad    <N>         primary address");
            Console.WriteLine("    -ls                 list all instruments on a board and quit");
            Console.WriteLine("    -debug              prints debug messages");
            Console.WriteLine("    -cmdstr <strings>   commands to send to the device");
            Console.WriteLine("    -query              the command is a query command ");
            Console.WriteLine("    -save2file          save the response binary data to specify file | tail -c+9 1.jpg >2.JPG");
            Console.WriteLine("         -skip          skip first n bytes of received file");
            Console.WriteLine("    -help/-?            show this information");
            Console.WriteLine("Typical usage (Agilent 34401A on GPIB board index 0  with primary address 22 and secondary address 0 ) is");
            Console.WriteLine("    Just send Command:");
            Console.WriteLine("                 {0}  -gpib 0 -pad 22 -cmdstr \"CONFigure:CONTinuity\" ", System.AppDomain.CurrentDomain.FriendlyName);
            Console.WriteLine("    or send Command then read response immediately: ");
            Console.WriteLine("                 {0}  -gpib 0 -pad 22 -cmdstr \"READ?\" -query ", System.AppDomain.CurrentDomain.FriendlyName);
            Console.WriteLine("    or combine format ");
            Console.WriteLine("                 {0}  -gpib 0 -pad 22  -query -cmdstr \"CONFigure:CONTinuity ; READ?\" ", System.AppDomain.CurrentDomain.FriendlyName);
            Console.WriteLine("    or communicate with device Interactively:");
            Console.WriteLine("                 {0}  -gpib 0 -pad 22", System.AppDomain.CurrentDomain.FriendlyName);
            Console.WriteLine("    http://mikrosys.prz.edu.pl/KeySight/34410A_Quick_Reference.pdf ");
            Console.WriteLine("    http://ecee.colorado.edu/~mathys/ecen1400/pdf/references/HP34401A_BenchtopMultimeter.pdf ");
            Console.WriteLine("!Note: if -cmdstr not specified ,Press Enter (empty input) to read device response");
            //usleep(2000);
            return 0;
        }
    }
}