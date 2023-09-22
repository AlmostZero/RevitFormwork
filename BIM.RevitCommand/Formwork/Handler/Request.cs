using System.Threading;

namespace BIM.RevitCommand.Formwork.Handler
{
    public enum RequestId : int
    {
        CreateFormwork,
        DeleteElement,
        None,
    }

    public class Request
    {
        private int m_request = ( int )RequestId.CreateFormwork;

        public RequestId Take()
        {
            return ( RequestId )Interlocked.Exchange( ref m_request, ( int )RequestId.CreateFormwork );
        }

        public void Make( RequestId request )
        {
            Interlocked.Exchange( ref m_request, ( int )request );
        }
    }
}
