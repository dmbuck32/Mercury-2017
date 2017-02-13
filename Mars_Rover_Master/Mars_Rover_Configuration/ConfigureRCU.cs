using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Mars_Rover_Configuration
{
    [Serializable]
    public class ConfigureRCU
    {
        
        private ConfigureMaestro maestroConfig;
        private ConfigureAPM apmConfig;
        private ConfigureArm armConfig;
        private int listeningPort;
        private string wvuServerIP;
        //private List<ConfigureRoboteq> roboteqConfigs;
        //private ConfigureJRK jrkConfig;

        public ConfigureRCU()
        {
        }

        public ConfigureMaestro MaestroConfig
        {
            get { return maestroConfig; }
            set { maestroConfig = value; }
        }

        public ConfigureAPM APMConfig
        {
            get { return apmConfig; }
            set { apmConfig = value; }
        }

        public ConfigureArm ArmConfig
        {
            get { return armConfig; }
            set { armConfig = value; }
        }

        public int ListeningPort
        {
            get { return listeningPort; }
            set { listeningPort = value; }
        }

        public string WVUServerIP
        {
            get { return wvuServerIP; }
            set { wvuServerIP = value; }
        }

        //public List<ConfigureRoboteq> RoboteqConfigs
        //{
        //    get { return roboteqConfigs; }
        //    set { roboteqConfigs = value; }
        //}

        //public ConfigureJRK JRKConfig
        //{
        //    get { return jrkConfig; }
        //    set { jrkConfig = value; }
        //}
    }
}
