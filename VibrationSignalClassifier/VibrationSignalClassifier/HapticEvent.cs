using System;
using System.Globalization;


namespace VibrationSignalClassifier {
    public class HapticEvent {
        private readonly float duration_; // 這三個基本資訊合起來是 channel 裡的 stateinfo 
        private readonly float amplitude_;
        private readonly float frequency_;
        private readonly DateTime event_daytime_;// new CultureInfo("en-US");

        private readonly string event_type_;
        private readonly string event_time_;
        private readonly string event_name_;
        private readonly string source_type_name_;

        private readonly DateTime enter_list_time_;

        private readonly string msg;


        public HapticEvent(string EventTime, string SourceTypeName, string EventType, string EventName, string Amp, string Freq, string Dur, DateTime EnListTime, string msg) {
            this.duration_ = Convert.ToSingle(Dur);
            this.amplitude_ = Convert.ToSingle(Amp);
            this.frequency_ = Convert.ToSingle(Freq);
            this.event_daytime_ = DateTime.Parse(EventTime, new CultureInfo("en-US"), DateTimeStyles.NoCurrentDateDefault);

            this.event_time_ = EventTime;
            this.event_type_ = EventType;
            this.event_name_ = EventName;
            this.source_type_name_ = SourceTypeName;

            this.enter_list_time_ = EnListTime;

            this.msg = msg;

        }
        public float get_duration { get => duration_; }
        public float get_amplitude { get => amplitude_; }
        public float get_freqeuncy { get => frequency_; }
        public DateTime get_event_daytime { get => event_daytime_; }
        public string get_event_type { get => event_type_; }
        public string get_event_time { get => event_time_; }
        public string get_event_name { get => event_name_; }
        public string get_source_type_name { get => source_type_name_; }
        public DateTime get_enter_list_daytime { get => enter_list_time_; }

        public string get_msg { get => msg; }
    }
}
