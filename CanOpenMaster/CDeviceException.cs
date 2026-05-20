namespace EposCmd
{
    namespace Net
    {
        public class CDeviceException : Exception
        {
            public CDeviceException(string errorMessage)
            {
                ErrorMessage = errorMessage;
            }

            public CDeviceException(uint errorCode)
            {
                ErrorCode = errorCode;
            }

            public CDeviceException(string errorMessage, uint errorCode)
            {
                ErrorCode = errorCode;
                ErrorMessage = errorMessage;
            }

            public CDeviceException(string errorMessage, Exception innerEx)
            {
            }

            public CDeviceException(string errorMessage, uint errorCode, Exception innerEx)
            {
            }

            public uint ErrorCode { get; }
            public string ErrorMessage { get; }
        }
    }
}