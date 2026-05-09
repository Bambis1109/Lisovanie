using System;
using EposCmd.Net.DeviceCmdSet.Configuration;
using EposCmd.Net.DeviceCmdSet.DataRecorder;
using EposCmd.Net.DeviceCmdSet.Initialization;
using EposCmd.Net.DeviceCmdSet.LowLayer;
using EposCmd.Net.DeviceCmdSet.Operation;
using static IXXAT.CANopenMasterAPI6;


namespace EposCmd
{
    namespace Net
    {
        public abstract class CDeviceCO
        {
            public CDataCO Data;

            protected ushort KeyHandle;
            public string Name = "Unknow";
            public byte NodeId;
            //public abstract Configuration Configuration { get; }
            //public abstract DataRecorder DataRecording { get; }
            //public abstract Initialization Initialization { get; }
            public abstract LowLayer LowLayer { get; }
            //public abstract Operation Operation { get; }

            public event EventHandler ReceivePdo;
            public event EventHandler ReceiveEmergency;
            public event EventHandler ReceiveStatus;

            protected void RecPdo(EventArgs e)
            {
                ReceivePdo?.Invoke(this, e);
            }

            protected void RecEmergency(EventArgs e)
            {
                ReceiveEmergency?.Invoke(this, e);
            }

            protected void RecStatus(EventArgs e)
            {
                ReceiveStatus?.Invoke(this, e);
            }

            public abstract void ReadPdo(COP_t_RX_PDO sp_pdo);
            public abstract void ReadStatus(COP_t_EVENT_OBJ eventMsg);
            public abstract void ReadEmergency(COP_t_EMERGENCY_OBJ sp_emergency);
            public abstract string GetLastEmergencyMsg();
           
           
        }
    }
}