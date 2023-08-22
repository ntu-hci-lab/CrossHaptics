using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using RedisEndpoint;

namespace VibrationSignalClassifier {


    class Program {
        private static Subscriber VibrationSignalSubscriber = new Subscriber("localhost", 6379);
        private static string VibrationSignalChannelName;
        static void Main(string[] args) {
            VibrationSignalChannelName = args[0];
            Console.WriteLine("start sub " + VibrationSignalChannelName);
            VibrationSignalSubscriber.SubscribeTo(VibrationSignalChannelName);
            VibrationSignalSubscriber.msgQueue.OnMessage(msg => VibrationSignalClassifier.msg_Handler(msg.Message));

            Thread classifierThread = new Thread(VibrationSignalClassifier.classifier_thread);

            classifierThread.Start();
            Console.WriteLine("normal start");

            _ = Console.ReadKey();
            return;
        }


    }

    static class VibrationSignalClassifier {
        private readonly static int k_DELAY_TIME_WINDOW = 80; // Delay time to detect symmetric signals
        private static LinkedList<HapticEvent> eventList = new LinkedList<HapticEvent>();
        private static Publisher nonsymmetrical_event_publisher = new Publisher("localhost", 6379);
        private static Publisher symmetrical_event_publisher = new Publisher("localhost", 6379);
        private static bool IsSymmetric() {
            // 檢查第一個有沒有跟人對稱
            lock (eventList) {
                var iterate_node = eventList.First;
                var nowEvent = eventList.First.Value;
                while (iterate_node.Next != null) {
                    var next_node = iterate_node.Next;
                    var nextEvent = next_node.Value;
                    if (nextEvent.get_duration == nowEvent.get_duration
                        && nextEvent.get_amplitude == nowEvent.get_amplitude
                        && nextEvent.get_freqeuncy == nowEvent.get_freqeuncy
                        && nextEvent.get_source_type_name != nowEvent.get_source_type_name) {
                        // 拔掉對稱
                        eventList.Remove(next_node);
                        return true;

                    }
                    iterate_node = next_node;
                }
            }

            return false;
        }
        public static void msg_Handler(string msg) {
            // 解析msg
            // msg sample:
            // 06/04 21:05:56.644 RightController Output Vibration Amp 0.1600 Freq 1.0000 Duration 0.0000
            string[] haptic_event_info_lists = msg.Split(' ');

#if DEBUG
            Console.WriteLine(msg);
            //Console.WriteLine("EventTime " + haptic_event_info_lists[1]);
            //Console.WriteLine("SourceTypeName " + haptic_event_info_lists[2]);
            //Console.WriteLine("EventType " + haptic_event_info_lists[3]);
            //Console.WriteLine("EventName " + haptic_event_info_lists[4]);
            //Console.WriteLine("Amp " + haptic_event_info_lists[6]);
            //Console.WriteLine("Freq " + haptic_event_info_lists[8]);
            //Console.WriteLine("Dur " + haptic_event_info_lists[10]);
#endif
            HapticEvent temp = new HapticEvent(
                EventTime: haptic_event_info_lists[1],
                SourceTypeName: haptic_event_info_lists[2],
                EventType: haptic_event_info_lists[3],
                EventName: haptic_event_info_lists[4],
                Amp: haptic_event_info_lists[6],
                Freq: haptic_event_info_lists[8],
                Dur: haptic_event_info_lists[10],
                EnListTime: DateTime.Now,
                msg: msg
            ); 
            // add into eventList
            lock (eventList) {
                eventList.AddLast(temp);
            }

        }

/////////////////////////////////////////////////////////
        public static void classifier_thread() {
            HapticEvent nowEvent; // 後面會一直用到 在這邊宣告應該比較不浪費宣告又release的時間
            while (true) {
                // 看list裡面的時間過期沒
                while (eventList.Count() != 0) {
                    DateTime nowTime = DateTime.Now;
                    lock (eventList) {
                        nowEvent = eventList.First();
                    }
                    // 給個time window XX ms 看這個區間有沒有產生對稱震動 這個區間太大的話會很誤觸到雙手對稱
                    if ((nowTime - nowEvent.get_enter_list_daytime).TotalMilliseconds < k_DELAY_TIME_WINDOW)
                        break;
                    if (IsSymmetric()) {
                        Console.WriteLine("SYM " + nowEvent.get_msg);
                        symmetrical_event_publisher.Publish("symmetrical_event", nowEvent.get_msg);
                    }
                    else {
                        Console.WriteLine("NONE " + nowEvent.get_msg);
                        nonsymmetrical_event_publisher.Publish("nonsymmetrical_event", nowEvent.get_msg);

                    }
                    // 05/11 06:43:52.024  :RightController|Output|Vibration|Amp 0.1600 Freq 1.0000 Duration 0.0000
                    // NONE 05 / 11 06:46:35.000  :| RightController | 0.16 | 1 | 0 | Vibration
                    // non pub:5 / 11 / 2023 6:46:35 AM | RightController | Output | Vibration | Amp 0.1600 Freq 1.0000 Duration 0.0000
                    lock (eventList) {
                        eventList.RemoveFirst();
                    }
                }
            }
        }
/////////////////////////////////////////////////////////

    }
}
