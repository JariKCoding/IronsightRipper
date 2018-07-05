/*
To extract sounds from fsb files download the program here: http://aluigi.altervista.org/search.php?src=fsbext
*/
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronSightRipper
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] files = args.Where(x => Path.GetExtension(x) == ".wpg" && File.Exists(x)).ToArray();

            Console.WriteLine("");
            Console.WriteLine("IronSight Model,Texture & Audio Ripper by JerriGaming (With a lot of help from Scobalula & DTZxPorter)");
            Console.WriteLine("To export sounds from fsb files use the following program: http://aluigi.altervista.org/papers.htm#fsbext");
            Console.WriteLine("");

            if (files.Length < 1)
            {
                Console.WriteLine("No valid WPG Files given.");
            }

            foreach (var file in files)
            {
                if (!ScobUtil.CanAccessFile(file))
                {
                    Console.WriteLine(string.Format("File {0} is in-use or permissions were denied", Path.GetFileName(file)));
                    continue;
                }

                WPGFile wpgFile = new WPGFile();
                try
                {
                    wpgFile.Decode(file);
                }
                catch (Exception e)
                {
                    Console.Write(e);
                }
            }

            Console.WriteLine("");
            Console.WriteLine(string.Format("{0} WPG File(s) Processed.", files.Length));
            Console.WriteLine("");
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }
    }
}
