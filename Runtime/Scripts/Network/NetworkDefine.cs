using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ST.Core.Network
{
    /// <summary>
    /// 
    /// </summary>
    public enum IMsgType
    {
        FlatBuffer,
        Protobuf,
    }

    /// <summary>
    /// 
    /// </summary>
    public class IFlatBufferProcFun
    {
        public virtual void Invoke(byte[] buf)
        {

        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class IProtobufProcFun
    {
        public virtual void Invoke(byte[] buf)
        {

        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="msg"></param>
    public delegate void MsgProcDelegate<T>(T msg);

    /// <summary>
    /// 
    /// </summary>
    public class NetworkDefine
    {

    }
}
