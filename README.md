# CrossHaptics: Enabling Real-time Multi-device Haptic Feedback for VR Games via Controller Vibration Pattern Analysis
![Teaser Figure](https://i.imgur.com/T3jK4ZA.png)




## Overview
We presented CrossHaptics, which explores how controller vibration patterns designed by game developers can be used to enable support for additional haptic devices, proposed a framework that automatically detects asymmetrical and symmetrical vibration signals to infer localized vs. full-body haptic events in real-time and provided a plugin architecture to simplify adding support for additional haptic devices.

We encapsulate CrossHaptics into two components, CrossHaptics Launcher and Haptic Device Plugin, and implement both of the sample code with C# .NET framework.

<img src="https://imgur.com/a18LCzT.png" width="700">

## CrossHaptics Launcher
In this components, we listen to the VR controllers signal using OpenVR api (eg. Oculus, Vive Pro), and classify the signal into symmetrical and asymmetrical signals according to the characteristics and send 

## Haptic Device Plugin
We provide a sample device plugin using bhaptics TactSuit and Tactosy for Arms for user to understand how it works.

<br />
<br />
<br />



# How To Use (for non-developer)
For those non-developer, we've implement a sample device plugin with bHaptics tacsuit and tactal. User may follow the instructions to integrate the haptic feedback devices with commercial games easily! 
1. clone the project from github
    ```
    git clone https://github.com/goolu0623/CrossHaptics.git
    ```
1. [install redis](https://redis.io/docs/getting-started/installation/) and start the server.

    <img src="https://i.imgur.com/rHxfc72.png" width="700">

1. Open CrossHaptics.sln inside CrossHapticStarter with Visual Studio version <span style="color:red">4.6.1</span> (need to update all the related packages to the corresponding version if not using version 4.6.1)
    1. use Visual Studio Installer to get the correct version as the following steps
    <img src="https://imgur.com/oBUyHQC.png" width="700">
    <img src="https://imgur.com/3sF9QJS.png" width="700">

1. Restore NuGet Packages (if needed)

    <img src="https://i.imgur.com/xvu1SBr.png" width="700">

1. Rebuild solution (Rebuild All)

    <img src="https://i.imgur.com/eL7tjVz.png" width="700">
1. Connect all your devices
    1. VR devices
    1. Hatpic devices(sample)


1. Launch the scripts and repeat the same steps with "HapticDeviceSamplePlugin"
    1. You can saw every components and script window if you launch with <span style="color:red">DEBUG</span> mode
    1. You may <span style="color:red">hide</span> all the information (if you don't care) with <span style="color:red">RELEASE</span> mode
1. Enjoy the game!

# How To Implement Your Own Haptic Devices Plugin and Listen To Vibration Signal Events. 
For those haptic-developer, who wants to integrate CrossHaptics with there own devices, may follow the instructions below to implement you own haptic devices plugin. In the following sections, we provide c#(Unity) and c#(.Net framework) sample instructions. However, it's language-indepent structure, which means you may listen to the message broker(Redis) via any type of programming language that supports, theoretically. 
## redis installation 
[redis installation instructions](https://redis.io/docs/getting-started/installation/)
## follow the non-developer section and execute the CrossHaptics Launcher

# For C# Developer
1. Create your own haptic device solution with the corresponding version(using )
1. add project 
    1. RedisEndpoint_donetFramework if you are using .NET framework
    1. RedisEndpoint_donetCore if you are using .NET core

    <img src="https://i.imgur.com/safzr2c.png" width="700">
1. add RedisEndpoint as reference

    <img src="https://i.imgur.com/vhFqTvv.png" width="700">
1. install StackExchange.REDIS in NUGET manage store (project sample using Version 2.1.58)
    <img src="https://imgur.com/OGJqKxN.png" width="350">
    <img src="https://i.imgur.com/tb86GxK.png" width="700">
1. listen to the channel on local host 6379/6380(or whatever the redis port you select) as the example below
    1. channel name = "symmetrical_event"
    2. channel name = "nonsymmetrical_event"
1. and you'll recieved the event msg in the following format whenever the controllers accept haptic events in real-time


1. sample haptic device implementation useing bHaptics Devices.
```
using System;
using System.IO;
using bHapticsLib;
using System.Threading;
using RedisEndpoint;


namespace SampleHapticDevicePlugin
{
    public class Program
    {
        private ScaleOption scaleOption = new ScaleOption();
        private HapticPattern hapticFeedback;
        
        private void Main(){
            // bHaptics device pattern initialization
            string hapticFeedbackPath = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "hapticFeedbackPattern.tact");
            hapticFeedback = HapticPattern.LoadFromFile("testfeedback", hapticFeedbackPath);            
            bHapticsManager.Connect("bHapticsLib", "TestApplication", maxRetries: 0);
            Thread.Sleep(1000);

            // bHaptics device connection initialization 
            bHapticsConnection Connection = new bHapticsConnection("bHapticsLib", "AdditionalConnection", maxRetries: 0);
            Connection.BeginInit();
            Console.WriteLine(Connection.Status);

            // Subscribe redics channel
            Subscriber exampleSubscriber = new Subscriber("localhost", 6379);
            string subscribeChannelName = "nonsymmetrical_event"; // here subscribe to symmetrical_event or nonsymmectrical_event
            exampleSubscriber.SubscribeTo(subscribeChannelName);
            exampleSubscriber.msgQueue.OnMessage(msg => msgHandler(msg.Message));

            Console.Read();
            return;
        }


        
        // whenever this component received a msg from the corrseponding channel
        // this function will be run once to handle the message 
        void msgHandler(string msg){
            // msg example as below
            // 06/04 21:05:56.644 RightController Output Vibration Amp 0.1600 Freq 1.0000 Duration 0.0000
            // seperate the information you need
            string [] eventMessage = msg.Split(' ');
            string date = eventMessage[0];
            string time = eventMessage[1];
            string sourceTypeName = eventMessage[2]; // RightController / LeftController
            // according to OpenVR API: https://valvesoftware.github.io/steamvr_unity_plugin/api/Valve.VR.ISteamVR_Action_Vibration.html
            // |Name        |Description                                                                                |
            // |------------|-------------------------------------------------------------------------------------------|
            // |duration    |How long the haptic action should last (in seconds)                                        |
            // |frequency   |How often the haptic motor should bounce (0 - 320 in hz. The lower end being more useful)  |
            // |amplitude   |How intense the haptic action should be (0 - 1)                                            |
            string amp = eventMessage[6]; 
            string freq = eventMessage[8];
            string dur = eventMessage[10];
            
            // play aroudn with your device here
            scaleOption.Duration = Convert.ToSingle(dur);
            scaleOption.Intensity = Convert.ToSingle(amp);
            hapticFeedback.Play(scaleOption);
        }
    }
}
```
# For Unity Developer
1. [install NugetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) in your Unity project
1. restart your Unity project
1. install StackExchange.Redis in Unity with NugetForUnity

    <img src="https://i.imgur.com/agVdmL6.png" width="700">
1. Make sure if the version of StackExchange you install in Unity Porject is the same as the version of CrossHapticsStarter(check in NuGetManager)
    
    <img src="https://i.imgur.com/MGaMKQd.png" width="700">
1. if Not, update stackExchange and relevent package to the same version.
1. Add class RedisEndpoint to Unity script for ease of use (you may skip this step if you want to setup your own Redis connection)
    ```
    using StackExchange.Redis;
    public class RedisEndpoint {

        protected static ConfigurationOptions redisConfiguration;
        protected static ConnectionMultiplexer multiplexer;
        protected ISubscriber connection;

        public RedisEndpoint(string url, ushort port) {
            if (multiplexer == null) {
                string host = url + ":" + port.ToString();
                string redisConfiguration2 = host+"Password=password,Ssl=false,ConnectTimeout=6000,SyncTimeout=6000,AllowAdmin=true";
                multiplexer = ConnectionMultiplexer.Connect(redisConfiguration2);
            }
        }

        static int Main() {
            return 0;
        }
    }

    public class Publisher : RedisEndpoint {
        public Publisher(string url, ushort port) : base(url, port) {
            connection = multiplexer.GetSubscriber();
        }
        public void Publish(string channelName, string msg) {
            connection.PublishAsync(channelName, msg, flags: CommandFlags.FireAndForget);
        }
    }

    public class Subscriber : RedisEndpoint {
        public ChannelMessageQueue msgQueue;
        public Subscriber(string url, ushort port) : base(url, port) {
            connection = multiplexer.GetSubscriber();
        }
        public void SubscribeTo(string channelName) {
            msgQueue = connection.Subscribe(channelName);
        }
    }
    ```
1. Listen to the events as the following sample usage to connect with your device
    ```
    using UnityEngine;

    public class redis_code_sample : MonoBehaviour
    {
        Subscriber subscriber;
        string subscribeChannelName = "nonsymmetrical_event"; // here subscribe to symmetrical_event or nonsymmectrical_event

        string url = "localhost";
        ushort port = 6379;
        void Start()
        {
            subscriber = new Subscriber(url, port);
            subscriber.SubscribeTo(subscribeChannelName);
            subscriber.msgQueue.OnMessage(msg => OnMsgEnter(msg.Message));
        }

        // Update is called once per frame
        void Update()
        {
        }

        
        // whenever this component received a msg from the corrseponding channel
        // this function will be run once to handle the message 
        void OnMsgEnter(string message) {
            

            // msg example as below
            // 06/04 21:05:56.644 RightController Output Vibration Amp 0.1600 Freq 1.0000 Duration 0.0000
            // seperate the information you need
            string [] eventMessage = msg.Split(' ');
            string date = eventMessage[0];
            string time = eventMessage[1];
            string sourceTypeName = eventMessage[2]; // RightController / LeftController
            // according to OpenVR API: https://valvesoftware.github.io/steamvr_unity_plugin/api/Valve.VR.ISteamVR_Action_Vibration.html
            // |Name        |Description                                                                                |
            // |------------|-------------------------------------------------------------------------------------------|
            // |duration    |How long the haptic action should last (in seconds)                                        |
            // |frequency   |How often the haptic motor should bounce (0 - 320 in hz. The lower end being more useful)  |
            // |amplitude   |How intense the haptic action should be (0 - 1)                                            |
            string amp = eventMessage[6]; 
            string freq = eventMessage[8];
            string dur = eventMessage[10];
            
            // play around with your device here
            Debug.Log("device actuated");
        }
    }

    ```
