using System;
using System.IO;
using System.Threading;
using bHapticsLib;
using RedisEndpoint;

namespace CrossHapticsDeviceSamplePlugin {

    class Program {
        private static ScaleOption scaleOption = new ScaleOption();

        private static HapticPattern vestHapticFeedback;
        private static HapticPattern leftArmHapticFeedback;
        private static HapticPattern rightArmHapticFeedback;
        static readonly string nonSymmetricalChannelName = "nonsymmetrical_event"; // ChannelName use to subscribe to symmetrical_event or nonsymmectrical_event
        static readonly string symmetricalChannelName = "symmetrical_event";
        static void Main() {
            // bHaptics device pattern initialization
            string vestHapticFeedbackPath = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "vestHapticFeedback.tact");
            string leftArmHapticFeedbackPath = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "leftArmHapticFeedback.tact");
            string rightArmHapticFeedbackPath = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "rightArmHapticFeedback.tact");
            vestHapticFeedback = HapticPattern.LoadFromFile("vestHapticFeedback", vestHapticFeedbackPath);
            leftArmHapticFeedback = HapticPattern.LoadFromFile("leftArmHapticFeedback", leftArmHapticFeedbackPath);
            rightArmHapticFeedback = HapticPattern.LoadFromFile("rightArmHapticFeedback", rightArmHapticFeedbackPath);
            bHapticsManager.Connect("bHapticsLib", "TestApplication", maxRetries: 0);
            Thread.Sleep(1000);

            // bHaptics device connection initialization 
            bHapticsConnection Connection = new bHapticsConnection("bHapticsLib", "AdditionalConnection", maxRetries: 0);
            Connection.BeginInit();
            Console.WriteLine(Connection.Status);

            // Subscribe redis channel
            Subscriber symmetricalEventSubscriber = new Subscriber("localhost", 6379);
            symmetricalEventSubscriber.SubscribeTo(symmetricalChannelName);
            symmetricalEventSubscriber.msgQueue.OnMessage(msg => symmetricalMsgHandler(msg.Message));

            Subscriber nonSymmetricalEventSubscriber = new Subscriber("localhost", 6379);
            nonSymmetricalEventSubscriber.SubscribeTo(nonSymmetricalChannelName);
            nonSymmetricalEventSubscriber.msgQueue.OnMessage(msg => nonSymmetricalMsgHandler(msg.Message));
            Console.Read();
            return;
        }

        static void symmetricalMsgHandler(string msg) {
            Console.WriteLine(msg);
            // msg example as below
            // 06/04 21:05:56.644 RightController Output Vibration Amp 0.1600 Freq 1.0000 Duration 0.0000
            // seperate the information you need
            string[] eventMessage = msg.Split(' ');
            string amp = eventMessage[6];
            string dur = eventMessage[10];

            // play aroudn with your device here
            scaleOption.Duration = Convert.ToSingle(dur);
            scaleOption.Intensity = Convert.ToSingle(amp);

            scaleOption.Duration = Math.Max(Convert.ToSingle(dur), 0.11f);
            scaleOption.Intensity = Math.Min(Convert.ToSingle(amp), 1f);
            if (scaleOption.Intensity < 0.001f)
                return;
            vestHapticFeedback.Play(scaleOption);
        }
        static void nonSymmetricalMsgHandler(string msg) {
            Console.WriteLine(msg);
            // msg example as below
            // 06/04 21:05:56.644 RightController Output Vibration Amp 0.1600 Freq 1.0000 Duration 0.0000
            // seperate the information you need
            string[] eventMessage = msg.Split(' ');
            string sourceTypeName = eventMessage[2];
            string amp = eventMessage[6];
            string dur = eventMessage[10];

            // play aroudn with your device here
            scaleOption.Duration = Math.Max(Convert.ToSingle(dur),0.11f);
            scaleOption.Intensity = Math.Min(Convert.ToSingle(amp),1f);
            if (scaleOption.Intensity<0.8f) 
                return;
            if (sourceTypeName == "RightController") {
                rightArmHapticFeedback.Play(scaleOption);
            }
            else if(sourceTypeName=="LeftController") {
                leftArmHapticFeedback.Play(scaleOption);
            }
        }
    }
}
