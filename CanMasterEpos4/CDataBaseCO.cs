using static IXXAT.CANopenMasterAPI6;

namespace EposCmd.Net
{
    public class CDataBaseCO
    {
        public readonly object NodePdoLock = new object();
        public readonly object NodeSdoLock = new object();
        protected readonly object LockingWpdoError = new object();
        protected readonly object LockingNmtStatus = new object();

        public byte NodeId { get; set; }
        public string Name { get; set; }

        // Zjednotená veľkosť buffrov na 8 bajtov pre bezpečnosť
        public byte[] TxdataPDO1 { get; set; } = new byte[8];
        public byte[] TxdataPDO2 { get; set; } = new byte[8];
        public byte[] TxdataPDO3 { get; set; } = new byte[8];
        public byte[] TxdataPDO4 { get; set; } = new byte[8];

        public COP_t_EMERGENCY_OBJ LastEmergency { get; set; }
        public COP_t_EVENT_OBJ LastEvent { get; set; }

        private ENmtStatus _nmtStatus;
        public ENmtStatus NmtStatus
        {
            get { lock (LockingNmtStatus) { return _nmtStatus; } }
            set { lock (LockingNmtStatus) { _nmtStatus = value; } }
        }

        private bool _wpdoError;
        private byte _wpdoErrorPdoNumber;

        public bool WpdoError
        {
            get { lock (LockingWpdoError) { return _wpdoError; } }
            set { lock (LockingWpdoError) { _wpdoError = value; } }
        }

        public byte WpdoErrorPdoNumber
        {
            get { lock (LockingWpdoError) { return _wpdoErrorPdoNumber; } }
            set { lock (LockingWpdoError) { _wpdoErrorPdoNumber = value; } }
        }

        public void ResetWpdoError()
        {
            lock (LockingWpdoError)
            {
                _wpdoError = false;
                _wpdoErrorPdoNumber = 0;
            }
        }

        // Virtuálna vlastnosť pre kontrolu, či je povolený zápis PDO
        public virtual bool IsPdoCommunicationAllowed => true;

        public string NmtStatusString()
        {
            switch (NmtStatus)
            {
                case ENmtStatus.NcsBOOTUP: return "Bootup";
                case ENmtStatus.NcsDISCONNECTED: return "Disconected";
                case ENmtStatus.NcsSTOPPED: return "Stopped";
                case ENmtStatus.NcsOPERATIONAL: return "Operational";
                case ENmtStatus.NcsPREOPERATIONAL: return "Preoperational";
                case ENmtStatus.NcsUNKNOWN: return "Unknown";
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}