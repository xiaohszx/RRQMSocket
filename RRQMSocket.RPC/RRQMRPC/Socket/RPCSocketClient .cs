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
using RRQMCore.Exceptions;
using RRQMCore.Log;
using RRQMCore.Run;
using RRQMCore.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RRQMSocket.RPC.RRQMRPC
{
    /// <summary>
    /// RPC服务器辅助类
    /// </summary>
    public class RPCSocketClient : ProtocolSocketClient
    {
        static RPCSocketClient()
        {
            AddUsedProtocol(100, "请求RPC代理文件");
            AddUsedProtocol(101, "RPC调用");
            AddUsedProtocol(102, "获取注册服务");
            AddUsedProtocol(103, "ID调用客户端");
            AddUsedProtocol(104, "RPC回调");
        }

        internal Func<RPCSocketClient, RPCContext, RPCContext> IDAction;

        internal RRQMReceivedProcotolEventHandler Received;

        internal SerializeConverter serializeConverter;

        internal RRQMWaitHandle<RPCContext> waitHandle;

        /// <summary>
        /// 构造函数
        /// </summary>
        public RPCSocketClient()
        {
            waitHandle = new RRQMWaitHandle<RPCContext>();
        }

        /// <summary>
        /// 回调RPC
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="methodToken"></param>
        /// <param name="invokeOption"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public T CallBack<T>(int methodToken, InvokeOption invokeOption = null, params object[] parameters)
        {
            RPCContext context = new RPCContext();
            WaitData<RPCContext> waitData = this.waitHandle.GetWaitData(context);

            context.MethodToken = methodToken;

            ByteBlock byteBlock = this.BytePool.GetByteBlock(this.BufferLength);
            if (invokeOption == null)
            {
                invokeOption = InvokeOption.WaitInvoke;
            }
            try
            {
                context.Feedback = (byte)invokeOption.FeedbackType;
                List<byte[]> datas = new List<byte[]>();
                foreach (object parameter in parameters)
                {
                    datas.Add(this.serializeConverter.SerializeParameter (parameter));
                }
                context.ParametersBytes = datas;
                context.Serialize(byteBlock);

                this.InternalSend(104, byteBlock.Buffer, 0, byteBlock.Len);
            }
            catch (Exception e)
            {
                throw new RRQMException(e.Message);
            }
            finally
            {
                byteBlock.Dispose();
            }

            switch (invokeOption.FeedbackType)
            {
                case FeedbackType.OnlySend:
                    {
                        this.waitHandle.Destroy(waitData);
                        return default(T);
                    }
                case FeedbackType.WaitSend:
                    {
                        waitData.Wait(invokeOption.WaitTime * 1000);

                        RPCContext resultContext = waitData.WaitResult;
                        this.waitHandle.Destroy(waitData);
                        if (resultContext.Status == 0)
                        {
                            throw new RRQMTimeoutException("等待结果超时");
                        }
                        return default(T);
                    }
                case FeedbackType.WaitInvoke:
                    {
                        waitData.Wait(invokeOption.WaitTime * 1000);

                        RPCContext resultContext = waitData.WaitResult;
                        this.waitHandle.Destroy(waitData);
                        if (resultContext.Status == 0)
                        {
                            throw new RRQMTimeoutException("等待结果超时");
                        }
                        else if (resultContext.Status == 2)
                        {
                            throw new RRQMRPCInvokeException("未找到该公共方法，或该方法未标记RRQMRPCCallBackMethod");
                        }
                        else if (resultContext.Status == 3)
                        {
                            throw new RRQMRPCException("客户端未开启反向RPC");
                        }
                        else if (resultContext.Status == 4)
                        {
                            throw new RRQMRPCException($"调用异常，信息：{resultContext.Message}");
                        }

                        try
                        {
                            return (T)this.serializeConverter.DeserializeParameter(resultContext.ReturnParameterBytes, typeof(T));
                        }
                        catch (Exception e)
                        {
                            throw new RRQMException(e.Message);
                        }
                    }
                default:
                    return default(T);
            }
        }

        /// <summary>
        /// 回调RPC
        /// </summary>
        /// <param name="methodToken"></param>
        /// <param name="invokeOption"></param>
        /// <param name="parameters"></param>
        public void CallBack(int methodToken, InvokeOption invokeOption = null, params object[] parameters)
        {
            RPCContext context = new RPCContext();
            WaitData<RPCContext> waitData = this.waitHandle.GetWaitData(context);

            context.MethodToken = methodToken;

            ByteBlock byteBlock = this.BytePool.GetByteBlock(this.BufferLength);
            if (invokeOption == null)
            {
                invokeOption = InvokeOption.WaitInvoke;
            }
            try
            {
                context.Feedback = (byte)invokeOption.FeedbackType;
                List<byte[]> datas = new List<byte[]>();
                foreach (object parameter in parameters)
                {
                    datas.Add(this.serializeConverter.SerializeParameter(parameter));
                }
                context.ParametersBytes = datas;
                context.Serialize(byteBlock);

                this.InternalSend(104, byteBlock.Buffer, 0, byteBlock.Len);
            }
            catch (Exception e)
            {
                throw new RRQMException(e.Message);
            }
            finally
            {
                byteBlock.Dispose();
            }
            switch (invokeOption.FeedbackType)
            {
                case FeedbackType.OnlySend:
                    {
                        this.waitHandle.Destroy(waitData);
                        return;
                    }
                case FeedbackType.WaitSend:
                    {
                        waitData.Wait(invokeOption.WaitTime * 1000);

                        RPCContext resultContext = waitData.WaitResult;
                        this.waitHandle.Destroy(waitData);
                        if (resultContext.Status == 0)
                        {
                            throw new RRQMTimeoutException("等待结果超时");
                        }
                        return;
                    }
                case FeedbackType.WaitInvoke:
                    {
                        waitData.Wait(invokeOption.WaitTime * 1000);

                        RPCContext resultContext = waitData.WaitResult;
                        this.waitHandle.Destroy(waitData);
                        if (resultContext.Status == 0)
                        {
                            throw new RRQMTimeoutException("等待结果超时");
                        }
                        else if (resultContext.Status == 2)
                        {
                            throw new RRQMRPCInvokeException("未找到该公共方法，或该方法未标记RRQMRPCCallBackMethod");
                        }
                        else if (resultContext.Status == 3)
                        {
                            throw new RRQMRPCException("客户端未开启反向RPC");
                        }
                        else if (resultContext.Status == 4)
                        {
                            throw new RRQMRPCException($"调用异常，信息：{resultContext.Message}");
                        }
                        return;
                    }
                default:
                    return;
            }
        }

        /// <summary>
        /// 回调RPC
        /// </summary>
        /// <param name="invokeContext"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public byte[] CallBack(RPCContext invokeContext, int timeout)
        {
            RPCContext context = new RPCContext();
            WaitData<RPCContext> waitData = this.waitHandle.GetWaitData(context);
            context.MethodToken = invokeContext.MethodToken;

            ByteBlock byteBlock = this.BytePool.GetByteBlock(this.BufferLength);

            try
            {
                context.Feedback = invokeContext.Feedback;

                context.ParametersBytes = invokeContext.ParametersBytes;
                context.Serialize(byteBlock);

                this.InternalSend(104, byteBlock.Buffer, 0, byteBlock.Len);
            }
            catch (Exception e)
            {
                throw new RRQMException(e.Message);
            }
            finally
            {
                byteBlock.Dispose();
            }
            switch (invokeContext.Feedback)
            {
                case 0:
                    {
                        this.waitHandle.Destroy(waitData);
                        return null;
                    }
                case 1:
                    {
                        waitData.Wait(timeout * 1000);

                        RPCContext resultContext = waitData.WaitResult;
                        this.waitHandle.Destroy(waitData);
                        if (resultContext.Status == 0)
                        {
                            throw new RRQMTimeoutException("等待结果超时");
                        }
                        return null;
                    }
                case 2:
                    {
                        waitData.Wait(timeout * 1000);

                        RPCContext resultContext = waitData.WaitResult;
                        this.waitHandle.Destroy(waitData);
                        if (resultContext.Status == 0)
                        {
                            throw new RRQMTimeoutException("等待结果超时");
                        }
                        else if (resultContext.Status == 2)
                        {
                            throw new RRQMRPCInvokeException("未找到该公共方法，或该方法未标记RRQMRPCCallBackMethod");
                        }
                        else if (resultContext.Status == 3)
                        {
                            throw new RRQMRPCException("客户端未开启反向RPC");
                        }
                        else if (resultContext.Status == 4)
                        {
                            throw new RRQMRPCException($"调用异常，信息：{resultContext.Message}");
                        }

                        return resultContext.ReturnParameterBytes;
                    }
                default:
                    return null;
            }
        }

        /// <summary>
        /// 内部发送器
        /// </summary>
        /// <param name="procotol"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        internal void InternalSend(short procotol, byte[] buffer, int offset, int length)
        {
            base.InternalSend(procotol, buffer, offset, length);
        }

        /// <summary>
        /// 处理协议数据
        /// </summary>
        /// <param name="procotol"></param>
        /// <param name="byteBlock"></param>
        protected sealed override void HandleProtocolData(short? procotol, ByteBlock byteBlock)
        {
            byte[] buffer = byteBlock.Buffer;
            int r = (int)byteBlock.Position;
            switch (procotol)
            {
                case 100:/*100，请求RPC文件*/
                    {
                        try
                        {
                            string proxyToken = Encoding.UTF8.GetString(buffer, 2, r - 2);
                            byte[] data = SerializeConvert.RRQMBinarySerialize(((IRRQMRPCParser)this.Service).GetProxyInfo(proxyToken, this), true);
                            this.InternalSend(100, data, 0, data.Length);
                        }
                        catch (Exception e)
                        {
                            Logger.Debug(LogType.Error, this, $"错误代码: 100, 错误详情:{e.Message}");
                        }
                        break;
                    }
                case 101:/*函数调用*/
                    {
                        try
                        {
                            RPCContext content = RPCContext.Deserialize(buffer, 2);
                            if (content.Feedback == 1)
                            {
                                List<byte[]> ps = content.ParametersBytes;

                                ByteBlock returnByteBlock = this.BytePool.GetByteBlock(this.bufferLength);
                                try
                                {
                                    content.ParametersBytes = null;
                                    content.Status = 1;
                                    content.Serialize(returnByteBlock);
                                    this.InternalSend(101, returnByteBlock.Buffer, 0, (int)returnByteBlock.Length);
                                }
                                finally
                                {
                                    content.ParametersBytes = ps;
                                    returnByteBlock.Dispose();
                                }
                            }
                            ((IRRQMRPCParser)this.Service).ExecuteContext(content, this);
                        }
                        catch (Exception e)
                        {
                            Logger.Debug(LogType.Error, this, $"错误代码: 101, 错误详情:{e.Message}");
                        }
                        break;
                    }
                case 102:/*获取注册服务*/
                    {
                        try
                        {
                            string proxyToken = Encoding.UTF8.GetString(buffer, 2, r - 2);
                            byte[] data = SerializeConvert.RRQMBinarySerialize(((IRRQMRPCParser)this.Service).GetRegisteredMethodItems(proxyToken, this), true);
                            this.InternalSend(102, data, 0, data.Length);
                        }
                        catch (Exception e)
                        {
                            Logger.Debug(LogType.Error, this, $"错误代码: 102, 错误详情:{e.Message}");
                        }
                        break;
                    }
                case 103:/*ID调用客户端*/
                    {
                        Task.Run(() =>
                        {
                            try
                            {
                                RPCContext content = RPCContext.Deserialize(buffer, 2);
                                content = this.IDAction(this, content);

                                ByteBlock retuenByteBlock = this.BytePool.GetByteBlock(this.BufferLength);
                                try
                                {
                                    content.Serialize(retuenByteBlock);
                                    this.InternalSend(103, retuenByteBlock.Buffer, 0, (int)retuenByteBlock.Length);
                                }
                                finally
                                {
                                    byteBlock.Dispose();
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.Debug(LogType.Error, this, $"错误代码: 103, 错误详情:{e.Message}");
                            }
                        });
                        break;
                    }
                case 104:/*回调函数调用*/
                    {
                        try
                        {
                            RPCContext content = RPCContext.Deserialize(buffer, 2);
                            this.waitHandle.SetRun(content);
                        }
                        catch (Exception e)
                        {
                            Logger.Debug(LogType.Error, this, $"错误代码: 104, 错误详情:{e.Message}");
                        }
                        break;
                    }
                default:
                    RPCHandleDefaultData(procotol, byteBlock);
                    break;
            }
        }

        /// <summary>
        /// RPC处理其余协议
        /// </summary>
        /// <param name="procotol"></param>
        /// <param name="byteBlock"></param>
        protected virtual void RPCHandleDefaultData(short? procotol, ByteBlock byteBlock)
        {
            this.OnHandleDefaultData(procotol, byteBlock);
        }

        /// <summary>
        /// 处理其余协议的事件触发
        /// </summary>
        /// <param name="procotol"></param>
        /// <param name="byteBlock"></param>
        protected void OnHandleDefaultData(short? procotol, ByteBlock byteBlock)
        {
            Received?.Invoke(this, procotol, byteBlock);
        }
    }
}