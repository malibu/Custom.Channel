using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace Apache.NMS.WCF
{

    public class NmsReplyChannel : NmsInputQueueChannelBase<Message>, IReplyChannel
    {

        public NmsReplyChannel(ChannelListenerBase factory, EndpointAddress localAddress)
            : base(factory, localAddress)
        {

        }

        public IAsyncResult BeginReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return BeginDequeue(timeout, callback, state);
        }

        public IAsyncResult BeginReceiveRequest(AsyncCallback callback, object state)
        {
            return BeginReceiveRequest(DefaultReceiveTimeout, callback, state);
        }

        public IAsyncResult BeginTryReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return BeginDequeue(timeout, callback, state);
        }

        public IAsyncResult BeginWaitForRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public RequestContext EndReceiveRequest(IAsyncResult result)
        {
            //return EndDequeue(result);
        }

        public bool EndTryReceiveRequest(IAsyncResult result, out RequestContext context)
        {
            message = null;
            //return TryDequeue(result, out message);
        }

        public bool EndWaitForRequest(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        public RequestContext ReceiveRequest(TimeSpan timeout)
        {
            return Receive(timeout);
        }

        public RequestContext ReceiveRequest()
        {
            return Receive(DefaultReceiveTimeout);
        }

        public bool TryReceiveRequest(TimeSpan timeout, out RequestContext context)
        {
            throw new NotImplementedException();
        }

        public bool WaitForRequest(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
    }
}
