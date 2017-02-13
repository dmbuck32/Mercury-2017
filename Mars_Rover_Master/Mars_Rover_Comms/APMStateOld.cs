using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Collections;
using Mars_Rover_APM;

namespace Mars_Rover_Comms
{
    public class APMStateOld
    {
        public APMStateOld()
        {
            this.sysid = 0;
            this.compid = 0;
            this.param = new Hashtable();
            this.packets = new byte[0x100][];
            this.packetseencount = new int[0x100];
            this.aptype = 0;
            this.apname = 0;
            this.recvpacketcount = 0;
        }

        /// <summary>
        /// mavlink remote sysid
        /// </summary>
        public byte sysid { get; set; }
        /// <summary>
        /// mavlink remove compid
        /// </summary>
        public byte compid { get; set; }
        /// <summary>
        /// storage for whole paramater list
        /// </summary>
        public Hashtable param { get; set; }
        /// <summary>
        /// storage of a previous packet recevied of a specific type
        /// </summary>
        public byte[][] packets { get; set; }
        public int[] packetseencount { get; set; }
        /// <summary>
        /// mavlink ap type
        /// </summary>
        public APMOld.APM_TYPE aptype { get; set; }
        public APMOld.APM_AUTOPILOT apname { get; set; }
        /// <summary>
        /// used as a snapshot of what is loaded on the ap atm. - derived from the stream
        /// </summary>
        public Dictionary<int, APMOld.apm_mission_item_t> wps = new Dictionary<int, APMOld.apm_mission_item_t>();

        public Dictionary<int, APMOld.apm_rally_point_t> rallypoints = new Dictionary<int, APMOld.apm_rally_point_t>();

        public Dictionary<int, APMOld.apm_fence_point_t> fencepoints = new Dictionary<int, APMOld.apm_fence_point_t>();

        /// <summary>
        /// Store the guided mode wp location
        /// </summary>
        public APMOld.apm_mission_item_t GuidedMode = new APMOld.apm_mission_item_t();

        public int recvpacketcount = 0;
    }
}
