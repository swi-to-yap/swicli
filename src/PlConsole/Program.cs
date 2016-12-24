using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using SbsSW.SwiPlCs;

namespace PlConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            libpl.PL_initialise(args.Length, args);
        }
    }
}
