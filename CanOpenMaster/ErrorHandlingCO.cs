namespace EposCmd
{
    namespace Net
    {
        public class ErrorHandlingCO : IDisposable
        {
            protected uint ErrorCode;
            protected bool IsDisposed;

            protected ErrorHandlingCO()
            {
            }

            public uint LastError { get; set; }

            public void Dispose()
            {
            }

            protected virtual void Dispose(bool isDisposeByUser)
            {
            }
        }
    }
}