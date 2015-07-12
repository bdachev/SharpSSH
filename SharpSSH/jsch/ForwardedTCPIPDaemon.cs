using System;
using Tamir.SharpSsh.java.lang;

namespace Tamir.SharpSsh.jsch
{
    public interface ForwardedTCPIPDaemon : JavaRunnable
    {
        void setChannel(ChannelForwardedTCPIP channel);
        void setArg(Object[] arg);
    }
}