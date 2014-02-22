using System;
using System.IO.Pipes;

namespace MonoPipeTest
{
    class Program
    {
        static void Main(string[] args)
        {
            NamedPipeClientStream pipe = new NamedPipeClientStream("MaxUnityBridge");
            pipe.Connect();

            do
            {
                byte[] data1 = new byte[] { 1, 2, 3, 4 };

                pipe.Write(data1, 0, data1.Length);
                pipe.Flush();
                pipe.WaitForPipeDrain();

                byte[] datarx1 = new byte[4];
                pipe.Read(datarx1, 0, 4);

            } while (true);
        }
    }
}
