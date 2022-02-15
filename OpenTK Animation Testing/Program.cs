using System;
using System.Threading;
using BOLL7708;
using Valve.VR;

namespace OpenTK_Animation_Testing
{
    class Program
    {
        static void Main(string[] args)
        {
            EasyOpenVRSingleton vrSingleton = EasyOpenVRSingleton.Instance;
            
            using (App app = new App(300, 300, "Animation Testing"))
            {
                Console.Write("Connecting to SteamVR.");
                while (!vrSingleton.Init())
                {
                    Console.Write(".");
                    Thread.Sleep(500);
                }
                Console.WriteLine();

                vrSingleton.SetApplicationType(EVRApplicationType.VRApplication_Overlay);

                app.SpriteSheet("sheet1.png", 233, 233, 0.1);

                app.Run();
            }
        }
    }
}