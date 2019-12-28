using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevOps_Migration_Helper
{
    public static class  Extensions
    {
        public static void Writeline(this StreamWriter sw, string text)
        {
            sw.WriteLine(text);
            Console.WriteLine(text);
        }
    }
}
