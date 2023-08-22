using System;
using System.Collections.Generic;

namespace RobotArmHelix_WPF
{
    internal class RosbridgeModel
    {
        private List<Tuple<string, string>> _subscribe_topics; // <topic, msg_type>

        public static class RosTopics
        {
            public const string learn_progress = "learn_progress";
            public const string learn_status = "learn_status";

            public const string generate_rate = "generate_rate";
            public const string generate_status = "generate_status";

            public const string generate_image = "generate_image";
            public const string break_generate = "break_generate";
            public const string joint_states = "joint_states";
        }

        public RosbridgeModel()
        {
            _subscribe_topics = new List<Tuple<string, string>>
            {
                //new Tuple<string, string>(RosTopics.learn_progress, "std_msgs/String"),
                //new Tuple<string, string>(RosTopics.learn_status, "std_msgs/String"),

                //new Tuple<string, string>(RosTopics.generate_rate, "std_msgs/String"),
                //new Tuple<string, string>(RosTopics.generate_status, "std_msgs/String"),
                //new Tuple<string, string>(RosTopics.joint_states, "sensor_msgs/JointState"),
            };
        }

        public List<Tuple<string, string>> GetSubscribeTopics()
        {
            return _subscribe_topics;
        }

        public void AddSubscribeTopics(string topic, string msg_type)
        {
            _subscribe_topics.Add(new Tuple<string, string>(topic, msg_type));
        }
    }
}
