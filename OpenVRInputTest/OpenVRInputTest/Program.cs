using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Globalization;
using System.Xml.Serialization;
using Valve.VR;
using RedisEndpoint;
using static OpenVRInputTest.VREventCallback;
//Sources: https://github.com/BOLL7708/OpenVRInputTest
namespace OpenVRInputTest
{
    public class ProcConfig
    {
        public int DataUpdateRatePerSec = 333;
    }
    class Program {
        public static Publisher publisher = new Publisher("localhost", 6379);
        public static string outletChannelName;
        public static float DataFrameRate = 90f;
        static ulong mActionSetHandle;
        static VRActiveActionSet_t[] mActionSetArray;
        static Controller rightController, leftController;
        const string ConfigFileName = "Configs.xml";
        static ProcConfig config;


        public static Queue<Tuple<DateTime, string, string>> output_data = new Queue<Tuple<DateTime, string, string>>();
        public static DateTime start_time = new DateTime(DateTime.MinValue.Ticks);
        public static Stopwatch sw = new Stopwatch();
        public static bool Is_Enabled = true;


        // # items are referencing this list of actions: https://github.com/ValveSoftware/openvr/wiki/SteamVR-Input#getting-started
        static void Main(string[] args)
        {
            var _XmlSerializer = new XmlSerializer(typeof(ProcConfig));
            if (!File.Exists(ConfigFileName))
                using (var stream = File.Create(ConfigFileName))
                    _XmlSerializer.Serialize(stream, new ProcConfig());
            config = (ProcConfig)_XmlSerializer.Deserialize(File.OpenRead(ConfigFileName));


            outletChannelName = args[0];
            // Initializing connection to OpenVR
            var error = EVRInitError.None;
            OpenVR.Init(ref error, EVRApplicationType.VRApplication_Background); // Had this as overlay before to get it working, but changing it back is now fine?
            start_time = DateTime.Now;

            var workerThread = new Thread(Worker);
            workerThread.Priority = ThreadPriority.Highest;

            var writerThread = new Thread(Writer);
            writerThread.Priority = ThreadPriority.Normal;


            if (error != EVRInitError.None)
                Utils.PrintError($"OpenVR initialization errored: {Enum.GetName(typeof(EVRInitError), error)}");
            else
            {
                Utils.PrintInfo("OpenVR initialized successfully.");

                // Load app manifest, I think this is needed for the application to show up in the input bindings at all
                Utils.PrintVerbose("Loading app.vrmanifest");
                var appError = OpenVR.Applications.AddApplicationManifest(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFullPath("app.vrmanifest")), false); if (appError != EVRApplicationError.None)
                    Utils.PrintError($"Failed to load Application Manifest: {Enum.GetName(typeof(EVRApplicationError), appError)}");
                else 
                    Utils.PrintInfo("Application manifest loaded successfully.");

                // #3 Load action manifest
                Utils.PrintVerbose("Loading actions.json");
                var ioErr = OpenVR.Input.SetActionManifestPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFullPath("actions.json")));
                if (ioErr != EVRInputError.None) 
                    Utils.PrintError($"Failed to load Action Manifest: {Enum.GetName(typeof(EVRInputError), ioErr)}");
                else 
                    Utils.PrintInfo("Action Manifest loaded successfully.");

                // #4 Get action handles
                Utils.PrintVerbose("Getting action handles");
                rightController =
                    new Controller(DeviceType.RightController, "RightController", "/user/hand/right", "/actions/default/in/right_", "/actions/default/out/haptic_right")
                    .AttachNewEvent(new Button_B_Event())
                    .AttachNewEvent(new Button_A_Event())
                    .AttachNewEvent(new Button_Trigger_Event())
                    .AttachNewEvent(new Button_TriggerVector1_Event())
                    .AttachNewEvent(new Button_Touchpad_Event())
                    .AttachNewEvent(new Button_TouchpadVector2_Event())
                    .AttachNewEvent(new Button_System_Event())
                    .AttachNewEvent(new Button_ThumbStick_Event())
                    .AttachNewEvent(new Button_ThumbStickVector2_Event())
                    .AttachNewEvent(new Button_Grip_Event())
                    .AttachNewEvent(new Button_GripVector1_Event());

                leftController =
                    new Controller(DeviceType.LeftController, "LeftController", "/user/hand/left", "/actions/default/in/left_", "/actions/default/out/haptic_left")
                    .AttachNewEvent(new Button_B_Event())
                    .AttachNewEvent(new Button_A_Event())
                    .AttachNewEvent(new Button_Trigger_Event())
                    .AttachNewEvent(new Button_TriggerVector1_Event())
                    .AttachNewEvent(new Button_Touchpad_Event())
                    .AttachNewEvent(new Button_TouchpadVector2_Event())
                    .AttachNewEvent(new Button_System_Event())
                    .AttachNewEvent(new Button_ThumbStick_Event())
                    .AttachNewEvent(new Button_ThumbStickVector2_Event())
                    .AttachNewEvent(new Button_Grip_Event())
                    .AttachNewEvent(new Button_GripVector1_Event());


                // #5 Get action set handle
                Utils.PrintVerbose("Getting action set handle");
                var errorAS = OpenVR.Input.GetActionSetHandle("/actions/default", ref mActionSetHandle);
                if (errorAS != EVRInputError.None) 
                    Utils.PrintError($"GetActionSetHandle Error: {Enum.GetName(typeof(EVRInputError), errorAS)}");
                Utils.PrintDebug($"Action Set Handle default: {mActionSetHandle}");

                // Starting worker
                Utils.PrintDebug("Starting worker thread.");
                if (!workerThread.IsAlive)
                    workerThread.Start();
                else
                    Utils.PrintError("Could not start worker thread.");
                if (!writerThread.IsAlive)
                    writerThread.Start();
                else
                    Utils.PrintError("Could not start writer thread.");
            }


            Console.Read();
            workerThread.Abort();
            writerThread.Abort();
            OpenVR.Shutdown();
            return;

            //########################################## console form with disable key##############################
            //Utils.PrintWarning("Press key V to switch vibration mode");
            //Utils.PrintWarning("Press enter to end the scripts");


            //while (true) {
            //    ConsoleKeyInfo key = Console.ReadKey(true);
            //    switch (key.Key) {
            //        case ConsoleKey.V:
            //            if (Is_Enabled == true) {
            //                Utils.PrintWarning("Disable Vibration");
            //                Is_Enabled = false;
            //            }
            //            else {
            //                Utils.PrintWarning("Enable Vibration");
            //                Is_Enabled = true;
            //            }
            //            break;
            //        case ConsoleKey.Enter:
            //            workerThread.Abort();
            //            writerThread.Abort();
            //            OpenVR.Shutdown();
            //            return;
            //    }
            //}
        }

        private static void Worker()
        {

            sw.Start();
            Thread.CurrentThread.IsBackground = true;
            int RefreshRate = (int)(1000 / DataFrameRate);
            ulong LeftVibration = 0, RightVibration = 0;
            OpenVR.Input.GetActionHandle("/actions/default/out/haptic_left", ref LeftVibration); 
            OpenVR.Input.GetActionHandle("/actions/default/out/haptic_right", ref RightVibration);
            // #6 Update action set
            if (mActionSetArray == null)
            {
                var actionSet = new VRActiveActionSet_t
                {
                    ulActionSet = mActionSetHandle,
                    ulRestrictedToDevice = OpenVR.k_ulInvalidActionSetHandle,
                    nPriority = 0
                };
                mActionSetArray = new VRActiveActionSet_t[] { actionSet };
            }
            while (true) {
                if (!Is_Enabled) {
                    // #6 Update action set
                    if (mActionSetArray == null) {
                        var actionSet = new VRActiveActionSet_t {
                            ulActionSet = mActionSetHandle,
                            ulRestrictedToDevice = OpenVR.k_ulInvalidActionSetHandle,
                            nPriority = 0
                        };
                        mActionSetArray = new VRActiveActionSet_t[] { actionSet };

                    }
                    // 不停的送amp=0, dur=0的訊號
                    var errorUAS = OpenVR.Input.UpdateActionState(mActionSetArray, (uint)Marshal.SizeOf(typeof(VRActiveActionSet_t)));
                    if (errorUAS != EVRInputError.None)
                        Utils.PrintError($"UpdateActionState Error: {Enum.GetName(typeof(EVRInputError), errorUAS)}");

                    var errorLeftVibration = OpenVR.Input.TriggerHapticVibrationAction(leftController.VibrationHandle, 0, 1000, 0, 0, OpenVR.k_ulInvalidInputValueHandle);
                    if (errorLeftVibration != EVRInputError.None)
                        Utils.PrintError($"Left Vibration Error: {Enum.GetName(typeof(EVRInputError), errorLeftVibration)}");

                    var errorRightVibration = OpenVR.Input.TriggerHapticVibrationAction(rightController.VibrationHandle, 0, 1000, 0, 0, OpenVR.k_ulInvalidInputValueHandle);
                    if (errorRightVibration != EVRInputError.None)
                        Utils.PrintError($"Right Vibration Error: {Enum.GetName(typeof(EVRInputError), errorRightVibration)}");

                }
                else {
                    // HMD跟controller的orientation跟position
                    if (sw.ElapsedMilliseconds > RefreshRate) {
                        sw.Restart();
                        TrackableDeviceInfo.UpdateTrackableDevicePosition(ref output_data);
                    }

                    // Getting events
                    var vrEvents = new List<VREvent_t>();
                    var vrEvent = new VREvent_t();

                    try {
                        while (OpenVR.System.PollNextEvent(ref vrEvent, Utils.SizeOf(vrEvent))) {
                            vrEvents.Add(vrEvent);
                        }
                    }
                    catch (Exception e) {
                        Utils.PrintWarning($"Could not get events: {e.Message}");
                    }

                    // Printing events
                    foreach (VREvent_t e in vrEvents) {

                        var pid = e.data.process.pid;
                        if (e.eventType == (uint)EVREventType.VREvent_Input_HapticVibration) {
                            ETrackedControllerRole DeviceType = OpenVR.System.GetControllerRoleForTrackedDeviceIndex(e.data.process.pid);
                            if (DeviceType != ETrackedControllerRole.LeftHand && DeviceType != ETrackedControllerRole.RightHand)
                                continue;
                            NewVibrationEvent(DeviceType, e.data.hapticVibration, ref output_data);
                            //leftController.DisableVibration(DeviceType, e.data.hapticVibration);
                            //rightController.DisableVibration(DeviceType, e.data.hapticVibration);
                        }
#if DEBUG
                        if ((EVREventType)vrEvent.eventType != EVREventType.VREvent_None) {
                            var name = Enum.GetName(typeof(EVREventType), e.eventType);
                            var message = $"[{pid}] {name}";
                            if (pid == 0)
                                Utils.PrintVerbose(message);
                            else if (name == null)
                                Utils.PrintVerbose(message);
                            else if (name.ToLower().Contains("fail"))
                                Utils.PrintWarning(message);
                            else if (name.ToLower().Contains("error"))
                                Utils.PrintError(message);
                            else if (name.ToLower().Contains("success"))
                                Utils.PrintInfo(message);
                            else
                                Utils.Print(message);
                        }
                        Utils.PrintWarning($"each");
#endif
                    }

                    // #6 Update action set
                    if (mActionSetArray == null) {
                        var actionSet = new VRActiveActionSet_t {
                            ulActionSet = mActionSetHandle,
                            ulRestrictedToDevice = OpenVR.k_ulInvalidActionSetHandle,
                            nPriority = 0
                        };
                        mActionSetArray = new VRActiveActionSet_t[] { actionSet };
                    }

                    var errorUAS = OpenVR.Input.UpdateActionState(mActionSetArray, (uint)Marshal.SizeOf(typeof(VRActiveActionSet_t)));
                    if (errorUAS != EVRInputError.None) Utils.PrintError($"UpdateActionState Error: {Enum.GetName(typeof(EVRInputError), errorUAS)}");


                    // #7 Load input action data
                    leftController.UpdateAllState(ref output_data);
                    rightController.UpdateAllState(ref output_data);
                    // Restrict rate
                }
            }
        }
        private static void Writer() {
            Thread.CurrentThread.IsBackground = true;
            while (true) {
                if (output_data.Count != 0) {
                    Tuple<DateTime, string, string> data;
                    lock (output_data) {
                        data = output_data.Dequeue();
                    }
                    // ========時間格式==========
                    CultureInfo ci = new CultureInfo("en-US");
                    string EventTimeForAnalyze = data.Item1.ToString("MM/dd HH:mm:ss.fff", ci); // 分析資料用 所以把fff(毫秒特別拉出來)
                    string EventTimeForCrossHaptics = data.Item1.ToString(ci); // 玩遊戲用的話精度不用那麼高 用標準格式比較好處理
                    //LogWriterTest.LogWriter.LogWrite(EventTimeForAnalyze + temp.Item2, "console.txt"); // 如果要寫進log做資料分析
                    // ========時間格式==========

                    // ========redis publish==========
                    if (Program.outletChannelName != null) {
                        if (data.Item3 == "Output") { // controller的資料
                            Program.publisher.Publish(Program.outletChannelName, $"{EventTimeForAnalyze} {data.Item2}"); // pub到redis 給其他script吃
#if DEBUG
                            Utils.PrintDebug(EventTimeForAnalyze + data.Item2);
#endif
                        }
                    }
                    // ========redis publish==========
                }
            }
        }
    }
}

