using System;
using System.Collections.Generic;

namespace Assets.Scripts
{

    public partial class Student
    {
        public int P1 { get; set; }
        public string P2 { get; set; }
        public long P3 { get; set; }
        public byte P4 { get; set; }
        public DateTime P5 { get; set; }
        public double P6 { get; set; }
        public byte[] P7 { get; set; }
        public string[] P8 { get; set; }

        public List<int> List1 { get; set; }
        public List<string> List2 { get; set; }
        public List<byte[]> List3 { get; set; }

        public Dictionary<int, int> Dic1 { get; set; }
        public Dictionary<int, string> Dic2 { get; set; }
        public Dictionary<string, string> Dic3 { get; set; }
        public Dictionary<int, Arg> Dic4 { get; set; }
    }


    public partial class Arg
    {
        public Arg()
        {
        }

        public Arg(int myProperty)
        {
            this.MyProperty = myProperty;
        }

        public int MyProperty { get; set; }
    }
}
