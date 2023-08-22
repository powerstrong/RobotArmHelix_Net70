using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rosbridge.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static RobotArmHelix_WPF.RosbridgeModel;

namespace RobotArmHelix_WPF
{
    public sealed class RosbridgeMgr
    {
        private MessageDispatcher? _md;
        private RosbridgeModel _rosbrdgModel;

        private bool _isConnected;

        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                _isConnected = value;
                //Document.Instance.IsConnected = value;
            }
        }

        List<Subscriber> _subscribers;

        private RosbridgeMgr()
        {
            _rosbrdgModel = new RosbridgeModel();
            _subscribers = new List<Subscriber>();
            IsConnected = false;
        }
        private static readonly Lazy<RosbridgeMgr> _instance = new Lazy<RosbridgeMgr>(() => new RosbridgeMgr());
        public static RosbridgeMgr Instance => _instance.Value;

        private Window? _mainWindow;
        public void SetMainWindow(Window w)
        {
            _mainWindow = w;
        }

        public async void Close()
        {
            foreach (var s in _subscribers)
            {
                s.UnsubscribeAsync().Wait();
            }
            IsConnected = false;
            _subscribers.Clear();

            if (_md is not null)
            {
                await _md.StopAsync();
                _md = null;
            }
        }

        public async void Connect(string uri)
        {

            if (IsConnected)
            {
                foreach (var s in _subscribers)
                {
                    await s.UnsubscribeAsync();
                }
                IsConnected = false;
                _subscribers.Clear();

                if (_md is not null)
                {
                    await _md.StopAsync();
                    _md = null;
                }
            }
            else
            {
                try
                {
                    _md = new MessageDispatcher(new Socket(new Uri(uri)), new MessageSerializerV2_0());
                    _md.StartAsync().Wait();

                    foreach (var tuple in _rosbrdgModel.GetSubscribeTopics())
                    {
                        SubscribeMsg(tuple.Item1, tuple.Item2);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message,
                         "Rosbridge server에 연결할 수 없습니다.", MessageBoxButton.OK, MessageBoxImage.Error);
                    _md = null;
                    return;
                }

                IsConnected = true;
            }
        }

        public async void AdvertiseMsg(string topic, string msg_type)
        {
            if (_md is null) return;
            var pb = new Rosbridge.Client.Publisher(topic, msg_type, _md);
            await pb.AdvertiseAsync();
        }

        public async void UnadvertiseMsg(string topic, string msg_type)
        {
            if (_md is null) return;
            var pb = new Rosbridge.Client.Publisher(topic, msg_type, _md);
            await pb.UnadvertiseAsync();
        }

        public async void PublishMsg(string topic, string msg_type, string msg)
        {
            if (_md is null) return;

            var pb = new Rosbridge.Client.Publisher(topic, msg_type, _md);
            await pb.PublishAsync(JObject.Parse(msg));
        }

        private void _subscriber_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var topic = e.Message["topic"]!.ToString();
            switch (topic)
            {
                case RosTopics.joint_states:
                    {
                        List<string> names = new();
                        foreach (var name in e.Message["msg"]["name"])
                        {
                            names.Add(name.ToString());
                        }

                        List<double> positions = new();
                        foreach (var position in e.Message["msg"]["position"])
                        {
                            positions.Add(Double.Parse(position.ToString()));
                        }

                        (_mainWindow as MainWindow)?.SyncJointStates(positions);

                        //int rate = int.Parse(e.Message["msg"]["data"].ToString());
                        //main_vm.GetLearningVM().UpdateRate(rate);
                    }
                    break;
            }
        }

        public bool CheckOverlab_Subscriber(string topic)
        {
            bool result = false;

            foreach (var s in _subscribers)
            {
                if (s.Topic == topic)
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        public async void SubscribeMsg(string topic, string msg_type)
        {
            if (_md is null) return;

            if (CheckOverlab_Subscriber(topic))
            {
                return;
            }

            var s = new Subscriber(topic, msg_type, _md);
            s.MessageReceived += _subscriber_MessageReceived;

            await s.SubscribeAsync();
            _subscribers.Add(s);
        }

        public async void UnSubscribeMsg(string topic, string msg_type)
        {
            if (_md is null) return;

            foreach (var s in _subscribers)
            {
                if (s.Topic == topic)
                {
                    await s.UnsubscribeAsync();
                    _subscribers.Remove(s);
                    break;
                }
            }
        }

        public async Task<JToken?> ServiceCallMsg(string topic, string msg)
        {
            if (_md is null) return null;

            var sc = new ServiceClient(topic, _md);
            JArray argsList = JArray.Parse(msg);
            var dynamics = argsList.ToObject<List<dynamic>>();
            if (dynamics != null)
            {
                return await sc.Call(dynamics);
            }
            return null;
        }

        public async Task<JToken?> ServiceCallMsg(string topic, JObject jobj)
        {
            if (_md is null) return null;

            var sc = new ServiceClient(topic, _md);
            List<dynamic> dynamics = new() { jobj };
            //var dynamics = jobj.ToObject<List<dynamic>>();
            if (dynamics != null)
            {
                return await sc.Call(dynamics);
            }
            return null;
        }

        public void NodeOnOffCommand(string[] kill_nodes, string cmd)
        {
            if (_md is null) return;

            JObject obj = new();
            obj["kill_nodes"] = new JArray(kill_nodes);
            obj["start_launch_command"] = cmd;

            string messageJson = JsonConvert.SerializeObject(obj);
            PublishMsg("interface_manager/switch_command", "interface_manager/NodeCommand", messageJson);
        }
    }
}
