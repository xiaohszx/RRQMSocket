//------------------------------------------------------------------------------
//  此代码版权归作者本人若汝棋茗所有
//  源代码使用协议遵循本仓库的开源协议及附加协议，若本仓库没有设置，则按MIT开源协议授权
//  CSDN博客：https://blog.csdn.net/qq_40374647
//  哔哩哔哩视频：https://space.bilibili.com/94253567
//  Gitee源代码仓库：https://gitee.com/RRQM_Home
//  Github源代码仓库：https://github.com/RRQM
//  交流QQ群：234762506
//  感谢您的下载和使用
//------------------------------------------------------------------------------
//------------------------------------------------------------------------------
using RRQMCore.ByteManager;
using System;

namespace RRQMSocket
{
    /// <summary>
    /// 简单协议服务器
    /// </summary>
    public class SimpleProtocolService : ProtocolService<SimpleProtocolSocketClient>
    {
        /// <summary>
        /// 处理数据
        /// </summary>
        public event Action<SimpleProtocolSocketClient, short?, ByteBlock> Received;

        /// <summary>
        /// 成功连接后创建（或从对象池中获得）辅助类,
        /// 用户可以在该方法中再进行自定义设置，
        /// 但是如果该对象是从对象池获得的话，为避免重复设定某些值，
        /// 例如事件等，请先判断CreatOption.NewCreat值再做处理。
        /// </summary>
        public event Action<SimpleProtocolSocketClient, CreateOption> CreateSocketCliect;

        /// <summary>
        /// 接收辅助类
        /// </summary>
        /// <param name="tcpSocketClient"></param>
        /// <param name="createOption"></param>
        protected override void OnCreateSocketCliect(SimpleProtocolSocketClient tcpSocketClient, CreateOption createOption)
        {
            this.CreateSocketCliect?.Invoke(tcpSocketClient, createOption);
            if (createOption.NewCreate)
            {
                tcpSocketClient.OnReceived = this.OnReceive;
            }
        }

        private void OnReceive(SimpleProtocolSocketClient socketClient, short? procotol, ByteBlock byteBlock)
        {
            this.Received?.Invoke(socketClient, procotol, byteBlock);
        }
    }
}